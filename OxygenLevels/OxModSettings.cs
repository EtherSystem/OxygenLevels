using Description = ModSettings.DescriptionAttribute;
using System.ComponentModel;

namespace OxygenLevels
{
    internal class OxModSettings : JsonModSettings
    {
        // HUD
        [Section("HUD Settings")]

        [Name("Using InterloperHUDpro ?")]
        [Description("prevents HUDs from overlapping.")]
        public bool interHUD = false;

        // Acclimatization
        [Section("Acclimatization")]

        [Name("Time needed to acclimatize")]
        [Description("Base = 24 - ingame hours")]
        [Slider(1, 72)]
        public float AcclimatizationTimer = 24f;

        // O₂ levels
        [Section("O₂ Effects")]

        [Name("Low o₂")]
        [Description("Show or hide Low o₂ settings.")]
        [Choice("+", "-")]
        public bool LowO2 = false;

        [Name("Altitude threshold")]
        [Description("Base = 360")]
        [Slider(0, 700)]
        public float LowThreshold = 360f;

        [Name("Stamina recovery speed")]
        [Description("Base = 0.6")]
        [Slider(0.1f, 1)]
        public float LowStaminaMultiplier = 0.6f;

        [Name("Stamina consumption speed")]
        [Description("Base = 1.5")]
        [Slider(1.5f, 10)]
        public float LowStaminaConsumptionMultiplier = 1.5f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base = 4")]
        [Slider(2, 100)]
        public float LowMinFatigueBurnMultiplier = 4f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base = 4")]
        [Slider(2, 100)]
        public float LowMaxFatigueBurnMultiplier = 4f;

        [Name("Fire ignition multiplier")]
        [Description("Base = 2")]
        [Slider(2, 10)]
        public float LowFireIgnitionMultiplier = 2f;

        [Name("Seconds before recovering stamina multiplier")]
        [Description("Base = 2")]
        [Slider(2, 20)]
        public float LowSecondsBeforeRecovStamMultiplier = 2f;

        [Name("Dysentery recovery time multiplier")]
        [Description("Base = 1.5")]
        [Slider(1.5f, 20)]
        public float LowDysenteryRecoveryTimeMultiplier = 1.5f;

        [Name("Food poisoning recovery time multiplier")]
        [Description("Base = 1.5")]
        [Slider(1.5f, 20)]
        public float LowFoodPoisoningRecoveryTimeMultiplier = 1.5f;


        [Name("Critical o₂")]
        [Description("Show or hide Critical o₂ settings.")]
        [Choice("+", "-")]
        public bool CriticalO2 = false;

        [Name("Altitude threshold")]
        [Description("Base = 460")]
        [Slider(0, 750)]
        public float CritThreshold = 460f;

        [Name("Stamina recovery speed")]
        [Description("Base = 0.3")]
        [Slider(0.1f, 1)]
        public float CritStaminaMultiplier = 0.3f;

        [Name("Stamina consumption speed")]
        [Description("Base = 2.5")]
        [Slider(2, 10)]
        public float CritStaminaConsumptionMultiplier = 2.5f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base = 10")]
        [Slider(2, 100)]
        public float CritMinFatigueBurnMultiplier = 10f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base = 10")]
        [Slider(2, 100)]
        public float CritMaxFatigueBurnMultiplier = 10f;

        [Name("Fire ignition multiplier")]
        [Description("Base = 3")]
        [Slider(2, 10)]
        public float CritFireIgnitionMultiplier = 3f;

        [Name("Seconds before recovering stamina multiplier")]
        [Description("Base = 3")]
        [Slider(2, 20)]
        public float CritSecondsBeforeRecovStamMultiplier = 3f;

        [Name("Dysentery recovery time multiplier")]
        [Description("Base = 2")]
        [Slider(2, 20)]
        public float CritDysenteryRecoveryTimeMultiplier = 2f;

        [Name("Food poisoning recovery time multiplier")]
        [Description("Base = 2")]
        [Slider(2, 20)]
        public float CritFoodPoisoningRecoveryTimeMultiplier = 2f;


        [Name("Insufficient o₂")]
        [Description("Show or hide Insufficient o₂ settings.")]
        [Choice("+", "-")]
        public bool InsufficientO2 = false;

        [Name("Altitude threshold")]
        [Description("Base = 580")]
        [Slider(0, 800)]
        public float InsuThreshold = 580f;

        [Name("Stamina recovery speed")]
        [Description("Base = 0.1 / 10")]
        [Slider(0.1f, 10.00f, 99)]
        public float InsuStaminaMultiplier = 0.1f;

        [Name("Stamina consumption speed")]
        [Description("Base = 3")]
        [Slider(2, 10)]
        public float InsuStaminaConsumptionMultiplier = 3f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base = 20")]
        [Slider(2, 100)]
        public float InsuMinFatigueBurnMultiplier = 20f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base = 20")]
        [Slider(2, 100)]
        public float InsuMaxFatigueBurnMultiplier = 20f;

