using Description = ModSettings.DescriptionAttribute;
using System.ComponentModel;

namespace OxygenLevels
{
    internal class OxModSettings : JsonModSettings
    {
        // HUD
        [Section("HUD Settings")]

        [Name("Altitude HUD ?")]
        [Description("The altitude HUD is turned on by default.")]
        public bool ShowHUD = true;

        [Name("Customize altitude HUD")]
        [Description("Show customization settings for the altitude HUD.")]
        public bool ShowAltitudeHUDCustomization = false;

        [Name("Altitude HUD X position")]
        [Description("Moves the altitude display left or right. Default: -45.")]
        [Slider(-200, 2000, 2201, NumberFormat = "{0:0}")]
        public int AltitudeHudX = -45;

        [Name("Altitude HUD Y position")]
        [Description("Moves the altitude display up or down. Default: 127.")]
        [Slider(-150, 1005, 1156, NumberFormat = "{0:0}")]
        public int AltitudeHudY = 127;

        [Name("Altitude HUD size")]
        [Description("Controls the size of the altitude HUD text. Default: 32.")]
        [Slider(14, 48, 35, NumberFormat = "{0:0}")]
        public int AltitudeHudFontSize = 32;

        // Acclimatization
        [Section("Acclimatization")]

        [Name("Time needed to acclimatize")]
        [Description("Base: 24 in-game hours. Controls how long the player must stay exposed before gaining each acclimatization level.")]
        [Slider(1f, 72f, 143, NumberFormat = "{0:0.0}h")]
        public float AcclimatizationTimer = 24f;

        // O₂ levels
        [Section("O₂ Effects")]

        [Name("Low o₂")]
        [Description("Show or hide Low o₂ settings.")]
        [Choice("+", "-")]
        public bool LowO2 = false;

        [Name("Altitude threshold")]
        [Description("Base: 360m. Altitude where Low O₂ effects start.")]
        [Slider(0, 700, 701, NumberFormat = "{0:0}m")]
        public int LowThreshold = 360;

        [Name("Stamina recovery speed")]
        [Description("Base: 0.6x. Lower values make stamina recover slower.")]
        [Slider(0.1f, 1f, 10, NumberFormat = "{0:0.0}x")]
        public float LowStaminaMultiplier = 0.6f;

        [Name("Stamina consumption speed")]
        [Description("Base: 1.5x. Higher values make stamina drain faster.")]
        [Slider(1.5f, 10f, 86, NumberFormat = "{0:0.0}x")]
        public float LowStaminaConsumptionMultiplier = 1.5f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base: 4x. Minimum fatigue burn multiplier while affected by Low O₂.")]
        [Slider(2f, 100f, 981, NumberFormat = "{0:0.0}x")]
        public float LowMinFatigueBurnMultiplier = 4f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base: 4x. Maximum fatigue burn multiplier while affected by Low O₂.")]
        [Slider(2f, 100f, 981, NumberFormat = "{0:0.0}x")]
        public float LowMaxFatigueBurnMultiplier = 4f;

        [Name("Fire ignition multiplier")]
        [Description("Base: 2x. Higher values make fire starting slower.")]
        [Slider(2f, 10f, 81, NumberFormat = "{0:0.0}x")]
        public float LowFireIgnitionMultiplier = 2f;

        [Name("Seconds before recovering stamina multiplier")]
        [Description("Base: 2x. Higher values delay stamina recovery for longer.")]
        [Slider(2f, 20f, 181, NumberFormat = "{0:0.0}x")]
        public float LowSecondsBeforeRecovStamMultiplier = 2f;

        [Name("Dysentery recovery time multiplier")]
        [Description("Base: 1.5x. Higher values make dysentery recovery take longer.")]
        [Slider(1.5f, 20f, 186, NumberFormat = "{0:0.0}x")]
        public float LowDysenteryRecoveryTimeMultiplier = 1.5f;

