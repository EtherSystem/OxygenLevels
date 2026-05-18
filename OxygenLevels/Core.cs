using AfflictionComponent.Components;
using LocalizationUtilities;
using OxygenLevels.Afflictions;
using OxygenLevels.Persistence;
using OxygenLevels.Resources.Localization;
using static OxygenLevels.Afflictions.AMS;
using static OxygenLevels.Afflictions.AMSrisk;

[assembly: MelonInfo(typeof(OxygenLevels.Core), "OxygenLevels", "2.0.0", "EtherSystem, Flower Field", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace OxygenLevels
{
    public class Core : MelonMod
    {
        private const float UpdateIntervalSeconds = 1f;
        private const float SuffocationThresholdHours = 1f;

        private const float OxygenStaminaEmptyEpsilon = 0.001f;
        private const float OxygenConditionDrainChunk = 0.01f;
        private const int MaxOxygenConditionDrainChunksPerTick = 512;
        private const float LastKnownOutdoorAltitudeSaveEpsilon = 1f;

        private const float AMSRiskHeadacheStart = 0.25f;
        private const float AMSRiskStaggerStart = 0.5f;
        private const float AMSRiskLogBucketSize = 0.25f;

        private const float DefaultAcclimatizationTimerHours = 24f;
        private const float CriticalAcclimatizedLossHoursAtDefaultTimer = 14f;
        private const float InsufficientAcclimatizedLossHoursAtDefaultTimer = 20f;
        private const float AcclimatizationBaseDecayMultiplier = 2f;
        private const float LowO2AcclimatizationDecayMultiplier = 8f;
        private const float NormalO2AcclimatizationDecayMultiplier = 10f;

        public enum AltitudeState
        {
            Normal,
            Weakened,
            HeavyWeakened,
            TooWeak
        }

        public static Core? Instance { get; private set; }
        public static AltitudeState currentState = AltitudeState.Normal;
        internal static OLState State = new();

        private float _updateTimerSeconds;

        private float _lastOutdoorAltitude;
        private bool _hasLastOutdoorAltitude;
        private float _defaultFireIgnitionTimeSeconds = -1f;

        private float _amsRollTimerHours;

        private bool _wasAnyAcclimatizedLastTick;
        private bool _lastAppliedCriticalAcclimatized;
        private bool _lastAppliedInsufficientAcclimatized;
        private bool _lastLoggedUnpreparedInsufficientExposure;

        private AltitudeState _lastAppliedAltitudeState = AltitudeState.Normal;

        private bool _wasAMSActiveLastTick;
        private uint _amsHeartbeatPlayingId;

        private float _amsNeurologicalStaggerPhase;
        private float _amsHeadachePulseRealTimer;
        private bool _lastAMSNeurologicalEffectsActive;
        private int _lastAMSNeurologicalLogBucket = -1;

        private static bool _suppressSprintStaminaPatch;
        private static bool _oxygenWalkingStaminaExhausted;

        private static bool _hasVanillaDysenteryRestForCure;
        private static float _vanillaDysenteryRestForCure;
        private static bool _hasVanillaFoodPoisoningRestForCure;
        private static float _vanillaFoodPoisoningRestForCure;

        public static string? LoadEmbeddedJSON(string resourceName)
        {
            string fullResourceName = resourceName.StartsWith("OxygenLevels.", StringComparison.Ordinal) ? resourceName : $"OxygenLevels.Resources.Localization.{resourceName}";

            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName);
            if (stream == null) return null;

            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        // -------------------------log helper-----------------------------------
        internal static void Log(string message, bool onlyWhenDebugEnabled = true)
        {
            if (onlyWhenDebugEnabled && !Settings.options.IsLogging) return;

            Instance?.LoggerInstance.Msg(message);
        }

        internal static void Warn(string message, bool onlyWhenDebugEnabled = true)
        {
            if (onlyWhenDebugEnabled && !Settings.options.IsLogging) return;

            Instance?.LoggerInstance.Warning(message);
        }

        public override void OnInitializeMelon()
        {
            Instance = this;

            LocalizationManager.LoadJsonLocalization(LoadEmbeddedJSON("Localization.json"));
            Log("takes a deep breath...", false);
            Settings.OnLoad();

            RegisterConsoleCommands();
        }

        public override void OnUpdate()
        {
            LocalizationRefresh.FlushPendingRefresh();
            UpdateAMSNeurologicalEffects();

            if (!ConsumeUpdateInterval(out float realTimeElapsedSeconds)) return;

            if (!CanRunGameplayUpdate())
            {
                ResetAMSAudio();
                ResetAMSNeurologicalEffects();
                _oxygenWalkingStaminaExhausted = false;
                currentState = AltitudeState.Normal;
                return;
            }

            if (GameManager.m_IsPaused) return;

            SaveDataManager.EnsureLoaded();

            CacheDefaultFireIgnitionTime();

            if (!TryGetEffectiveAltitude(out float altitude)) return;

            currentState = GetAltitudeState(altitude);

            float gameHoursPassed = GetGameHoursPassed(realTimeElapsedSeconds);
            bool hasAMSRisk = HasAffliction<AMSriskAffliction>();
            bool hasAMS = AMSAffliction.IsAMSActive || HasAffliction<AMSAffliction>();

            UpdateAcclimatization(gameHoursPassed, hasAMSRisk, hasAMS);
            ApplyAltitudeEffects();
            SyncAfflictionRestForCureOverrides();
            UpdateAMSProgression(gameHoursPassed, hasAMSRisk, hasAMS);
            UpdateAMSAudio(AMSAffliction.IsAMSActive || HasAffliction<AMSAffliction>());
            ApplyInsufficientOxygenWalkingEffects(realTimeElapsedSeconds);
        }

        private static void RegisterConsoleCommands()
        {
            // You need to be at the last threshold to apply them naturally or they will get instant cured by the system
            uConsole.RegisterCommand("ams", new Action(() =>
            {
                new AMSAffliction(AfflictionBodyArea.Head).Start();
                OnAMSApplied();
            }));

            uConsole.RegisterCommand("amsrisk", new Action(() =>
            {
                new AMSriskAffliction(AfflictionBodyArea.Head).Start();
            }));

            uConsole.RegisterCommand("ams_cure", new Action(DebugCureAMS));
            uConsole.RegisterCommand("amsrisk_cure", new Action(DebugCureAMSRisk));
            uConsole.RegisterCommand("acclimatized_i", new Action(() => DebugForceAcclimatization(forceLevelII: false)));
            uConsole.RegisterCommand("acclimatized_ii", new Action(() => DebugForceAcclimatization(forceLevelII: true)));
        }

        private bool ConsumeUpdateInterval(out float elapsedSeconds)
        {
            _updateTimerSeconds += Time.deltaTime;

            if (_updateTimerSeconds < UpdateIntervalSeconds)
            {
                elapsedSeconds = 0f;
                return false;
            }

            elapsedSeconds = _updateTimerSeconds;
            _updateTimerSeconds = 0f;
            return true;
        }

        private static bool CanRunGameplayUpdate()
        {
            if (GameManager.m_Instance == null) return false;

            string scene = GameManager.m_ActiveScene;
            return !string.IsNullOrEmpty(scene) && scene != "MainMenu" && scene != "Boot" && scene != "Empty";
        }

        private static float GetGameHoursPassed(float realTimeElapsedSeconds)
        {
            TimeOfDay tod = GameManager.GetTimeOfDayComponent();
            return tod != null ? tod.GetTODHours(realTimeElapsedSeconds) : 0f;
        }

        private void CacheDefaultFireIgnitionTime()
        {
            if (_defaultFireIgnitionTimeSeconds >= 0f) return;

            if (GameManager.m_FireManager == null) return;

            _defaultFireIgnitionTimeSeconds = GameManager.m_FireManager.m_StartFireTimeSeconds;
        }

        private bool TryGetEffectiveAltitude(out float altitude)
        {
            altitude = 0f;

            Transform playerTransform = GameManager.GetPlayerTransform();
            if (playerTransform == null) return false;

            float rawAltitude = playerTransform.position.y;
            Weather weather = GameManager.GetWeatherComponent();
            bool isIndoorScene = weather != null && weather.IsIndoorScene();

            if (!isIndoorScene)
            {
                SetLastKnownOutdoorAltitude(rawAltitude);
                altitude = rawAltitude;
                return true;
            }

            altitude = _hasLastOutdoorAltitude ? _lastOutdoorAltitude : 0f;
            return true;
        }

        private void SetLastKnownOutdoorAltitude(float altitude)
        {
            _lastOutdoorAltitude = altitude;
            _hasLastOutdoorAltitude = true;

            if (State.HasLastKnownOutdoorAltitude && Mathf.Abs(State.LastKnownOutdoorAltitude - altitude) < LastKnownOutdoorAltitudeSaveEpsilon) return;

            State.HasLastKnownOutdoorAltitude = true;
            State.LastKnownOutdoorAltitude = altitude;

            SaveDataManager.MarkDirty();
        }

        internal static void ClearRuntimeStateForSaveTransition()
        {
            currentState = AltitudeState.Normal;
            _suppressSprintStaminaPatch = false;
            _oxygenWalkingStaminaExhausted = false;

            Instance?.ClearInstanceRuntimeStateForSaveTransition();
        }

        internal static void RestoreRuntimeStateFromLoadedSave()
        {
            Instance?.RestoreInstanceRuntimeStateFromLoadedSave();
        }

        private void ClearInstanceRuntimeStateForSaveTransition()
        {
            ResetAMSAudio();

            _updateTimerSeconds = 0f;
            _lastOutdoorAltitude = 0f;
            _hasLastOutdoorAltitude = false;
            _defaultFireIgnitionTimeSeconds = -1f;
            _amsRollTimerHours = 0f;
            _wasAnyAcclimatizedLastTick = false;
            _lastLoggedUnpreparedInsufficientExposure = false;
            _lastAppliedAltitudeState = (AltitudeState)(-1);
            _lastAppliedCriticalAcclimatized = false;
            _lastAppliedInsufficientAcclimatized = false;
            ResetAMSNeurologicalEffects();
        }

        private void RestoreInstanceRuntimeStateFromLoadedSave()
        {
            if (State.HasLastKnownOutdoorAltitude)
            {
                _lastOutdoorAltitude = State.LastKnownOutdoorAltitude;
                _hasLastOutdoorAltitude = true;
                Log($"Restored last known outdoor altitude: {_lastOutdoorAltitude:0.##}m.");
                return;
            }

            _lastOutdoorAltitude = 0f;
            _hasLastOutdoorAltitude = false;
            Log("No saved last outdoor altitude found, indoor scenes will default to Normal O2 until an outdoor altitude is known.");
        }

        internal static void NormalizeState()
        {
            float requiredHours = Mathf.Max(0f, Settings.options.AcclimatizationTimer);
            float timerScale = DefaultAcclimatizationTimerHours > 0f ? requiredHours / DefaultAcclimatizationTimerHours : 1f;
            float criticalLossHours = Mathf.Clamp(CriticalAcclimatizedLossHoursAtDefaultTimer * timerScale, 0f, requiredHours);
            float insufficientLossHours = Mathf.Clamp(InsufficientAcclimatizedLossHoursAtDefaultTimer * timerScale, 0f, requiredHours);

            State.CriticalAcclimatizationHours = Mathf.Clamp(State.CriticalAcclimatizationHours, 0f, requiredHours);
            State.InsufficientAcclimatizationHours = Mathf.Clamp(State.InsufficientAcclimatizationHours, 0f, requiredHours);
            State.UnpreparedInsufficientO2Hours = Mathf.Max(0f, State.UnpreparedInsufficientO2Hours);
            State.AMSInsufficientO2Hours = Mathf.Max(0f, State.AMSInsufficientO2Hours);

            State.CriticalAcclimatized = requiredHours <= 0f || (State.CriticalAcclimatized ? State.CriticalAcclimatizationHours >= criticalLossHours : State.CriticalAcclimatizationHours >= requiredHours);

            if (!State.CriticalAcclimatized)
            {
                State.InsufficientAcclimatized = false;
                return;
            }

            State.InsufficientAcclimatized = requiredHours <= 0f || (State.InsufficientAcclimatized ? State.InsufficientAcclimatizationHours >= insufficientLossHours : State.InsufficientAcclimatizationHours >= requiredHours);
        }

        private void UpdateAcclimatization(float gameHoursPassed, bool hasAMSRisk, bool hasAMS)
        {
            if (gameHoursPassed <= 0f) return;

            float requiredHours = Mathf.Max(0f, Settings.options.AcclimatizationTimer);
            bool blockGain = hasAMSRisk || hasAMS;

            float oldCriticalHours = State.CriticalAcclimatizationHours;
            float oldInsufficientHours = State.InsufficientAcclimatizationHours;
            bool oldCritical = State.CriticalAcclimatized;
            bool oldInsufficient = State.InsufficientAcclimatized;

            switch (currentState)
            {
                case AltitudeState.HeavyWeakened:
                    UpdateCriticalAcclimatization(gameHoursPassed, requiredHours, blockGain);
                    DecayInsufficientAcclimatization(gameHoursPassed);
                    break;

                case AltitudeState.TooWeak:
                    if (State.CriticalAcclimatized && !blockGain)
                    {
                        UpdateInsufficientAcclimatization(gameHoursPassed, requiredHours);
                    }
                    break;

                case AltitudeState.Weakened:
                    DecayCriticalAcclimatization(gameHoursPassed, LowO2AcclimatizationDecayMultiplier);
                    DecayInsufficientAcclimatization(gameHoursPassed, LowO2AcclimatizationDecayMultiplier);
                    break;

                default:
                    DecayCriticalAcclimatization(gameHoursPassed, NormalO2AcclimatizationDecayMultiplier);
                    DecayInsufficientAcclimatization(gameHoursPassed, NormalO2AcclimatizationDecayMultiplier);
                    break;
            }

            NormalizeState();

            bool changed =
                !Mathf.Approximately(oldCriticalHours, State.CriticalAcclimatizationHours) ||
                !Mathf.Approximately(oldInsufficientHours, State.InsufficientAcclimatizationHours) ||
                oldCritical != State.CriticalAcclimatized ||
                oldInsufficient != State.InsufficientAcclimatized;

            if (changed) SaveDataManager.MarkDirty();

            LogAcclimatizationTransitions(oldCritical, oldInsufficient);
            SyncAcclimatizedBuffs();
            UpdateAcclimatizationHudMessage(State.CriticalAcclimatized || State.InsufficientAcclimatized);
        }

        private static void UpdateCriticalAcclimatization(float gameHoursPassed, float requiredHours, bool blockGain)
        {
            if (blockGain) return;

            float before = State.CriticalAcclimatizationHours;

            State.CriticalAcclimatizationHours += gameHoursPassed;
            State.CriticalAcclimatizationHours = Mathf.Clamp(State.CriticalAcclimatizationHours, 0f, requiredHours);

            if (before <= 0f && State.CriticalAcclimatizationHours > 0f)
            {
                Log("Critical O2 acclimatization started.");
            }
        }

        private static void UpdateInsufficientAcclimatization(float gameHoursPassed, float requiredHours)
        {
            float before = State.InsufficientAcclimatizationHours;

            State.InsufficientAcclimatizationHours += gameHoursPassed;
            State.InsufficientAcclimatizationHours = Mathf.Clamp(State.InsufficientAcclimatizationHours, 0f, requiredHours);

            if (before <= 0f && State.InsufficientAcclimatizationHours > 0f)
            {
                Log("Insufficient O2 acclimatization started.");
            }
        }

        internal static float GetAMSConditionLossPerHour()
        {
            return Settings.options.AMSLethalityPreset switch
            {
                0 => 4f,     // Forgiving: roughly lethal after 20h
                1 => 5f,     // Standard: ~15h
                2 => 6.7f,   // Harsh:  ~10h
                3 => 11.1f,  // Brutal:  ~6h
                4 => 33.4f,  // Who Wants to Play Like This?: ~2h
                _ => 5f
            };
        }

        private static void DecayCriticalAcclimatization(float gameHoursPassed, float decayMultiplier = 1f)
        {
            if (State.CriticalAcclimatizationHours <= 0f) return;

            State.CriticalAcclimatizationHours -= gameHoursPassed * AcclimatizationBaseDecayMultiplier * decayMultiplier;
            State.CriticalAcclimatizationHours = Mathf.Max(0f, State.CriticalAcclimatizationHours);
        }

        private static void DecayInsufficientAcclimatization(float gameHoursPassed, float decayMultiplier = 1f)
        {
            if (State.InsufficientAcclimatizationHours <= 0f) return;

            State.InsufficientAcclimatizationHours -= gameHoursPassed * AcclimatizationBaseDecayMultiplier * decayMultiplier;
            State.InsufficientAcclimatizationHours = Mathf.Max(0f, State.InsufficientAcclimatizationHours);
        }

        private static void LogAcclimatizationTransitions(bool oldCritical, bool oldInsufficient)
        {
            bool shouldQueueSurvivalSave = false;

            if (!oldCritical && State.CriticalAcclimatized)
            {
                Log("Critical O2 acclimatization completed.");
                shouldQueueSurvivalSave = true;
            }
            else if (oldCritical && !State.CriticalAcclimatized)
            {
                Log("Critical O2 acclimatization lost.");
                shouldQueueSurvivalSave = true;
            }

            if (!oldInsufficient && State.InsufficientAcclimatized)
            {
                Log("Insufficient O2 acclimatization completed.");
                shouldQueueSurvivalSave = true;
            }
            else if (oldInsufficient && !State.InsufficientAcclimatized)
            {
                Log("Insufficient O2 acclimatization lost.");
                shouldQueueSurvivalSave = true;
            }

            if (shouldQueueSurvivalSave) SaveDataManager.MarkDirtyAndQueueSurvivalSave();
        }

        private void UpdateAcclimatizationHudMessage(bool anyAcclimatized)
        {
            if (anyAcclimatized && !_wasAnyAcclimatizedLastTick)
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_isAcclimatized"), 5, false);
            }
            else if (!anyAcclimatized && _wasAnyAcclimatizedLastTick)
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_isntAcclimatized"), 5, false);
            }

            _wasAnyAcclimatizedLastTick = anyAcclimatized;
        }

        private void ApplyAltitudeEffects()
        {
            bool criticalAcclimatized = State.CriticalAcclimatized;
            bool insufficientAcclimatized = State.InsufficientAcclimatized;

            bool shouldRefresh = currentState != _lastAppliedAltitudeState || criticalAcclimatized != _lastAppliedCriticalAcclimatized || insufficientAcclimatized != _lastAppliedInsufficientAcclimatized;

            if (!shouldRefresh) return;

            _lastAppliedAltitudeState = currentState;
            _lastAppliedCriticalAcclimatized = criticalAcclimatized;
            _lastAppliedInsufficientAcclimatized = insufficientAcclimatized;

            bool acclimatizedForCurrentAltitude = IsAcclimatizedForCurrentAltitude(currentState);

            AltitudeEffectMultipliers multipliers = GetAltitudeEffectMultipliers(currentState, acclimatizedForCurrentAltitude);

            ShowAltitudeStateWarning(currentState, acclimatizedForCurrentAltitude);
            ApplyRuntimeMultipliers(multipliers);
        }

        private static bool IsAcclimatizedForCurrentAltitude(AltitudeState state)
        {
            return state switch
            {
                AltitudeState.HeavyWeakened => State.CriticalAcclimatized,
                AltitudeState.TooWeak => State.InsufficientAcclimatized,
                _ => false
            };
        }

        private static AltitudeState GetAltitudeState(float altitude)
        {
            if (altitude >= Settings.options.InsuThreshold) return AltitudeState.TooWeak;
            if (altitude >= Settings.options.CritThreshold) return AltitudeState.HeavyWeakened;
            if (altitude >= Settings.options.LowThreshold) return AltitudeState.Weakened;

            return AltitudeState.Normal;
        }

        private static AltitudeEffectMultipliers GetAltitudeEffectMultipliers(AltitudeState state, bool acclimatized)
        {
            return state switch
            {
                AltitudeState.Weakened => new AltitudeEffectMultipliers(
                    Settings.options.LowStaminaMultiplier,
                    Settings.options.LowStaminaConsumptionMultiplier,
                    Settings.options.LowMaxFatigueBurnMultiplier,
                    Settings.options.LowMinFatigueBurnMultiplier,
                    Settings.options.LowFireIgnitionMultiplier,
                    Settings.options.LowSecondsBeforeRecovStamMultiplier,
                    Settings.options.LowDysenteryRecoveryTimeMultiplier,
                    Settings.options.LowFoodPoisoningRecoveryTimeMultiplier),

                AltitudeState.HeavyWeakened => new AltitudeEffectMultipliers(
                    Settings.options.CritStaminaMultiplier * (acclimatized ? 1.5f : 1f),
                    Settings.options.CritStaminaConsumptionMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.CritMaxFatigueBurnMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.CritMinFatigueBurnMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.CritFireIgnitionMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.CritSecondsBeforeRecovStamMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.CritDysenteryRecoveryTimeMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.CritFoodPoisoningRecoveryTimeMultiplier * (acclimatized ? 0.5f : 1f)),

                AltitudeState.TooWeak => new AltitudeEffectMultipliers(
                    (Settings.options.InsuStaminaMultiplier / 10f) * (acclimatized ? 1.5f : 1f),
                    Settings.options.InsuStaminaConsumptionMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.InsuMaxFatigueBurnMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.InsuMinFatigueBurnMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.InsuFireIgnitionMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.InsuSecondsBeforeRecovStamMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.InsuDysenteryRecoveryTimeMultiplier * (acclimatized ? 0.5f : 1f),
                    Settings.options.InsuFoodPoisoningRecoveryTimeMultiplier * (acclimatized ? 0.5f : 1f)),

                _ => AltitudeEffectMultipliers.Default
            };
        }

        private static void ShowAltitudeStateWarning(AltitudeState state, bool acclimatized)
        {
            string? localizationKey = state switch
            {
                AltitudeState.Normal => "GAMEPLAY_NormalizedWarning",
                AltitudeState.Weakened => "GAMEPLAY_LowWarning",
                AltitudeState.HeavyWeakened when !acclimatized => "GAMEPLAY_CriticalWarning",
                AltitudeState.TooWeak when !acclimatized => "GAMEPLAY_InsufficientWarning",
                _ => null
            };

            if (!string.IsNullOrEmpty(localizationKey)) HUDMessage.AddMessage(Localization.Get(localizationKey), 5, false);
        }

        private void ApplyRuntimeMultipliers(AltitudeEffectMultipliers multipliers)
        {
            if (GameManager.m_FireManager != null && _defaultFireIgnitionTimeSeconds >= 0f)
            {
                GameManager.m_FireManager.m_StartFireTimeSeconds = _defaultFireIgnitionTimeSeconds * multipliers.FireIgnition;
            }
        }

        private void UpdateAMSProgression(float gameHoursPassed, bool hadAMSRiskAtTickStart, bool hasAMS)
        {
            if (gameHoursPassed <= 0f) return;

            if (hasAMS)
            {
                State.UnpreparedInsufficientO2Hours = 0f;

                if (currentState == AltitudeState.TooWeak)
                {
                    if (State.AMSInsufficientO2Hours <= 0f)
                    {
                        Log("AMS Insufficient O2 exposure started.");
                    }

                    State.AMSInsufficientO2Hours += gameHoursPassed;

                    SaveDataManager.MarkDirty();
                    return;
                }

                if (State.AMSInsufficientO2Hours <= 0f)
                {
                    CureAMSAndAMSRisk();
                    SaveDataManager.MarkDirtyAndQueueSurvivalSave();
                    return;
                }

                State.AMSInsufficientO2Hours -= gameHoursPassed * Settings.options.AMSDisappearanceTime;
                State.AMSInsufficientO2Hours = Mathf.Max(0f, State.AMSInsufficientO2Hours);

                SaveDataManager.MarkDirty();

                if (State.AMSInsufficientO2Hours <= 0f)
                {
                    CureAMSAndAMSRisk();
                    Log("AMS Insufficient O2 exposure recovered.");
                    SaveDataManager.MarkDirtyAndQueueSurvivalSave();
                }

                return;
            }

            if (State.AMSInsufficientO2Hours > 0f)
            {
                State.AMSInsufficientO2Hours = 0f;
                SaveDataManager.MarkDirty();
            }

            if (currentState == AltitudeState.TooWeak && !State.CriticalAcclimatized)
            {
                if (State.UnpreparedInsufficientO2Hours <= 0f && !_lastLoggedUnpreparedInsufficientExposure)
                {
                    Log("Unprepared Insufficient O2 exposure started.");
                    _lastLoggedUnpreparedInsufficientExposure = true;
                }

                State.UnpreparedInsufficientO2Hours += gameHoursPassed;

                SaveDataManager.MarkDirty();

                TryRollForAMSRisk(gameHoursPassed, hadAMSRiskAtTickStart);
                return;
            }

            if (State.UnpreparedInsufficientO2Hours <= 0f)
            {
                _lastLoggedUnpreparedInsufficientExposure = false;
                return;
            }

            State.UnpreparedInsufficientO2Hours -= gameHoursPassed * Settings.options.AMSDisappearanceTime;
            State.UnpreparedInsufficientO2Hours = Mathf.Max(0f, State.UnpreparedInsufficientO2Hours);

            SaveDataManager.MarkDirty();

            if (State.UnpreparedInsufficientO2Hours <= 0f)
            {
                _lastLoggedUnpreparedInsufficientExposure = false;
                CureAMSAndAMSRisk();
                Log("Unprepared Insufficient O2 exposure recovered.");
                SaveDataManager.MarkDirtyAndQueueSurvivalSave();
            }
        }

        internal static void OnAMSApplied()
        {
            State.UnpreparedInsufficientO2Hours = 0f;
            State.AMSInsufficientO2Hours = 0f;

            Core? instance = Instance;
            if (instance != null)
            {
                instance._amsRollTimerHours = 0f;
                instance._lastLoggedUnpreparedInsufficientExposure = false;
            }

            Log("AMS applied. AMS exposure debt starts now.");

            SaveDataManager.MarkDirtyAndQueueSurvivalSave();
        }

        private static void DebugCureAMS()
        {
            SaveDataManager.EnsureLoaded();

            CureAffliction<AMSAffliction>();
            State.AMSInsufficientO2Hours = 0f;
            State.UnpreparedInsufficientO2Hours = 0f;

            Core? instance = Instance;
            if (instance != null)
            {
                instance._amsRollTimerHours = 0f;
                instance._lastLoggedUnpreparedInsufficientExposure = false;
                instance.ResetAMSAudio();
            }

            ClearAMSCameraEffectsIfInactive();

            HUDMessage.AddMessage("AMS cured.", 5, false);
            Log("Debug command: AMS cured.");
            SaveDataManager.MarkDirtyAndQueueSurvivalSave();
        }

        private static void DebugCureAMSRisk()
        {
            SaveDataManager.EnsureLoaded();

            CureAffliction<AMSriskAffliction>();
            State.UnpreparedInsufficientO2Hours = 0f;

            Core? instance = Instance;
            if (instance != null)
            {
                instance._amsRollTimerHours = 0f;
                instance._lastLoggedUnpreparedInsufficientExposure = false;
            }

            ClearAMSCameraEffectsIfInactive();

            HUDMessage.AddMessage("AMS Risk cured.", 5, false);
            Log("Debug command: AMS Risk cured.");
            SaveDataManager.MarkDirtyAndQueueSurvivalSave();
        }

        private static void DebugForceAcclimatization(bool forceLevelII)
        {
            SaveDataManager.EnsureLoaded();

            float requiredHours = Mathf.Max(0f, Settings.options.AcclimatizationTimer);

            State.CriticalAcclimatizationHours = requiredHours;
            State.CriticalAcclimatized = true;

            State.InsufficientAcclimatizationHours = forceLevelII ? requiredHours : 0f;
            State.InsufficientAcclimatized = forceLevelII;

            SyncAcclimatizedBuffs();

            Core? instance = Instance;
            if (instance != null)
            {
                instance._lastAppliedAltitudeState = (AltitudeState)(-1);
                instance._lastAppliedCriticalAcclimatized = !State.CriticalAcclimatized;
                instance._lastAppliedInsufficientAcclimatized = !State.InsufficientAcclimatized;
                instance._wasAnyAcclimatizedLastTick = State.CriticalAcclimatized || State.InsufficientAcclimatized;
            }

            string label = forceLevelII ? "Acclimatized II" : "Acclimatized I";
            HUDMessage.AddMessage($"{label} forced.", 5, false);
            Log($"Debug command: {label} forced. CriticalHours:{State.CriticalAcclimatizationHours:0.###} InsufficientHours:{State.InsufficientAcclimatizationHours:0.###}");
            SaveDataManager.MarkDirtyAndQueueSurvivalSave();
        }

        private void TryRollForAMSRisk(float gameHoursPassed, bool hadAMSRiskAtTickStart)
        {
            float amsAppearanceThresholdHours = SuffocationThresholdHours * Settings.options.AMSAppeanceTime;

            bool canRoll = currentState == AltitudeState.TooWeak && !State.CriticalAcclimatized && State.UnpreparedInsufficientO2Hours >= amsAppearanceThresholdHours && !AMSAffliction.IsAMSActive && !HasAffliction<AMSAffliction>();

            if (!canRoll)
            {
                _amsRollTimerHours = 0f;
                return;
            }

            _amsRollTimerHours += gameHoursPassed;
            if (_amsRollTimerHours < 1f) return;

            _amsRollTimerHours = 0f;

            if (hadAMSRiskAtTickStart || HasAffliction<AMSriskAffliction>()) return;

            float chance = Mathf.Clamp(Settings.options.AMSchance, 0f, 100f);

            if (chance <= 0f) return;

            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll >= chance) return;

            new AMSriskAffliction(AfflictionBodyArea.Head).Start();

            Log($"AMSrisk applied: unacclimatized Insufficient O2 exposure. Chance:{chance:0.#}% Roll:{roll:0.#}");

            SaveDataManager.MarkDirtyAndQueueSurvivalSave();
        }

        private static void CureAMSAndAMSRisk()
        {
            var mgr = AfflictionManager.GetAfflictionManagerInstance();
            if (mgr?.m_Afflictions == null) return;

            for (int i = mgr.m_Afflictions.Count - 1; i >= 0; i--)
            {
                var affliction = mgr.m_Afflictions[i];
                if (affliction == null) continue;

                if (affliction is AMSAffliction || affliction is AMSriskAffliction) affliction.Cure();
            }
        }

        private static void ClearAMSCameraEffectsIfInactive()
        {
            if (AMSAffliction.IsAMSActive || HasAffliction<AMSAffliction>() || HasAffliction<AMSriskAffliction>()) return;

            var cameraStatus = GameManager.GetCameraStatusEffects();
            if (cameraStatus == null) return;

            cameraStatus.m_TriggerHeadachePulse = false;
            cameraStatus.m_TriggerSuffocationPulse = false;
        }

        private void UpdateAMSNeurologicalEffects()
        {
            if (!CanRunGameplayUpdate() || GameManager.m_IsPaused)
            {
                SuspendAMSNeurologicalEffects();
                return;
            }

            bool hasAMS = AMSAffliction.IsAMSActive || HasAffliction<AMSAffliction>();
            AMSriskAffliction? amsRisk = GetAffliction<AMSriskAffliction>();

            if (!hasAMS && amsRisk == null)
            {
                ResetAMSNeurologicalEffects();
                ClearAMSCameraEffectsIfInactive();
                return;
            }

            float risk01 = amsRisk != null ? Mathf.Clamp01(amsRisk.GetRiskValue() / 100f) : 0f;
            float severity01 = hasAMS ? 1f : risk01;

            UpdateAMSHeadacheEffect(severity01, hasAMS);
            UpdateAMSPhysicalStagger(severity01, hasAMS);
            LogAMSNeurologicalEffects(severity01, hasAMS);
        }

        private void UpdateAMSHeadacheEffect(float severity01, bool hasAMS)
        {
            float headache01 = hasAMS ? 1f : Mathf.Clamp01(Mathf.InverseLerp(AMSRiskHeadacheStart, 1f, severity01));

            if (headache01 <= 0f)
            {
                _amsHeadachePulseRealTimer = 0f;
                return;
            }

            _amsHeadachePulseRealTimer += Time.unscaledDeltaTime;

            float pulseInterval = Mathf.Lerp(16f, 6f, headache01);
            if (_amsHeadachePulseRealTimer < pulseInterval) return;

            _amsHeadachePulseRealTimer = 0f;

            try
            {
                CameraStatusEffects cameraStatus = GameManager.GetCameraStatusEffects();
                if (cameraStatus == null) return;

                float amount = Mathf.Lerp(0.08f, hasAMS ? 0.55f : 0.35f, headache01);
                cameraStatus.HeadachePulse(amount);
            }
            catch
            {
            }
        }

        private void UpdateAMSPhysicalStagger(float severity01, bool hasAMS)
        {
            float stagger01 = hasAMS ? 1f : severity01 >= AMSRiskStaggerStart ? Mathf.Clamp01(severity01) : 0f;

            if (stagger01 <= 0f)
            {
                _amsNeurologicalStaggerPhase = 0f;
                return;
            }

            ApplyAMSPhysicalStagger(stagger01, hasAMS);
        }

        private void ApplyAMSPhysicalStagger(float stagger01, bool hasAMS)
        {
            if (stagger01 <= 0f) return;

            try
            {
                if (!hasAMS)
                {
                    PlayerManager pm = GameManager.GetPlayerManagerComponent();
                    if (pm == null) return;
                    if (!pm.PlayerIsWalking() && !pm.PlayerIsSprinting()) return;
                }

                var player = GameManager.GetVpFPSPlayer();
                if (player == null || player.Controller == null) return;

                Transform transform = player.transform;
                if (transform == null) return;

                float smoothStagger01 = hasAMS ? 1f : stagger01;

                _amsNeurologicalStaggerPhase += Time.deltaTime * 1.15f;

                float sideStrength = 0.00085f;
                float forwardStrength = 0.00013f;

                float side = Mathf.Sin(_amsNeurologicalStaggerPhase) * sideStrength * smoothStagger01;
                float forward = Mathf.Sin(_amsNeurologicalStaggerPhase * 0.57f + 1.9f) * forwardStrength * smoothStagger01;

                Vector3 force = transform.right * side + transform.forward * forward;

                player.Controller.AddForce(force);
            }
            catch
            {
            }
        }

        private void LogAMSNeurologicalEffects(float severity01, bool hasAMS)
        {
            int bucket = hasAMS ? 4 : Mathf.FloorToInt(Mathf.Clamp01(severity01) / AMSRiskLogBucketSize);
            bool active = hasAMS || severity01 > 0f;

            if (!active)
            {
                if (_lastAMSNeurologicalEffectsActive) Log("AMS neurological effects ended.");

                _lastAMSNeurologicalEffectsActive = false;
                _lastAMSNeurologicalLogBucket = -1;
                return;
            }

            if (_lastAMSNeurologicalEffectsActive && bucket == _lastAMSNeurologicalLogBucket) return;

            _lastAMSNeurologicalEffectsActive = true;
            _lastAMSNeurologicalLogBucket = bucket;

            string stage = hasAMS ? "AMS" : $"AMS Risk {Mathf.RoundToInt(severity01 * 100f)}%";
            Log($"AMS neurological effects active -> {stage}");
        }

        private void SuspendAMSNeurologicalEffects()
        {
            _amsNeurologicalStaggerPhase = 0f;
            _amsHeadachePulseRealTimer = 0f;
        }

        private void ResetAMSNeurologicalEffects()
        {
            SuspendAMSNeurologicalEffects();

            if (_lastAMSNeurologicalEffectsActive) Log("AMS neurological effects ended.");

            _lastAMSNeurologicalEffectsActive = false;
            _lastAMSNeurologicalLogBucket = -1;
        }

        private static void ApplyInsufficientOxygenWalkingEffects(float realSecondsElapsed)
        {
            if (realSecondsElapsed <= 0f) return;

            if (currentState != AltitudeState.TooWeak)
            {
                _oxygenWalkingStaminaExhausted = false;
                return;
            }

            PlayerManager playerManager = GameManager.GetPlayerManagerComponent();
            PlayerMovement playerMovement = GameManager.GetPlayerMovementComponent();

            if (playerManager == null || playerMovement == null) return;

            bool isWalking = playerManager.PlayerIsWalking();
            bool isSprinting = playerManager.PlayerIsSprinting();

            if (isSprinting)
            {
                _oxygenWalkingStaminaExhausted = false;
                return;
            }

            if (!isWalking)
            {
                _oxygenWalkingStaminaExhausted = false;
                KeepSprintBarVisibleBriefly();
                return;
            }

            float desiredStaminaDrain = Settings.options.InsuStaminaWalkingBurn * realSecondsElapsed;
            if (desiredStaminaDrain <= 0f) return;

            float currentStamina = Mathf.Max(0f, playerMovement.CurrentStamina);

            ShowSprintBarForOxygenDrain();

            if (_oxygenWalkingStaminaExhausted || currentStamina <= OxygenStaminaEmptyEpsilon)
            {
                _oxygenWalkingStaminaExhausted = true;

                if (currentStamina > 0f) AddSprintStaminaWithoutOxygenMultiplier(playerMovement, -currentStamina);

                ApplyConditionLossFromOxygenExhaustion(realSecondsElapsed);
                return;
            }

            if (currentStamina > desiredStaminaDrain)
            {
                AddSprintStaminaWithoutOxygenMultiplier(playerMovement, -desiredStaminaDrain);
                return;
            }

            AddSprintStaminaWithoutOxygenMultiplier(playerMovement, -currentStamina);
            _oxygenWalkingStaminaExhausted = true;

            float missingStaminaDrain = desiredStaminaDrain - currentStamina;
            float conditionDamageRatio = Mathf.Clamp01(missingStaminaDrain / desiredStaminaDrain);
            float conditionDamageSeconds = realSecondsElapsed * conditionDamageRatio;

            ApplyConditionLossFromOxygenExhaustion(conditionDamageSeconds);
        }

        private static void AddSprintStaminaWithoutOxygenMultiplier(PlayerMovement playerMovement, float amount)
        {
            if (playerMovement == null || Mathf.Approximately(amount, 0f)) return;

            _suppressSprintStaminaPatch = true;

            try
            {
                playerMovement.AddSprintStamina(amount);
            }
            finally
            {
                _suppressSprintStaminaPatch = false;
            }
        }

        private static bool ShouldForceSprintBarForOxygenWalking()
        {
            if (currentState != AltitudeState.TooWeak) return false;

            if (!CanRunGameplayUpdate()) return false;

            if (GameManager.m_IsPaused) return false;

            PlayerManager playerManager = GameManager.GetPlayerManagerComponent();
            if (playerManager == null) return false;

            return playerManager.PlayerIsWalking() && !playerManager.PlayerIsSprinting();
        }

        private static void ForceSprintBarVisibleForOxygenWalking(Panel_HUD hud)
        {
            if (hud == null) return;

            if (!ShouldForceSprintBarForOxygenWalking()) return;

            hud.m_SprintBar.alpha = 1f;
            hud.m_SprintFadeTimeTracker = 2f;
            hud.m_SprintBar_SecondsBeforeFadeOut = 2f;
        }

        private static void ShowSprintBarForOxygenDrain()
        {
            Panel_HUD hud = InterfaceManager.GetPanel<Panel_HUD>();
            if (hud == null) return;

            ForceSprintBarVisibleForOxygenWalking(hud);
        }

        private static void KeepSprintBarVisibleBriefly()
        {
            Panel_HUD hud = InterfaceManager.GetPanel<Panel_HUD>();
            if (hud == null) return;

            hud.m_SprintBar_SecondsBeforeFadeOut = 2f;
        }

        private static void ApplyConditionLossFromOxygenExhaustion(float realSecondsElapsed)
        {
            if (realSecondsElapsed <= 0f) return;

            float conditionLoss = (Settings.options.ConditionLostZeroStamina / 10f) * realSecondsElapsed;

            if (conditionLoss <= 0f) return;

            ApplyChunkedConditionDrain(conditionLoss, DamageSource.Unspecified);
        }

        internal static float ApplyChunkedConditionDrain(float hpLoss, DamageSource damageSource = DamageSource.Unspecified)
        {
            Condition condition = GameManager.GetConditionComponent();

            if (condition == null || hpLoss <= 0f || condition.m_CurrentHP <= 0f) return 0f;

            float remaining = hpLoss;
            float applied = 0f;
            int chunks = 0;

            while (remaining > 0.0001f && condition.m_CurrentHP > 0f)
            {
                float chunk = Mathf.Min(remaining, OxygenConditionDrainChunk);
                float before = condition.m_CurrentHP;

                condition.AddHealth(-chunk, damageSource);

                float after = condition.m_CurrentHP;
                applied += Mathf.Max(0f, before - after);

                remaining -= chunk;
                chunks++;

                if (chunks >= MaxOxygenConditionDrainChunksPerTick) break;
            }

            return applied;
        }

        private static bool HasAffliction<T>() where T : class
        {
            return GetAffliction<T>() != null;
        }

        private static T? GetAffliction<T>() where T : class
        {
            var mgr = AfflictionManager.GetAfflictionManagerInstance();
            if (mgr?.m_Afflictions == null) return null;

            for (int i = 0; i < mgr.m_Afflictions.Count; i++)
            {
                if (mgr.m_Afflictions[i] is T affliction) return affliction;
            }

            return null;
        }

        private static void CureAffliction<T>() where T : CustomAffliction
        {
            var mgr = AfflictionManager.GetAfflictionManagerInstance();
            if (mgr?.m_Afflictions == null) return;

            for (int i = mgr.m_Afflictions.Count - 1; i >= 0; i--)
            {
                var affliction = mgr.m_Afflictions[i];
                if (affliction == null) continue;

                if (affliction is T) affliction.Cure();
            }
        }

        private static void SyncAfflictionBuff<T>(bool shouldHaveBuff, Func<T> createBuff) where T : CustomAffliction
        {
            bool hasBuff = HasAffliction<T>();

            if (shouldHaveBuff && !hasBuff)
            {
                createBuff().Start();
                return;
            }

            if (!shouldHaveBuff && hasBuff)
            {
                CureAffliction<T>();
            }
        }

        private static void SyncAcclimatizedBuffs()
        {
            bool shouldHaveLevelII = State.InsufficientAcclimatized;
            bool shouldHaveLevelI = State.CriticalAcclimatized && !State.InsufficientAcclimatized;

            SyncAfflictionBuff<CriticalAcclimatizedBuff>(shouldHaveLevelI, () => new CriticalAcclimatizedBuff());

            SyncAfflictionBuff<InsufficientAcclimatizedBuff>(shouldHaveLevelII, () => new InsufficientAcclimatizedBuff());
        }

        private void UpdateAMSAudio(bool hasAMS)
        {
            GameObject player = GameManager.GetPlayerObject();
            if (player == null)
            {
                if (!hasAMS) StopAMSHeartbeat();

                _wasAMSActiveLastTick = hasAMS;
                return;
            }

            if (hasAMS && !_wasAMSActiveLastTick)
            {
                GameAudioManager.PlaySound(Il2CppAK.EVENTS.PLAY_CONDITIONPOOR, player);

                if (_amsHeartbeatPlayingId == 0)
                {
                    _amsHeartbeatPlayingId = AkSoundEngine.PostEvent(Il2CppAK.EVENTS.PLAY_CONDITIONHEARTBEAT, player);
                }
            }
            else if (!hasAMS && _wasAMSActiveLastTick)
            {
                StopAMSHeartbeat();
            }

            _wasAMSActiveLastTick = hasAMS;
        }

        private void StopAMSHeartbeat()
        {
            if (_amsHeartbeatPlayingId == 0) return;

            AkSoundEngine.StopPlayingID(_amsHeartbeatPlayingId);
            _amsHeartbeatPlayingId = 0;
        }

        private void ResetAMSAudio()
        {
            StopAMSHeartbeat();
            _wasAMSActiveLastTick = false;
        }

        private static bool ShouldDisableNaturalConditionRecovery()
        {
            return AMSAffliction.IsAMSActive || HasAffliction<AMSAffliction>();
        }

        private static bool ShouldApplyAltitudeStaminaEffects()
        {
            if (!CanRunGameplayUpdate()) return false;

            if (GameManager.m_IsPaused) return false;

            return currentState != AltitudeState.Normal;
        }

        private static bool IsWalkingAtInsufficientOxygenThreshold()
        {
            if (currentState != AltitudeState.TooWeak) return false;

            if (!CanRunGameplayUpdate()) return false;

            if (GameManager.m_IsPaused) return false;

            PlayerManager playerManager = GameManager.GetPlayerManagerComponent();
            if (playerManager == null) return false;

            return playerManager.PlayerIsWalking() && !playerManager.PlayerIsSprinting();
        }

        private static AltitudeEffectMultipliers GetCurrentAltitudeEffectMultipliers()
        {
            bool acclimatized = IsAcclimatizedForCurrentAltitude(currentState);
            return GetAltitudeEffectMultipliers(currentState, acclimatized);
        }

        private readonly struct AltitudeEffectMultipliers
        {
            internal static readonly AltitudeEffectMultipliers Default = new(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);

            internal readonly float StaminaRecovery;
            internal readonly float StaminaConsumption;
            internal readonly float MaxFatigueBurn;
            internal readonly float MinFatigueBurn;
            internal readonly float FireIgnition;
            internal readonly float SecondsBeforeStaminaRecovery;
            internal readonly float DysenteryRecoveryTime;
            internal readonly float FoodPoisoningRecoveryTime;

            internal AltitudeEffectMultipliers(
                float staminaRecovery,
                float staminaConsumption,
                float maxFatigueBurn,
                float minFatigueBurn,
                float fireIgnition,
                float secondsBeforeStaminaRecovery,
                float dysenteryRecoveryTime,
                float foodPoisoningRecoveryTime)
            {
                StaminaRecovery = staminaRecovery;
                StaminaConsumption = staminaConsumption;
                MaxFatigueBurn = maxFatigueBurn;
                MinFatigueBurn = minFatigueBurn;
                FireIgnition = fireIgnition;
                SecondsBeforeStaminaRecovery = secondsBeforeStaminaRecovery;
                DysenteryRecoveryTime = dysenteryRecoveryTime;
                FoodPoisoningRecoveryTime = foodPoisoningRecoveryTime;
            }
        }

        private readonly struct MovementDelayPatchState
        {
            internal readonly bool Active;
            internal readonly float OriginalSecondsBeforeRecovery;

            internal MovementDelayPatchState(bool active, float originalSecondsBeforeRecovery)
            {
                Active = active;
                OriginalSecondsBeforeRecovery = originalSecondsBeforeRecovery;
            }
        }

        private readonly struct FatiguePatchState
        {
            internal readonly bool Active;
            internal readonly float OriginalSprintingMin;
            internal readonly float OriginalSprintingMax;

            internal FatiguePatchState(bool active, float originalSprintingMin, float originalSprintingMax)
            {
                Active = active;
                OriginalSprintingMin = originalSprintingMin;
                OriginalSprintingMax = originalSprintingMax;
            }
        }

        private readonly struct AfflictionDurationPatchState
        {
            internal readonly bool Active;
            internal readonly float OriginalDurationMin;
            internal readonly float OriginalDurationMax;
            internal readonly float ModifiedDurationMin;
            internal readonly float ModifiedDurationMax;
            internal readonly float OriginalRestForCure;
            internal readonly float ModifiedRestForCure;
            internal readonly float Multiplier;

            internal AfflictionDurationPatchState(
                bool active,
                float originalDurationMin,
                float originalDurationMax,
                float modifiedDurationMin,
                float modifiedDurationMax,
                float originalRestForCure,
                float modifiedRestForCure,
                float multiplier)
            {
                Active = active;
                OriginalDurationMin = originalDurationMin;
                OriginalDurationMax = originalDurationMax;
                ModifiedDurationMin = modifiedDurationMin;
                ModifiedDurationMax = modifiedDurationMax;
                OriginalRestForCure = originalRestForCure;
                ModifiedRestForCure = modifiedRestForCure;
                Multiplier = multiplier;
            }
        }

        private static float GetVanillaDysenteryRestForCure(Dysentery dysentery)
        {
            if (!_hasVanillaDysenteryRestForCure)
            {
                _vanillaDysenteryRestForCure = Mathf.Max(0f, dysentery.m_NumHoursRestForCure);
                _hasVanillaDysenteryRestForCure = true;
            }

            return _vanillaDysenteryRestForCure;
        }

        private static float GetVanillaFoodPoisoningRestForCure(FoodPoisoning foodPoisoning)
        {
            if (!_hasVanillaFoodPoisoningRestForCure)
            {
                _vanillaFoodPoisoningRestForCure = Mathf.Max(0f, foodPoisoning.m_NumHoursRestForCure);
                _hasVanillaFoodPoisoningRestForCure = true;
            }

            return _vanillaFoodPoisoningRestForCure;
        }

        private static void ClearDysenteryRestForCureOverride(Dysentery dysentery)
        {
            if (_hasVanillaDysenteryRestForCure) dysentery.m_NumHoursRestForCure = _vanillaDysenteryRestForCure;

            if (!State.HasDysenteryRestForCureOverride) return;

            State.HasDysenteryRestForCureOverride = false;
            State.DysenteryRestForCureHours = 0f;
            SaveDataManager.MarkDirty();
        }

        private static void ClearFoodPoisoningRestForCureOverride(FoodPoisoning foodPoisoning)
        {
            if (_hasVanillaFoodPoisoningRestForCure) foodPoisoning.m_NumHoursRestForCure = _vanillaFoodPoisoningRestForCure;

            if (!State.HasFoodPoisoningRestForCureOverride) return;

            State.HasFoodPoisoningRestForCureOverride = false;
            State.FoodPoisoningRestForCureHours = 0f;
            SaveDataManager.MarkDirty();
        }

        private static void SyncAfflictionRestForCureOverrides()
        {
            Dysentery dysentery = GameManager.GetDysenteryComponent();
            if (dysentery != null)
            {
                GetVanillaDysenteryRestForCure(dysentery);

                if (dysentery.HasDysentery() && State.HasDysenteryRestForCureOverride)
                {
                    dysentery.m_NumHoursRestForCure = Mathf.Max(0f, State.DysenteryRestForCureHours);
                }
                else if (!dysentery.HasDysentery())
                {
                    ClearDysenteryRestForCureOverride(dysentery);
                }
            }

            FoodPoisoning foodPoisoning = GameManager.GetFoodPoisoningComponent();
            if (foodPoisoning != null)
            {
                GetVanillaFoodPoisoningRestForCure(foodPoisoning);

                if (foodPoisoning.HasFoodPoisoning() && State.HasFoodPoisoningRestForCureOverride)
                {
                    foodPoisoning.m_NumHoursRestForCure = Mathf.Max(0f, State.FoodPoisoningRestForCureHours);
                }
                else if (!foodPoisoning.HasFoodPoisoning())
                {
                    ClearFoodPoisoningRestForCureOverride(foodPoisoning);
                }
            }
        }

        // Patches for altitude stamina effects
        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.AddSprintStamina), typeof(float))]
        internal static class PlayerMovement_AddSprintStamina_OxygenPatch
        {
            private static void Prefix(ref float __0)
            {
                if (_suppressSprintStaminaPatch) return;

                if (Mathf.Approximately(__0, 0f)) return;

                if (_oxygenWalkingStaminaExhausted && __0 > 0f && IsWalkingAtInsufficientOxygenThreshold())
                {
                    __0 = 0f;
                    return;
                }

                if (!ShouldApplyAltitudeStaminaEffects()) return;

                AltitudeEffectMultipliers multipliers = GetCurrentAltitudeEffectMultipliers();

                if (__0 < 0f)
                {
                    __0 *= Mathf.Max(0f, multipliers.StaminaConsumption);
                    return;
                }

                __0 *= Mathf.Max(0f, multipliers.StaminaRecovery);
            }
        }

        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.Update))]
        internal static class PlayerMovement_Update_OxygenRecoveryDelayPatch
        {
            private static void Prefix(PlayerMovement __instance, out MovementDelayPatchState __state)
            {
                __state = default;

                if (__instance == null) return;

                if (!ShouldApplyAltitudeStaminaEffects()) return;

                AltitudeEffectMultipliers multipliers = GetCurrentAltitudeEffectMultipliers();

                __state = new MovementDelayPatchState(
                    true,
                    __instance.m_SecondsNotSprintingBeforeRecovery
                );

                __instance.m_SecondsNotSprintingBeforeRecovery =
                    __state.OriginalSecondsBeforeRecovery *
                    Mathf.Max(0f, multipliers.SecondsBeforeStaminaRecovery);
            }

            private static void Postfix(PlayerMovement __instance, MovementDelayPatchState __state)
            {
                if (__instance == null || !__state.Active) return;

                __instance.m_SecondsNotSprintingBeforeRecovery = __state.OriginalSecondsBeforeRecovery;
            }
        }

        [HarmonyPatch(typeof(Fatigue), nameof(Fatigue.CalculateFatigueIncrease), typeof(float))]
        internal static class Fatigue_CalculateFatigueIncrease_OxygenPatch
        {
            private static void Prefix(Fatigue __instance, out FatiguePatchState __state)
            {
                __state = default;

                if (__instance == null) return;

                if (!ShouldApplyAltitudeStaminaEffects()) return;

                AltitudeEffectMultipliers multipliers = GetCurrentAltitudeEffectMultipliers();

                __state = new FatiguePatchState(
                    true,
                    __instance.m_FatigueIncreasePerHourSprintingMin,
                    __instance.m_FatigueIncreasePerHourSprintingMax
                );

                __instance.m_FatigueIncreasePerHourSprintingMin =
                    __state.OriginalSprintingMin *
                    Mathf.Max(0f, multipliers.MinFatigueBurn);

                __instance.m_FatigueIncreasePerHourSprintingMax =
                    __state.OriginalSprintingMax *
                    Mathf.Max(0f, multipliers.MaxFatigueBurn);
            }

            private static void Postfix(Fatigue __instance, FatiguePatchState __state)
            {
                if (__instance == null || !__state.Active) return;

                __instance.m_FatigueIncreasePerHourSprintingMin = __state.OriginalSprintingMin;
                __instance.m_FatigueIncreasePerHourSprintingMax = __state.OriginalSprintingMax;
            }
        }

        [HarmonyPatch(typeof(Dysentery), nameof(Dysentery.DysenteryStart), typeof(bool), typeof(bool))]
        internal static class Dysentery_DysenteryStart_OxygenPatch
        {
            private static void Prefix(Dysentery __instance, out AfflictionDurationPatchState __state)
            {
                __state = default;

                if (__instance == null) return;

                if (!ShouldApplyAltitudeStaminaEffects()) return;

                AltitudeEffectMultipliers multipliers = GetCurrentAltitudeEffectMultipliers();
                float recoveryTimeMultiplier = Mathf.Max(0f, multipliers.DysenteryRecoveryTime);
                float originalMin = __instance.m_DurationHoursMin;
                float originalMax = __instance.m_DurationHoursMax;
                float originalRestForCure = GetVanillaDysenteryRestForCure(__instance);
                float modifiedMin = originalMin * recoveryTimeMultiplier;
                float modifiedMax = originalMax * recoveryTimeMultiplier;
                float modifiedRestForCure = originalRestForCure * recoveryTimeMultiplier;

                __state = new AfflictionDurationPatchState(true, originalMin, originalMax, modifiedMin, modifiedMax, originalRestForCure, modifiedRestForCure, recoveryTimeMultiplier);

                __instance.m_DurationHoursMin = modifiedMin;
                __instance.m_DurationHoursMax = modifiedMax;
                __instance.m_NumHoursRestForCure = modifiedRestForCure;
            }

            private static void Postfix(Dysentery __instance, AfflictionDurationPatchState __state)
            {
                if (__instance == null || !__state.Active) return;

                __instance.m_DurationHoursMin = __state.OriginalDurationMin;
                __instance.m_DurationHoursMax = __state.OriginalDurationMax;
                __instance.m_NumHoursRestForCure = __state.ModifiedRestForCure;

                State.HasDysenteryRestForCureOverride = true;
                State.DysenteryRestForCureHours = __state.ModifiedRestForCure;
                SaveDataManager.MarkDirtyAndQueueSurvivalSave();
            }
        }

        [HarmonyPatch(typeof(Dysentery), nameof(Dysentery.DysenteryEnd), typeof(bool))]
        internal static class Dysentery_DysenteryEnd_OxygenPatch
        {
            private static void Postfix(Dysentery __instance)
            {
                if (__instance == null) return;

                ClearDysenteryRestForCureOverride(__instance);
                SaveDataManager.MarkDirtyAndQueueSurvivalSave();
            }
        }

        [HarmonyPatch(typeof(FoodPoisoning), nameof(FoodPoisoning.FoodPoisoningStart), typeof(string), typeof(bool), typeof(bool))]
        internal static class FoodPoisoning_FoodPoisoningStart_OxygenPatch
        {
            private static void Prefix(FoodPoisoning __instance, string causeId, out AfflictionDurationPatchState __state)
            {
                __state = default;

                if (__instance == null) return;

                if (!ShouldApplyAltitudeStaminaEffects()) return;

                AltitudeEffectMultipliers multipliers = GetCurrentAltitudeEffectMultipliers();
                float recoveryTimeMultiplier = Mathf.Max(0f, multipliers.FoodPoisoningRecoveryTime);
                float originalMin = __instance.m_DurationHoursMin;
                float originalMax = __instance.m_DurationHoursMax;
                float originalRestForCure = GetVanillaFoodPoisoningRestForCure(__instance);
                float modifiedMin = originalMin * recoveryTimeMultiplier;
                float modifiedMax = originalMax * recoveryTimeMultiplier;
                float modifiedRestForCure = originalRestForCure * recoveryTimeMultiplier;

                __state = new AfflictionDurationPatchState(true, originalMin, originalMax, modifiedMin, modifiedMax, originalRestForCure, modifiedRestForCure, recoveryTimeMultiplier);

                __instance.m_DurationHoursMin = modifiedMin;
                __instance.m_DurationHoursMax = modifiedMax;
                __instance.m_NumHoursRestForCure = modifiedRestForCure;

                Log(
                    $"[FoodPoisoningPatch Prefix] State:{currentState} Cause:{causeId} " +
                    $"VanillaDurationBounds Min:{originalMin:0.###}h Max:{originalMax:0.###}h " +
                    $"ModifiedDurationBounds Min:{modifiedMin:0.###}h Max:{modifiedMax:0.###}h " +
                    $"VanillaRestForCure:{originalRestForCure:0.###}h ModifiedRestForCure:{modifiedRestForCure:0.###}h " +
                    $"Multiplier:{recoveryTimeMultiplier:0.###}"
                );
            }

            private static void Postfix(FoodPoisoning __instance, AfflictionDurationPatchState __state)
            {
                if (__instance == null || !__state.Active) return;

                float finalDuration = __instance.m_DurationHours;

                __instance.m_DurationHoursMin = __state.OriginalDurationMin;
                __instance.m_DurationHoursMax = __state.OriginalDurationMax;
                __instance.m_NumHoursRestForCure = __state.ModifiedRestForCure;

                State.HasFoodPoisoningRestForCureOverride = true;
                State.FoodPoisoningRestForCureHours = __state.ModifiedRestForCure;
                SaveDataManager.MarkDirtyAndQueueSurvivalSave();

                Log(
                    $"[FoodPoisoningPatch Postfix] FinalDuration:{finalDuration:0.###}h " +
                    $"RolledFromModifiedDurationBounds Min:{__state.ModifiedDurationMin:0.###}h Max:{__state.ModifiedDurationMax:0.###}h " +
                    $"ActiveRestForCure:{__instance.m_NumHoursRestForCure:0.###}h " +
                    $"RestoredVanillaDurationBounds Min:{__instance.m_DurationHoursMin:0.###}h Max:{__instance.m_DurationHoursMax:0.###}h " +
                    $"Multiplier:{__state.Multiplier:0.###}"
                );
            }
        }

        [HarmonyPatch(typeof(FoodPoisoning), nameof(FoodPoisoning.FoodPoisoningEnd), typeof(bool))]
        internal static class FoodPoisoning_FoodPoisoningEnd_OxygenPatch
        {
            private static void Postfix(FoodPoisoning __instance)
            {
                if (__instance == null) return;

                ClearFoodPoisoningRestForCureOverride(__instance);
                SaveDataManager.MarkDirtyAndQueueSurvivalSave();
            }
        }

        // Patch to keep the sprint bar visible while oxygen walking drain is active
        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.Update))]
        internal static class PanelHUD_Update_OxygenSprintBarPatch
        {
            private static void Postfix(Panel_HUD __instance)
            {
                ForceSprintBarVisibleForOxygenWalking(__instance);
            }
        }

        // Patches to stop natural condition regen caused by AMS
        [HarmonyPatch(typeof(Condition), nameof(Condition.Update))]
        internal static class AMSHealthyConditionRecoveryPatch
        {
            private static void Prefix(Condition __instance, out float __state)
            {
                __state = 0f;

                if (__instance == null || !ShouldDisableNaturalConditionRecovery()) return;

                __state = __instance.m_HPIncreasePerDayWhileHealthy;
                __instance.m_HPIncreasePerDayWhileHealthy = 0f;
            }

            private static void Postfix(Condition __instance, float __state)
            {
                if (__instance == null || !ShouldDisableNaturalConditionRecovery()) return;

                __instance.m_HPIncreasePerDayWhileHealthy = __state;
            }
        }

        [HarmonyPatch(typeof(Condition), nameof(Condition.MaybeIncreaseConditionFromWillpower))]
        internal static class Condition_MaybeIncreaseConditionFromWillpower
        {
            private static bool Prefix()
            {
                return !ShouldDisableNaturalConditionRecovery();
            }
        }
    }
}