        [Name("Fire ignition multiplier")]
        [Description("Base = 5")]
        [Slider(2, 10)]
        public float InsuFireIgnitionMultiplier = 5f;

        [Name("Seconds before recovering stamina multiplier")]
        [Description("Base = 5")]
        [Slider(2, 20)]
        public float InsuSecondsBeforeRecovStamMultiplier = 5f;

        [Name("Dysentery recovery time multiplier")]
        [Description("Base = 3")]
        [Slider(2, 20)]
        public float InsuDysenteryRecoveryTimeMultiplier = 3f;

        [Name("Food poisoning recovery time multiplier")]
        [Description("Base = 3")]
        [Slider(2, 20)]
        public float InsuFoodPoisoningRecoveryTimeMultiplier = 3f;

        [Name("Stamina consumption when walking")]
        [Description("Base = 0.1")]
        [Slider(0.1f, 10, 99)]
        public float InsuStaminaWalkingBurn = 0.1f;

        [Name("Condition lost when walking with no stamina")]
        [Description("Base = 0.5 / 10")]
        [Slider(0.5f, 10f, 95)]
        public float ConditionLostZeroStamina = 0.5f;

        [Name("AMS appearance time")]
        [Description("Base = 2 - ingame hours")]
        [Slider(1, 10)]
        public float AMSAppeanceTime = 2f;

        [Name("AMS disappearance time")]
        [Description("Base = 2 - ingame hours - If set to 1, the disappearance time will be the same as the appearance time. Set to 2, the disappearance time will be multiplied by 2, etc.")]
        [Slider(1, 10)]
        public float AMSDisappearanceTime = 2f;


        internal void RefreshFields()
        {
            // Low o₂
            SetFieldVisible(nameof(LowThreshold), LowO2);
            SetFieldVisible(nameof(LowStaminaMultiplier), LowO2);
            SetFieldVisible(nameof(LowStaminaConsumptionMultiplier), LowO2);
            SetFieldVisible(nameof(LowMinFatigueBurnMultiplier), LowO2);
            SetFieldVisible(nameof(LowMaxFatigueBurnMultiplier), LowO2);
            SetFieldVisible(nameof(LowFireIgnitionMultiplier), LowO2);
            SetFieldVisible(nameof(LowSecondsBeforeRecovStamMultiplier), LowO2);
            SetFieldVisible(nameof(LowDysenteryRecoveryTimeMultiplier), LowO2);
            SetFieldVisible(nameof(LowFoodPoisoningRecoveryTimeMultiplier), LowO2);

            // Critical o₂
            SetFieldVisible(nameof(CritThreshold), CriticalO2);
            SetFieldVisible(nameof(CritStaminaMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritStaminaConsumptionMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritMinFatigueBurnMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritMaxFatigueBurnMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritFireIgnitionMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritSecondsBeforeRecovStamMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritDysenteryRecoveryTimeMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritFoodPoisoningRecoveryTimeMultiplier), CriticalO2);

            // Insufficient o₂
            SetFieldVisible(nameof(InsuThreshold), InsufficientO2);
            SetFieldVisible(nameof(InsuStaminaMultiplier), InsufficientO2);
            SetFieldVisible(nameof(InsuStaminaConsumptionMultiplier), InsufficientO2);
            SetFieldVisible(nameof(InsuMinFatigueBurnMultiplier), InsufficientO2);
            SetFieldVisible(nameof(InsuMaxFatigueBurnMultiplier), InsufficientO2);
            SetFieldVisible(nameof(InsuFireIgnitionMultiplier), InsufficientO2);
            SetFieldVisible(nameof(InsuSecondsBeforeRecovStamMultiplier), InsufficientO2);
            SetFieldVisible(nameof(InsuDysenteryRecoveryTimeMultiplier), InsufficientO2);
            SetFieldVisible(nameof(InsuFoodPoisoningRecoveryTimeMultiplier), InsufficientO2);
            SetFieldVisible(nameof(InsuStaminaWalkingBurn), InsufficientO2);
            SetFieldVisible(nameof(ConditionLostZeroStamina), InsufficientO2);
            SetFieldVisible(nameof(AMSAppeanceTime), InsufficientO2);
            SetFieldVisible(nameof(AMSDisappearanceTime), InsufficientO2);
        }

        protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
        {
            RefreshFields();
        }

        protected override void OnConfirm()
        {
            base.OnConfirm();
            Core.isInterHUD = interHUD;

            Settings.RequestAltitudeHUDReposition = true;
        }
    }

    internal static class Settings
    {
        public static OxModSettings options;
        public static bool RequestAltitudeHUDReposition;

        public static void OnLoad()
        {
            options = new OxModSettings();
            options.AddToModSettings("OxygenLevels");

            options.RefreshFields();

            Core.isInterHUD = options.interHUD;
        }
    }
}