        [Name("Food poisoning recovery time multiplier")]
        [Description("Base: 1.5x. Higher values make food poisoning recovery take longer.")]
        [Slider(1.5f, 20f, 186, NumberFormat = "{0:0.0}x")]
        public float LowFoodPoisoningRecoveryTimeMultiplier = 1.5f;


        [Name("Critical o₂")]
        [Description("Show or hide Critical o₂ settings.")]
        [Choice("+", "-")]
        public bool CriticalO2 = false;

        [Name("Altitude threshold")]
        [Description("Base: 460m. Altitude where Critical O₂ effects start.")]
        [Slider(0f, 750f, 751, NumberFormat = "{0:0}m")]
        public float CritThreshold = 460f;

        [Name("Stamina recovery speed")]
        [Description("Base: 0.3x. Lower values make stamina recover slower.")]
        [Slider(0.1f, 1f, 10, NumberFormat = "{0:0.0}x")]
        public float CritStaminaMultiplier = 0.3f;

        [Name("Stamina consumption speed")]
        [Description("Base: 2.5x. Higher values make stamina drain faster.")]
        [Slider(2f, 10f, 81, NumberFormat = "{0:0.0}x")]
        public float CritStaminaConsumptionMultiplier = 2.5f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base: 10x. Minimum fatigue burn multiplier while affected by Critical O₂.")]
        [Slider(2f, 100f, 981, NumberFormat = "{0:0.0}x")]
        public float CritMinFatigueBurnMultiplier = 10f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base: 10x. Maximum fatigue burn multiplier while affected by Critical O₂.")]
        [Slider(2f, 100f, 981, NumberFormat = "{0:0.0}x")]
        public float CritMaxFatigueBurnMultiplier = 10f;

        [Name("Fire ignition multiplier")]
        [Description("Base: 3x. Higher values make fire starting slower.")]
        [Slider(2f, 10f, 81, NumberFormat = "{0:0.0}x")]
        public float CritFireIgnitionMultiplier = 3f;

        [Name("Seconds before recovering stamina multiplier")]
        [Description("Base: 3x. Higher values delay stamina recovery for longer.")]
        [Slider(2f, 20f, 181, NumberFormat = "{0:0.0}x")]
        public float CritSecondsBeforeRecovStamMultiplier = 3f;

        [Name("Dysentery recovery time multiplier")]
        [Description("Base: 2x. Higher values make dysentery recovery take longer.")]
        [Slider(2f, 20f, 181, NumberFormat = "{0:0.0}x")]
        public float CritDysenteryRecoveryTimeMultiplier = 2f;

        [Name("Food poisoning recovery time multiplier")]
        [Description("Base: 2x. Higher values make food poisoning recovery take longer.")]
        [Slider(2f, 20f, 181, NumberFormat = "{0:0.0}x")]
        public float CritFoodPoisoningRecoveryTimeMultiplier = 2f;


        [Name("Insufficient o₂")]
        [Description("Show or hide Insufficient o₂ settings.")]
        [Choice("+", "-")]
        public bool InsufficientO2 = false;

        [Name("Altitude threshold")]
        [Description("Base: 580m. Altitude where Insufficient O₂ effects start.")]
        [Slider(0f, 800f, 801, NumberFormat = "{0:0}m")]
        public float InsuThreshold = 580f;

        [Name("Stamina recovery speed")]
        [Description("Base: 0.1x. Lower values make stamina recover slower.")]
        [Slider(0.1f, 10f, 100, NumberFormat = "{0:0.0}x")]
        public float InsuStaminaMultiplier = 0.1f;

        [Name("Stamina consumption speed")]
        [Description("Base: 3x. Higher values make stamina drain faster.")]
        [Slider(2f, 10f, 81, NumberFormat = "{0:0.0}x")]
        public float InsuStaminaConsumptionMultiplier = 3f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base: 20x. Minimum fatigue burn multiplier while affected by Insufficient O₂.")]
        [Slider(2f, 100f, 981, NumberFormat = "{0:0.0}x")]
        public float InsuMinFatigueBurnMultiplier = 20f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base: 20x. Maximum fatigue burn multiplier while affected by Insufficient O₂.")]
        [Slider(2f, 100f, 981, NumberFormat = "{0:0.0}x")]
        public float InsuMaxFatigueBurnMultiplier = 20f;

