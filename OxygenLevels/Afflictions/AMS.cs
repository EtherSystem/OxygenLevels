using AfflictionComponent.Components;
using AfflictionComponent.Enums;
using AfflictionComponent.Interfaces;
using OxygenLevels.Resources.Localization;

namespace OxygenLevels.Afflictions
{
    internal class AMS
    {
        public class AMSAffliction : CustomAffliction, IRemedies, IInstance, ILocalizableAffliction
        {
            private const string NAME_KEY = "GAMEPLAY_AMSName";
            private const string CAUSE_KEY = "GAMEPLAY_AMSCause";
            private const string DESC_KEY = "GAMEPLAY_AMSDescription";

            private const string ICON = "OxygenLevels.Resources.Icons.Classic.AMS.png";
            private const string ALT_ICON = "OxygenLevels.Resources.Icons.Alt.AMS_ALT.png";

            private float m_LastWholeMinute = -1f;

            private const float DRAIN_LOG_INTERVAL_MINUTES = 60f;
            private float m_DrainLogHpLoss = 0f;
            private float m_DrainLogMinutes = 0f;

            public InstanceType Type { get; set; } = InstanceType.Single;

            public static bool IsAMSActive { get; private set; }

            public Tuple<string, int, int>[] RemedyItems { get; set; } = [];
            public Tuple<string, int, int>[] AltRemedyItems { get; set; } = [];

            public bool InstantHeal { get; set; } = true;

            public AMSAffliction(AfflictionBodyArea bodyArea) : base(NAME_KEY, CAUSE_KEY, DESC_KEY, null, UnityEngine.Random.Range(0f, 100f) < Settings.options.AltAfflictionIconChance ? ALT_ICON : ICON, bodyArea, true)
            {
            }

            public void OnFoundExistingInstance(CustomAffliction existingAffliction)
            {
                return;
            }

            public void CureSymptoms()
            {
                // cure symptoms but not the affliction
            }

            public void OnCure()
            {
                FlushDrainLog();

                var cameraStatus = GameManager.GetCameraStatusEffects();
                if (cameraStatus != null)
                {
                    cameraStatus.m_TriggerHeadachePulse = false;
                    cameraStatus.m_TriggerSuffocationPulse = false;
                }

                IsAMSActive = false;
                m_LastWholeMinute = -1f;
            }

            public override void OnUpdate()
            {
                IsAMSActive = true;

                var cameraStatus = GameManager.GetCameraStatusEffects();
                if (cameraStatus != null)
                {
                    cameraStatus.m_TriggerHeadachePulse = false;
                    cameraStatus.m_TriggerSuffocationPulse = true;
                }

                ApplyHealthDrain();
            }

            private static float GetCurrentWholeMinute()
            {
                TimeOfDay tod = GameManager.GetTimeOfDayComponent();
                if (tod == null) return 0f;

                return Mathf.Floor(tod.GetHoursPlayedNotPaused() * 60f);
            }

            private void ApplyHealthDrain()
            {
                Condition condition = GameManager.GetConditionComponent();
                if (condition == null || condition.m_CurrentHP <= 0f) return;

                float currentMinute = GetCurrentWholeMinute();

                if (m_LastWholeMinute < 0f)
                {
                    m_LastWholeMinute = currentMinute;
                    return;
                }

                float minuteDelta = currentMinute - m_LastWholeMinute;
                if (minuteDelta <= 0f) return;

                m_LastWholeMinute = currentMinute;

                float conditionLossPerHour = Core.GetAMSConditionLossPerHour();
                float conditionLoss = (conditionLossPerHour / 60f) * minuteDelta;
                float appliedLoss = Core.ApplyChunkedConditionDrain(conditionLoss, DamageSource.Unspecified);

                if (appliedLoss <= 0f) return;

                m_DrainLogHpLoss += appliedLoss;
                m_DrainLogMinutes += minuteDelta;

                if (m_DrainLogMinutes >= DRAIN_LOG_INTERVAL_MINUTES) FlushDrainLog();
            }

            private void FlushDrainLog()
            {
                if (m_DrainLogMinutes <= 0f || m_DrainLogHpLoss <= 0f) return;

                Core.Log($"AMS drain: {m_DrainLogHpLoss:0.###} HP over {m_DrainLogMinutes:0} min.");

                m_DrainLogHpLoss = 0f;
                m_DrainLogMinutes = 0f;
            }

            public void RefreshLocalization()
            {
                string oldName = m_Name;

                m_Name = Localization.Get(NAME_KEY);
                m_CauseText = Localization.Get(CAUSE_KEY);
                m_Description = Localization.Get(DESC_KEY);
                m_DescriptionNoHeal = null;

                Core.Log($"AMS refresh -> '{oldName}' => '{m_Name}'");
            }
        }
    }
}