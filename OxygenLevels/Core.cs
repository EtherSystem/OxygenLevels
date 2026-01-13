using Il2Cpp;
using Il2CppRewired;
using Il2CppRewired.ComponentControls.Data;
using MelonLoader;
using UnityEngine;
using LocalizationUtilities;
using ModComponent;

[assembly: MelonInfo(typeof(OxygenLevels.Core), "OxygenLevels", "1.1.6", "EtherSystem", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace OxygenLevels
{
    public class Core : MelonMod
    {
        public static bool isInterHUD = false;
        private float timeSpentAtCritAltitude = 0f;
        private float SUFFOCATION_THRESHOLD_HOURS = 1f;
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
        private float baseStaminaRegenRate = 500f;
        private float baseStaminaSprintUsageRate = 5f;
        private float baseMaxFatigueSprintUsageRate = 150f;
        private float baseMinFatigueSprintUsageRate = 1f;
        private float defaultFireIgnitionTime = -1f;

        public enum AltitudeState { Normal, Weakened, HeavyWeakened, TooWeak }
        public static AltitudeState currentState = AltitudeState.Normal;

        public override void OnUpdate()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer < 1.0f) return;
            float realTimeElapsed = updateTimer;
            updateTimer = 0f;

            if (GameManager.GetPlayerTransform() == null) return;

            if (defaultFireIgnitionTime < 0f)
            {
                defaultFireIgnitionTime = GameManager.m_FireManager.m_StartFireTimeSeconds;
            }
            float yValue = GameManager.GetPlayerTransform().position.y;
            ApplyAltitudeEffects(yValue);
            float gameHoursPassed = GameManager.GetTimeOfDayComponent().GetTODHours(realTimeElapsed);

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

                switch (currentState)
                {
                    case AltitudeState.Weakened:
                        staminaMultiplier = Settings.options.LowStaminaMultiplier;
                        staminaConsumptionMultiplier = Settings.options.LowStaminaConsumptionMultiplier;
                        maxFatigueBurnMultiplier = Settings.options.LowMaxFatigueBurnMultiplier;
                        minFatigueBurnMultiplier = Settings.options.LowMinFatigueBurnMultiplier;
                        fireIgnitionMultiplier = Settings.options.LowFireIgnitionMultiplier;
                        HUDMessage.AddMessage(Localization.Get("GAMEPLAY_LowWarning"), 5, false);
                        break;
                    case AltitudeState.HeavyWeakened:
                        staminaMultiplier = Settings.options.CritStaminaMultiplier;
                        staminaConsumptionMultiplier = Settings.options.CritStaminaConsumptionMultiplier;
                        maxFatigueBurnMultiplier = Settings.options.CritMaxFatigueBurnMultiplier;
                        minFatigueBurnMultiplier = Settings.options.CritMinFatigueBurnMultiplier;
                        fireIgnitionMultiplier = Settings.options.CritFireIgnitionMultiplier;
                        HUDMessage.AddMessage(Localization.Get("GAMEPLAY_CriticalWarning"), 5, false);
                        break;
                    case AltitudeState.TooWeak:
                        staminaMultiplier = (Settings.options.InsuStaminaMultiplier / 10);
                        staminaConsumptionMultiplier = Settings.options.InsuStaminaConsumptionMultiplier;
                        maxFatigueBurnMultiplier = Settings.options.InsuMaxFatigueBurnMultiplier;
                        minFatigueBurnMultiplier = Settings.options.InsuMinFatigueBurnMultiplier;
                        fireIgnitionMultiplier = Settings.options.InsuFireIgnitionMultiplier;
                        HUDMessage.AddMessage(Localization.Get("GAMEPLAY_InsufficientWarning"), 5, false);
                        break;
                    case AltitudeState.Normal:
                        staminaMultiplier = 1f;
                        staminaConsumptionMultiplier = 1f;
                        maxFatigueBurnMultiplier = 1f;
                        minFatigueBurnMultiplier = 1f;
                        HUDMessage.AddMessage(Localization.Get("GAMEPLAY_NormalizedWarning"), 5, false);
                        break;
                }
                GameManager.GetPlayerMovementComponent().m_SprintStaminaRecoverPerHour = baseStaminaRegenRate * staminaMultiplier;
                GameManager.GetPlayerMovementComponent().m_SprintStaminaUsagePerSecond = baseStaminaSprintUsageRate * staminaConsumptionMultiplier;
                GameManager.GetFatigueComponent().m_FatigueIncreasePerHourSprintingMax = baseMaxFatigueSprintUsageRate * maxFatigueBurnMultiplier;
                GameManager.GetFatigueComponent().m_FatigueIncreasePerHourSprintingMin = baseMinFatigueSprintUsageRate * minFatigueBurnMultiplier;
                GameManager.m_FireManager.m_StartFireTimeSeconds = defaultFireIgnitionTime * fireIgnitionMultiplier;
            }
        }
    }
}