        [Name("Fire ignition multiplier")]
        [Description("Base: 5x. Higher values make fire starting slower.")]
        [Slider(2f, 10f, 81, NumberFormat = "{0:0.0}x")]
        public float InsuFireIgnitionMultiplier = 5f;

        [Name("Seconds before recovering stamina multiplier")]
        [Description("Base: 5x. Higher values delay stamina recovery for longer.")]
        [Slider(2f, 20f, 181, NumberFormat = "{0:0.0}x")]
        public float InsuSecondsBeforeRecovStamMultiplier = 5f;

        [Name("Dysentery recovery time multiplier")]
        [Description("Base: 3x. Higher values make dysentery recovery take longer.")]
        [Slider(2f, 20f, 181, NumberFormat = "{0:0.0}x")]
        public float InsuDysenteryRecoveryTimeMultiplier = 3f;

        [Name("Food poisoning recovery time multiplier")]
        [Description("Base: 3x. Higher values make food poisoning recovery take longer.")]
        [Slider(2f, 20f, 181, NumberFormat = "{0:0.0}x")]
        public float InsuFoodPoisoningRecoveryTimeMultiplier = 3f;

        [Name("Stamina consumption when walking")]
        [Description("Base: 0.1 per second. Stamina drain while walking in Insufficient O₂.")]
        [Slider(0.1f, 10f, 100, NumberFormat = "{0:0.0}/s")]
        public float InsuStaminaWalkingBurn = 0.1f;

        [Name("Condition lost when walking with no stamina")]
        [Description("Base: 0.5. Condition loss when walking with no stamina in Insufficient O₂.")]
        [Slider(0.5f, 10f, 96, NumberFormat = "{0:0.0}")]
        public float ConditionLostZeroStamina = 0.5f;

        [Name("AMS chance")]
        [Description("Base: 50%. Chance per in-game hour to develop AMS risk after staying in Insufficient O₂ without Critical O₂ acclimatization.")]
        [Slider(0, 100, 101, NumberFormat = "{0:0}%")]
        public int AMSchance = 50;

        [Name("AMS appearance time")]
        [Description("Base: 2 in-game hours. Grace time before AMS risk rolls can start while staying in Insufficient O₂ without Critical O₂ acclimatization.")]
        [Slider(1f, 10f, 19, NumberFormat = "{0:0.0}h")]
        public float AMSAppeanceTime = 2f;

        [Name("AMS disappearance time")]
        [Description("Base: 2x. Recovery multiplier for unprepared Insufficient O₂ exposure. Higher values make exposure recover faster once the player leaves the danger state.")]
        [Slider(1f, 5f, 9, NumberFormat = "{0:0.0}x")]
        public float AMSDisappearanceTime = 2f;

        [Name("AMS lethality preset")]
        [Description("Controls how much condition AMS drains once the real affliction is active.")]
        [Choice("Forgiving", "Standard", "Harsh", "Brutal", "Who Wants to Play Like This?")]
        public int AMSLethalityPreset = 1;


        [Section("Advanced")]

        [Name("ML Logging")]
        [Description("Add logs for debugging in the ML console.")]
        public bool IsLogging = false;

        [Name("Do you want to mess with affliction ?")]
        [Description("This will ruin everything...")]
        public bool RevealShinyAfflictionIconChance1 = false;

        [Name("Are you sure you want to interfer with your destiny ?")]
        [Description("Think twice")]
        public bool RevealShinyAfflictionIconChance2 = false;

        [Name("Fine. Let's tempt fate.")]
        [Description("This will reveals a true heresy. Don't touch it.")]
        public bool RevealShinyAfflictionIconChance3 = false;

