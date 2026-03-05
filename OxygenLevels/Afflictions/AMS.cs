using AfflictionComponent.Components;
using AfflictionComponent.Interfaces;
using AfflictionComponent.Enums;

namespace OxygenLevels.Afflictions
{
    internal class AMS
    {
        public class AMSAffliction : CustomAffliction, IRemedies, IInstance
        {
            public InstanceType Type { get; set; } = InstanceType.Single;
            public void OnFoundExistingInstance(CustomAffliction existingAffliction)
            {
                return;//MelonLogger.Msg("AMS duplication");
            }

            public static bool IsAMSActive { get; private set; } = false;

            public Tuple<string, int, int>[] RemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();
            public Tuple<string, int, int>[] AltRemedyItems { get; set; } = Array.Empty<Tuple<string, int, int>>();

            public bool InstantHeal { get; set; } = true;

            public AMSAffliction(AfflictionBodyArea bodyArea) : base("GAMEPLAY_AMSName", "GAMEPLAY_AMSCause", "GAMEPLAY_AMSDescription", null, "OxygenLevels.Resources.Icons.AMS.png", bodyArea, true)
            {
            }

            public void CureSymptoms()
            {
                //cure symptoms but not the affliction
            }

            public void OnCure()
            {
                var cameraStatus = GameManager.GetCameraStatusEffects();
                cameraStatus.m_TriggerHeadachePulse = false;
                cameraStatus.m_TriggerSuffocationPulse = false;
                IsAMSActive = false;
            }

            public override void OnUpdate()
            {
                IsAMSActive = true;
                var cameraStatus = GameManager.GetCameraStatusEffects();
                cameraStatus.m_TriggerHeadachePulse = false;
                cameraStatus.m_TriggerSuffocationPulse = true;
                return;
            }
        }
    }
}