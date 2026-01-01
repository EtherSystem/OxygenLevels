using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(OxygenLevels.Core), "OxygenLevels", "1.0.0", "EtherSystem", null)]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace OxygenLevels
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }
        public const float MaxThreshold = 1200f;
        private float updateTimer = 0f;
        private float baseStaminaRegenRate = 500f;
        private float baseStaminaSprintUsageRate = 5f;
        private float baseMaxFatigueSprintUsageRate = 150f;
        private float baseMinFatigueSprintUsageRate = 1f;

        private enum AltitudeState { Normal, Weakened, HeavyWeakened, TooWeak }
        private AltitudeState currentState = AltitudeState.Normal;

        public override void OnUpdate()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer < 1.0f) return;
            updateTimer = 0f;

            if (GameManager.GetPlayerTransform() == null) return;

            float yValue = GameManager.GetPlayerTransform().position.y;
            ApplyAltitudeEffects(yValue);
        }

        private void ApplyAltitudeEffects(float yValue)
        {
            AltitudeState newState;

            if (yValue >= 580f) newState = AltitudeState.TooWeak;
            else if (yValue >= 460f) newState = AltitudeState.HeavyWeakened;
            else if (yValue >= 360f) newState = AltitudeState.Weakened;
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
                        staminaMultiplier = 0.6f;
                        staminaConsumptionMultiplier = 1.5f;
                        maxFatigueBurnMultiplier = 4f;
                        minFatigueBurnMultiplier = 4f;
                        HUDMessage.AddMessage("Low oxygen - You feel weak", 5, false);
                        break;
                    case AltitudeState.HeavyWeakened:
                        staminaMultiplier = 0.3f;
                        staminaConsumptionMultiplier = 2.5f;
                        maxFatigueBurnMultiplier = 10f;
                        minFatigueBurnMultiplier = 10f;
                        //headache visual effect
                        HUDMessage.AddMessage("Critical oxygen - You are seriously weakened", 5, false);
                        break;
                    case AltitudeState.TooWeak:
                        staminaMultiplier = 0.01f;
                        staminaConsumptionMultiplier = 3f;
                        maxFatigueBurnMultiplier = 20f;
                        minFatigueBurnMultiplier = 20f;
                        HUDMessage.AddMessage("You are far too weak...", 5, false);
                        break;

                    case AltitudeState.Normal:
                        staminaMultiplier = 1f;
                        staminaConsumptionMultiplier = 1f;
                        maxFatigueBurnMultiplier = 1f;
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