        [Name("Your Destiny")]
        [Description("This slider allows you to choose how much you want to alter your destiny, please don't touch it.")]
        [Slider(0f, 100f, 1001, NumberFormat = "{0:0.0}")]
        public float AltAfflictionIconChance = 0.1f;

        internal void RefreshFields()
        {
            // HUD customization
            bool showAltitudeHudCustomization = ShowAltitudeHUDCustomization && ShowHUD;
            SetFieldVisible(nameof(AltitudeHudX), showAltitudeHudCustomization);
            SetFieldVisible(nameof(AltitudeHudY), showAltitudeHudCustomization);
            SetFieldVisible(nameof(AltitudeHudFontSize), showAltitudeHudCustomization);

            // Low O₂
            SetFieldVisible(nameof(LowThreshold), LowO2);
            SetFieldVisible(nameof(LowStaminaMultiplier), LowO2);
            SetFieldVisible(nameof(LowStaminaConsumptionMultiplier), LowO2);
            SetFieldVisible(nameof(LowMinFatigueBurnMultiplier), LowO2);
            SetFieldVisible(nameof(LowMaxFatigueBurnMultiplier), LowO2);
            SetFieldVisible(nameof(LowFireIgnitionMultiplier), LowO2);
            SetFieldVisible(nameof(LowSecondsBeforeRecovStamMultiplier), LowO2);
            SetFieldVisible(nameof(LowDysenteryRecoveryTimeMultiplier), LowO2);
            SetFieldVisible(nameof(LowFoodPoisoningRecoveryTimeMultiplier), LowO2);

            // Critical O₂
            SetFieldVisible(nameof(CritThreshold), CriticalO2);
            SetFieldVisible(nameof(CritStaminaMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritStaminaConsumptionMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritMinFatigueBurnMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritMaxFatigueBurnMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritFireIgnitionMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritSecondsBeforeRecovStamMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritDysenteryRecoveryTimeMultiplier), CriticalO2);
            SetFieldVisible(nameof(CritFoodPoisoningRecoveryTimeMultiplier), CriticalO2);

            // Insufficient O₂
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
            SetFieldVisible(nameof(AMSchance), InsufficientO2);
            SetFieldVisible(nameof(AMSAppeanceTime), InsufficientO2);
            SetFieldVisible(nameof(AMSDisappearanceTime), InsufficientO2);
            SetFieldVisible(nameof(AMSLethalityPreset), InsufficientO2);
        }

        protected override void OnChange(FieldInfo field, object? oldValue, object? newValue)
        {
            if (field.Name == nameof(RevealShinyAfflictionIconChance1) ||
                field.Name == nameof(RevealShinyAfflictionIconChance2) ||
                field.Name == nameof(RevealShinyAfflictionIconChance3))
            {
                Settings.UpdateShinyAfflictionIconChanceVisibility();
            }

            RefreshFields();
        }

        protected override void OnConfirm()
        {
            base.OnConfirm();
        }
    }

    internal static class Settings
    {
        public static OxModSettings options;

        public static void OnLoad()
        {
            options = new OxModSettings();
            options.AddToModSettings("OxygenLevels");

            options.RefreshFields();

            UpdateShinyAfflictionIconChanceVisibility();
        }

        internal static void UpdateShinyAfflictionIconChanceVisibility()
        {
            bool showSecond = options.RevealShinyAfflictionIconChance1;
            bool showThird = showSecond && options.RevealShinyAfflictionIconChance2;
            bool showChance = showThird && options.RevealShinyAfflictionIconChance3;

            if (!showSecond)
            {
                options.RevealShinyAfflictionIconChance2 = false;
                options.RevealShinyAfflictionIconChance3 = false;
            }

            if (!showThird)
            {
                options.RevealShinyAfflictionIconChance3 = false;
            }

            options.SetFieldVisible(nameof(options.RevealShinyAfflictionIconChance2), showSecond);
            options.SetFieldVisible(nameof(options.RevealShinyAfflictionIconChance3), showThird);
            options.SetFieldVisible(nameof(options.AltAfflictionIconChance), showChance);
        }
    }
}