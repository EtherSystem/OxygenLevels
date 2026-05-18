using AfflictionComponent.Components;
using AfflictionComponent.Enums;
using AfflictionComponent.Interfaces;
using OxygenLevels.Resources.Localization;

namespace OxygenLevels.Afflictions
{
    public class CriticalAcclimatizedBuff : CustomAffliction, IInstance, IBuff, ILocalizableAffliction
    {
        private const string NAME_KEY = "GAMEPLAY_AcclimatizedIBuffName";
        private const string CAUSE_KEY = "GAMEPLAY_AcclimatizedIBuffCause";
        private const string DESC_KEY = "GAMEPLAY_AcclimatizedIBuffDescription";

        private const string ICON = "OxygenLevels.Resources.Icons.Classic.AcclimatizedI.png";
        private const string ALT_ICON = "OxygenLevels.Resources.Icons.Alt.AcclimatizedI_ALT.png";

        public static bool IsActive { get; private set; }

        public InstanceType Type { get; set; } = InstanceType.Single;
        public bool Buff { get; set; } = true;
        public bool BuffCold { get; set; }
        public bool BuffFatigue { get; set; }
        public bool BuffHunger { get; set; }
        public bool BuffThirst { get; set; }

        public CriticalAcclimatizedBuff() : base(NAME_KEY, CAUSE_KEY, DESC_KEY, null, UnityEngine.Random.Range(0f, 100f) < Settings.options.AltAfflictionIconChance ? ALT_ICON : ICON, AfflictionBodyArea.Head, true)
        {
        }

        public void OnFoundExistingInstance(CustomAffliction existing)
        {
            IsActive = true;
        }

        public override void OnUpdate()
        {
            IsActive = true;
        }

        public void OnCure()
        {
            IsActive = false;
        }

        public void RefreshLocalization()
        {
            string oldName = m_Name;

            m_Name = Localization.Get(NAME_KEY);
            m_CauseText = Localization.Get(CAUSE_KEY);
            m_Description = Localization.Get(DESC_KEY);
            m_DescriptionNoHeal = null;

            Core.Log($"CriticalAcclimatizedBuff refresh -> '{oldName}' => '{m_Name}'");
        }
    }
}