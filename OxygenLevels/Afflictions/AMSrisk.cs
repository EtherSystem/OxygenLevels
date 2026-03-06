using static OxygenLevels.Afflictions.AMS;
using AfflictionComponent.Components;
using AfflictionComponent.Interfaces;
using AfflictionComponent.Enums;

namespace OxygenLevels.Afflictions
{
    internal class AMSrisk
    {
        public class AMSriskAffliction : CustomAffliction, IRemedies, IInstance, IRiskPercentage
        {
            public InstanceType Type { get; set; } = InstanceType.Single;
            public void OnFoundExistingInstance(CustomAffliction existingAffliction)
            {
                return;
            }

            private float m_RiskValue = 0f;
            private float m_LastUpdateTime;
            public bool Risk { get; set; } = true;

            public Tuple<string, int, int>[] RemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();
            public Tuple<string, int, int>[] AltRemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();

            public bool InstantHeal { get; set; } = true;

            public AMSriskAffliction(AfflictionBodyArea bodyArea) : base("GAMEPLAY_AMSriskName", "GAMEPLAY_AMSCause", "GAMEPLAY_AMSriskDescription", null, "OxygenLevels.Resources.Icons.AMSrisk.png", bodyArea, true)
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
                        return;
                    }
                    if (GetRiskValue() < 0f)
                    {
                        Cure();
                        return;
                    }
                    UpdateRiskValue();
                    cameraStatus.m_TriggerHeadachePulse = true;
                    cameraStatus.m_TriggerSuffocationPulse = false;
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
        }
    }
}