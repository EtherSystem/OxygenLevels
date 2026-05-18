using AfflictionComponent.Components;
using AfflictionComponent.Enums;
using AfflictionComponent.Interfaces;
using OxygenLevels.Resources.Localization;
using static OxygenLevels.Afflictions.AMS;

namespace OxygenLevels.Afflictions
{
    internal class AMSrisk
    {
        public class AMSriskAffliction : CustomAffliction, IRemedies, IInstance, IRiskPercentage, ILocalizableAffliction
        {
            private const string NAME_KEY = "GAMEPLAY_AMSriskName";
            private const string CAUSE_KEY = "GAMEPLAY_AMSCause";
            private const string DESC_KEY = "GAMEPLAY_AMSriskDescription";

            private const string ICON = "OxygenLevels.Resources.Icons.Classic.AMSrisk.png";
            private const string ALT_ICON = "OxygenLevels.Resources.Icons.Alt.AMSrisk_ALT.png";

            public InstanceType Type { get; set; } = InstanceType.Single;
            public void OnFoundExistingInstance(CustomAffliction existingAffliction)
            {
                return;
            }

            private float m_RiskValue = 0f;
            private float m_LastUpdateTime;
            public bool Risk { get; set; } = true;

            public Tuple<string, int, int>[] RemedyItems { get; set; } = [];
            public Tuple<string, int, int>[] AltRemedyItems { get; set; } = [];

            public bool InstantHeal { get; set; } = true;

            public AMSriskAffliction(AfflictionBodyArea bodyArea) : base(NAME_KEY, CAUSE_KEY, DESC_KEY, null, UnityEngine.Random.Range(0f, 100f) < Settings.options.AltAfflictionIconChance ? ALT_ICON : ICON, bodyArea, true)
            {
                m_LastUpdateTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            }

            public void CureSymptoms()
            {
                //cure symptoms but not the affliction
            }

            public void OnCure()
            {
                //when the affliction is cured, apply this code
            }

            public float GetRiskValue() => m_RiskValue;

            public override void OnUpdate()
            {
                if (Risk)
                {
                    var cameraStatus = GameManager.GetCameraStatusEffects();

                    if (GetRiskValue() >= 100)
                    {
                        Cure(false);
                        new AMSAffliction(AfflictionBodyArea.Head).Start();
                        Core.OnAMSApplied();
                        return;
                    }
                    if (GetRiskValue() < 0f)
                    {
                        Cure();
                        return;
                    }
                    UpdateRiskValue();

                    if (cameraStatus != null)
                    {
                        cameraStatus.m_TriggerHeadachePulse = true;
                        cameraStatus.m_TriggerSuffocationPulse = false;
                    }
                }
            }
            public void UpdateRiskValue()
            {
                var currentTime = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
                var elapsedTime = currentTime - m_LastUpdateTime;

                var riskIncrease = elapsedTime * 60f;

                m_RiskValue = Mathf.Min(m_RiskValue + riskIncrease, 100f);
                m_LastUpdateTime = currentTime;

                // Mod.Logger.Log($"Risk for {m_AfflictionKey} increased to {m_RiskPercentage:F2}%", ComplexLogger.FlaggedLoggingLevel.Debug);
            }

            public void RefreshLocalization()
            {
                string oldName = m_Name;

                m_Name = Localization.Get(NAME_KEY);
                m_CauseText = Localization.Get(CAUSE_KEY);
                m_Description = Localization.Get(DESC_KEY);
                m_DescriptionNoHeal = null;

                Core.Log($"AMSrisk refresh -> '{oldName}' => '{m_Name}'");
            }
        }
    }
}