using Il2Cpp;
using Il2CppRewired;
using Il2CppRewired.ComponentControls.Data;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(OxygenLevels.Core), "OxygenLevels", "1.1.2", "EtherSystem", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace OxygenLevels
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
            Settings.OnLoad();
        }
        public const float MaxThreshold = 1200f;
        private float updateTimer = 0f;
        private float baseStaminaRegenRate = 500f;
        private float baseStaminaSprintUsageRate = 5f;
        private float baseMaxFatigueSprintUsageRate = 150f;
        private float baseMinFatigueSprintUsageRate = 1f;
        
        public enum AltitudeState { Normal, Weakened, HeavyWeakened, TooWeak }
        public static AltitudeState currentState = AltitudeState.Normal;

        public override void OnUpdate()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer < 1.0f) return;
            updateTimer = 0f;

            if (GameManager.GetPlayerTransform() == null) return;

            float yValue = GameManager.GetPlayerTransform().position.y;
            ApplyAltitudeEffects(yValue);

            var isWalking = GameManager.GetPlayerManagerComponent().PlayerIsWalking();
            var isSprinting = GameManager.GetPlayerManagerComponent().PlayerIsSprinting();
            var currentStamina = GameManager.GetPlayerMovementComponent().CurrentStamina;


            if (isWalking == true && currentStamina > 0 && yValue > Settings.options.InsuThreshold)
            {
                InterfaceManager.GetPanel<Panel_HUD>().m_SprintBar.alpha = 1f;
                InterfaceManager.GetPanel<Panel_HUD>().m_SprintFadeTimeTracker = 2;
                GameManager.GetPlayerMovementComponent().AddSprintStamina(-Settings.options.InsuStaminaWalkingBurn);
            }
            else if (isWalking == false && isSprinting == false && yValue > Settings.options.InsuThreshold)
            {
                InterfaceManager.GetPanel<Panel_HUD>().m_SprintBar_SecondsBeforeFadeOut = 2;
            }
            else if (isWalking == true && currentStamina == 1 && yValue > Settings.options.InsuThreshold)
            {
                GameManager.m_Condition.m_CurrentHP -= (Settings.options.ConditionLostZeroStamina / 10);
            }
        }

        private void ApplyAltitudeEffects(float yValue)
        {
            AltitudeState newState;

            if (yValue >= Settings.options.InsuThreshold) newState = AltitudeState.TooWeak;
            else if (yValue >= Settings.options.CritThreshold) newState = AltitudeState.HeavyWeakened;
            else if (yValue >= Settings.options.lowThreshold) newState = AltitudeState.Weakened;
            else newState = AltitudeState.Normal;

            if (newState != currentState)
            {
                currentState = newState;
                float staminaMultiplier = 1f;
                float staminaConsumptionMultiplier = 1f;
                float maxFatigueBurnMultiplier = 1f;
                float minFatigueBurnMultiplier = 1f;

                switch (currentState)
                {
                    case AltitudeState.Weakened:
                        staminaMultiplier = Settings.options.lowStaminaMultiplier;
                        staminaConsumptionMultiplier = Settings.options.lowStaminaConsumptionMultiplier;
                        maxFatigueBurnMultiplier = Settings.options.lowMaxFatigueBurnMultiplier;
                        minFatigueBurnMultiplier = Settings.options.lowMinFatigueBurnMultiplier;
                        HUDMessage.AddMessage("Low oxygen - You feel weak", 5, false);
                        break;
                    case AltitudeState.HeavyWeakened:
                        staminaMultiplier = Settings.options.CritStaminaMultiplier;
                        staminaConsumptionMultiplier = Settings.options.CritStaminaConsumptionMultiplier;
                        maxFatigueBurnMultiplier = Settings.options.CritMaxFatigueBurnMultiplier;
                        minFatigueBurnMultiplier = Settings.options.CritMinFatigueBurnMultiplier;
                        //GameManager.GetCameraStatusEffects().m_TriggerHeadachePulse = true;

                        
                        HUDMessage.AddMessage("Critical oxygen - You are seriously weakened", 5, false);
                        break;
                    case AltitudeState.TooWeak:
                        staminaMultiplier = (Settings.options.InsuStaminaMultiplier / 10);
                        staminaConsumptionMultiplier = Settings.options.InsuStaminaConsumptionMultiplier;
                        maxFatigueBurnMultiplier = Settings.options.InsuMaxFatigueBurnMultiplier;
                        minFatigueBurnMultiplier = Settings.options.InsuMinFatigueBurnMultiplier;
                        HUDMessage.AddMessage("You are far too weak...", 5, false);
                        break;
                    case AltitudeState.Normal:
                        staminaMultiplier = 1f;
                        staminaConsumptionMultiplier = 1f;
                        maxFatigueBurnMultiplier = 1f;
                        minFatigueBurnMultiplier = 1f;
                        HUDMessage.AddMessage("Oxygen level stabilized", 5, false);
                        break;
                }
                GameManager.GetPlayerMovementComponent().m_SprintStaminaRecoverPerHour = baseStaminaRegenRate * staminaMultiplier;
                GameManager.GetPlayerMovementComponent().m_SprintStaminaUsagePerSecond = baseStaminaSprintUsageRate * staminaConsumptionMultiplier;
                GameManager.GetFatigueComponent().m_FatigueIncreasePerHourSprintingMax = baseMaxFatigueSprintUsageRate * maxFatigueBurnMultiplier;
                GameManager.GetFatigueComponent().m_FatigueIncreasePerHourSprintingMin = baseMinFatigueSprintUsageRate * minFatigueBurnMultiplier;
            }
        }
    }
}