using Il2Cpp;
using Il2CppRewired;
using Il2CppRewired.ComponentControls.Data;
using MelonLoader;
using UnityEngine;
using LocalizationUtilities;
using ModComponent;

[assembly: MelonInfo(typeof(OxygenLevels.Core), "OxygenLevels", "1.2.0", "EtherSystem", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace OxygenLevels
{
    public class Core : MelonMod
    {
        public static bool isInterHUD = false;
        private float timeSpentAtCritAltitude = 0f;
        private readonly float SUFFOCATION_THRESHOLD_HOURS = 1f;
        private float lastOutdoorAltitude = 0f;
        public static string? LoadEmbeddedJSON(string Localization)
        {
            string? result = null;

            Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OxygenLevels.Localization.json");
            if (stream != null)
            {
                StreamReader reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }
            return result;
        }
        public override void OnInitializeMelon()
        {
            LocalizationManager.LoadJsonLocalization(LoadEmbeddedJSON("Localization.json"));
            LoggerInstance.Msg("takes a deep breath...");
            Settings.OnLoad();
        }
        private float updateTimer = 0f;
        private readonly float baseDelayRecoverStam = 2f;
        private readonly float baseStaminaRegenRate = 500f;
        private readonly float baseStaminaSprintUsageRate = 5f;
        private readonly float baseMaxFatigueSprintUsageRate = 150f;
        private readonly float baseMinFatigueSprintUsageRate = 1f;
        private float defaultFireIgnitionTime = -1f;
        private readonly float baseDysenteryMaxRecoveryTime = 24f;
        private readonly float baseDysenteryMinRecoveryTime = 18f;
        private readonly float baseFoodPoisoningMaxRecoveryTime = 24f;
        private readonly float baseFoodPoisoningMinRecoveryTime = 12f;
        private float acclimatationTimerHours = 0f;
        private bool isAcclimatized = false;
        private bool wasAcclimatizedLastFrame = false;

        public enum AltitudeState { Normal, Weakened, HeavyWeakened, TooWeak }
        public static AltitudeState currentState = AltitudeState.Normal;

        public override void OnUpdate()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer < 1.0f) return;
            float realTimeElapsed = updateTimer;
            updateTimer = 0f;

            if (GameManager.m_Instance == null || GameManager.m_IsPaused) return;
            string scene = GameManager.m_ActiveScene;
            if (scene == "MainMenu" || scene == "Boot" || scene == "Empty") return;

            if (defaultFireIgnitionTime < 0f)
            {
                defaultFireIgnitionTime = GameManager.m_FireManager.m_StartFireTimeSeconds;
            }
            float rawY = GameManager.GetPlayerTransform().position.y;
            bool isIndoors = GameManager.GetWeatherComponent().IsIndoorScene();
            if (!isIndoors)
            {
                lastOutdoorAltitude = rawY;
            }
            float yValue = isIndoors ? lastOutdoorAltitude : rawY;

            float gameHoursPassed = GameManager.GetTimeOfDayComponent().GetTODHours(realTimeElapsed);
            float HOURS_TO_ACCLIMATIZE = (Settings.options.AcclimatizationTimer);

            if (currentState >= AltitudeState.HeavyWeakened)
            {
                acclimatationTimerHours += gameHoursPassed;
            }
            else if (currentState <= AltitudeState.Weakened)
            {
                acclimatationTimerHours -= gameHoursPassed * 2f;
            }
            acclimatationTimerHours = Mathf.Clamp(acclimatationTimerHours, 0f, HOURS_TO_ACCLIMATIZE);
            isAcclimatized = (acclimatationTimerHours >= HOURS_TO_ACCLIMATIZE);
            //MelonLogger.Msg("base : " + acclimatationTimerHours);

            if (isAcclimatized && !wasAcclimatizedLastFrame)
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_isAcclimatized"), 5, false);
            }
            if (!isAcclimatized && wasAcclimatizedLastFrame)
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_isntAcclimatized"), 5, false);
            }
            wasAcclimatizedLastFrame = isAcclimatized;

            ApplyAltitudeEffects(yValue);

            if (currentState == AltitudeState.TooWeak)
            {
                timeSpentAtCritAltitude += gameHoursPassed;
                timeSpentAtCritAltitude = Math.Min(timeSpentAtCritAltitude, 4f);
            }
            else
            {
                timeSpentAtCritAltitude -= (gameHoursPassed * Settings.options.AMSDisappearanceTime);
                timeSpentAtCritAltitude = Mathf.Max(timeSpentAtCritAltitude, 0f);
            }
            var cameraStatus = GameManager.GetCameraStatusEffects();
            if (timeSpentAtCritAltitude > 0f)
            {
                if (timeSpentAtCritAltitude >= (SUFFOCATION_THRESHOLD_HOURS * Settings.options.AMSAppeanceTime))
                {
                    cameraStatus.m_TriggerHeadachePulse = false;
                    cameraStatus.m_TriggerSuffocationPulse = true;
                }
                else
                {
                    cameraStatus.m_TriggerHeadachePulse = true;
                    cameraStatus.m_TriggerSuffocationPulse = false;
                }
            }
            else
            {
                cameraStatus.m_TriggerHeadachePulse = false;
                cameraStatus.m_TriggerSuffocationPulse = false;
            }

            var isWalking = GameManager.GetPlayerManagerComponent().PlayerIsWalking();
            var isSprinting = GameManager.GetPlayerManagerComponent().PlayerIsSprinting();
            var currentStamina = GameManager.GetPlayerMovementComponent().CurrentStamina;

            if (isWalking == true && currentStamina > 0.5 && yValue > Settings.options.InsuThreshold)
            {
                InterfaceManager.GetPanel<Panel_HUD>().m_SprintBar.alpha = 1f;
                InterfaceManager.GetPanel<Panel_HUD>().m_SprintFadeTimeTracker = 2;
                GameManager.GetPlayerMovementComponent().AddSprintStamina(-Settings.options.InsuStaminaWalkingBurn);
            }
            else if (isWalking == false && isSprinting == false && yValue > Settings.options.InsuThreshold)
            {
                InterfaceManager.GetPanel<Panel_HUD>().m_SprintBar_SecondsBeforeFadeOut = 2;
            }
            else if (isWalking == true && currentStamina <= 0.5 && yValue > Settings.options.InsuThreshold)
            {
                GameManager.m_Condition.m_CurrentHP -= (Settings.options.ConditionLostZeroStamina / 10);
            }
        }

        private void ApplyAltitudeEffects(float yValue)
        {
            AltitudeState newState;

            if (yValue >= Settings.options.InsuThreshold) newState = AltitudeState.TooWeak;
            else if (yValue >= Settings.options.CritThreshold) newState = AltitudeState.HeavyWeakened;
            else if (yValue >= Settings.options.LowThreshold) newState = AltitudeState.Weakened;
            else newState = AltitudeState.Normal;

            if (newState != currentState)
            {
                currentState = newState;
                float staminaMultiplier = 1f;
                float staminaConsumptionMultiplier = 1f;
                float maxFatigueBurnMultiplier = 1f;
                float minFatigueBurnMultiplier = 1f;
                float fireIgnitionMultiplier = 1f;
                float secondsBeforeRecovStamMultiplier = 1f;
                float dysenteryRecoveryTimeMultiplier = 1f;
                float foodPoisoningRecoveryTimeMultiplier = 1f;

                switch (currentState)
                {
                    case AltitudeState.Weakened:
                        staminaMultiplier = Settings.options.LowStaminaMultiplier;
                        staminaConsumptionMultiplier = Settings.options.LowStaminaConsumptionMultiplier;
                        maxFatigueBurnMultiplier = Settings.options.LowMaxFatigueBurnMultiplier;
                        minFatigueBurnMultiplier = Settings.options.LowMinFatigueBurnMultiplier;
                        fireIgnitionMultiplier = Settings.options.LowFireIgnitionMultiplier;
                        secondsBeforeRecovStamMultiplier = Settings.options.LowSecondsBeforeRecovStamMultiplier;
                        dysenteryRecoveryTimeMultiplier = Settings.options.LowDysenteryRecoveryTimeMultiplier;
                        foodPoisoningRecoveryTimeMultiplier = Settings.options.LowFoodPoisoningRecoveryTimeMultiplier;
                        HUDMessage.AddMessage(Localization.Get("GAMEPLAY_LowWarning"), 5, false);
                        break;
                    case AltitudeState.HeavyWeakened:
                        if (isAcclimatized)
                        {
                            staminaMultiplier = (Settings.options.CritStaminaMultiplier * 1.5f);
                            staminaConsumptionMultiplier = (Settings.options.CritStaminaConsumptionMultiplier * 0.5f);
                            maxFatigueBurnMultiplier = (Settings.options.CritMaxFatigueBurnMultiplier * 0.5f);
                            minFatigueBurnMultiplier = (Settings.options.CritMinFatigueBurnMultiplier * 0.5f);
                            fireIgnitionMultiplier = (Settings.options.CritFireIgnitionMultiplier * 0.5f);
                            secondsBeforeRecovStamMultiplier = (Settings.options.CritSecondsBeforeRecovStamMultiplier * 0.5f);
                            dysenteryRecoveryTimeMultiplier = (Settings.options.CritDysenteryRecoveryTimeMultiplier * 0.5f);
                            foodPoisoningRecoveryTimeMultiplier = (Settings.options.CritFoodPoisoningRecoveryTimeMultiplier * 0.5f);
                        }
                        else
                        {
                            staminaMultiplier = Settings.options.CritStaminaMultiplier;
                            staminaConsumptionMultiplier = Settings.options.CritStaminaConsumptionMultiplier;
                            maxFatigueBurnMultiplier = Settings.options.CritMaxFatigueBurnMultiplier;
                            minFatigueBurnMultiplier = Settings.options.CritMinFatigueBurnMultiplier;
                            fireIgnitionMultiplier = Settings.options.CritFireIgnitionMultiplier;
                            secondsBeforeRecovStamMultiplier = Settings.options.CritSecondsBeforeRecovStamMultiplier;
                            dysenteryRecoveryTimeMultiplier = Settings.options.CritDysenteryRecoveryTimeMultiplier;
                            foodPoisoningRecoveryTimeMultiplier = Settings.options.CritFoodPoisoningRecoveryTimeMultiplier;
                            HUDMessage.AddMessage(Localization.Get("GAMEPLAY_CriticalWarning"), 5, false);
                        }
                        break;
                    case AltitudeState.TooWeak:
                        if (isAcclimatized)
                        {
                            staminaMultiplier = ((Settings.options.InsuStaminaMultiplier / 10) * 1.5f);
                            staminaConsumptionMultiplier = (Settings.options.InsuStaminaConsumptionMultiplier * 0.5f);
                            maxFatigueBurnMultiplier = (Settings.options.InsuMaxFatigueBurnMultiplier * 0.5f);
                            minFatigueBurnMultiplier = (Settings.options.InsuMinFatigueBurnMultiplier * 0.5f);
                            fireIgnitionMultiplier = (Settings.options.InsuFireIgnitionMultiplier * 0.5f);
                            secondsBeforeRecovStamMultiplier = (Settings.options.InsuSecondsBeforeRecovStamMultiplier * 0.5f);
                            dysenteryRecoveryTimeMultiplier = (Settings.options.InsuDysenteryRecoveryTimeMultiplier * 0.5f);
                            foodPoisoningRecoveryTimeMultiplier = (Settings.options.InsuFoodPoisoningRecoveryTimeMultiplier * 0.5f);
                        }
                        else
                        {
                            staminaMultiplier = (Settings.options.InsuStaminaMultiplier / 10);
                            staminaConsumptionMultiplier = Settings.options.InsuStaminaConsumptionMultiplier;
                            maxFatigueBurnMultiplier = Settings.options.InsuMaxFatigueBurnMultiplier;
                            minFatigueBurnMultiplier = Settings.options.InsuMinFatigueBurnMultiplier;
                            fireIgnitionMultiplier = Settings.options.InsuFireIgnitionMultiplier;
                            secondsBeforeRecovStamMultiplier = Settings.options.InsuSecondsBeforeRecovStamMultiplier;
                            dysenteryRecoveryTimeMultiplier = Settings.options.InsuDysenteryRecoveryTimeMultiplier;
                            foodPoisoningRecoveryTimeMultiplier = Settings.options.InsuFoodPoisoningRecoveryTimeMultiplier;
                            HUDMessage.AddMessage(Localization.Get("GAMEPLAY_InsufficientWarning"), 5, false);
                        }
                        break;
                    case AltitudeState.Normal:
                        staminaMultiplier = 1f;
                        staminaConsumptionMultiplier = 1f;
                        maxFatigueBurnMultiplier = 1f;
                        minFatigueBurnMultiplier = 1f;
                        fireIgnitionMultiplier = 1f;
                        secondsBeforeRecovStamMultiplier = 1f;
                        dysenteryRecoveryTimeMultiplier = 1f;
                        foodPoisoningRecoveryTimeMultiplier = 1f;
                        HUDMessage.AddMessage(Localization.Get("GAMEPLAY_NormalizedWarning"), 5, false);
                        break;
                }
                GameManager.GetPlayerMovementComponent().m_SprintStaminaRecoverPerHour = baseStaminaRegenRate * staminaMultiplier;
                GameManager.GetPlayerMovementComponent().m_SprintStaminaUsagePerSecond = baseStaminaSprintUsageRate * staminaConsumptionMultiplier;
                GameManager.GetFatigueComponent().m_FatigueIncreasePerHourSprintingMax = baseMaxFatigueSprintUsageRate * maxFatigueBurnMultiplier;
                GameManager.GetFatigueComponent().m_FatigueIncreasePerHourSprintingMin = baseMinFatigueSprintUsageRate * minFatigueBurnMultiplier;
                GameManager.m_FireManager.m_StartFireTimeSeconds = defaultFireIgnitionTime * fireIgnitionMultiplier;
                GameManager.GetPlayerMovementComponent().m_SecondsNotSprintingBeforeRecovery = baseDelayRecoverStam * secondsBeforeRecovStamMultiplier;
                GameManager.GetDysenteryComponent().m_DurationHoursMax = baseDysenteryMaxRecoveryTime * dysenteryRecoveryTimeMultiplier;
                GameManager.GetDysenteryComponent().m_DurationHoursMin = baseDysenteryMinRecoveryTime * dysenteryRecoveryTimeMultiplier;
                GameManager.GetFoodPoisoningComponent().m_DurationHoursMax = baseFoodPoisoningMaxRecoveryTime * foodPoisoningRecoveryTimeMultiplier;
                GameManager.GetFoodPoisoningComponent().m_DurationHoursMin = baseFoodPoisoningMinRecoveryTime * foodPoisoningRecoveryTimeMultiplier;
                
                //MelonLogger.Msg("default : " + GameManager.GetConditionComponent().m_HPIncreasePerDayWhileHealthy);
            }
        }
    }
}