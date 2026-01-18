﻿using Description = ModSettings.DescriptionAttribute;
using UnityEngine;
using ModSettings;
using System.ComponentModel;
using System.Reflection;
using Unity.VisualScripting;

namespace OxygenLevels
{
    internal class OxModSettings : JsonModSettings
    {
        //interloperHUDpro
        [Section("HUD Settings")]

        [Name("Using InterloperHUDpro ?")]
        [Description("A game restart will be required.")]
        public bool interHUD = false;

        //Acclimatization
        [Section("Acclimatization")]

        [Name("Time needed to acclimatize")]
        [Description("Base = 24 - ingame hours")]
        [Slider(1, 72)]
        public float AcclimatizationTimer = 24f;

        //Low o₂
        [Section("Low o₂")]

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

        //Critical o₂
        [Section("Critical o₂")]

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

        //Insufficient o₂
        [Section("Insufficient o₂")]

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

        protected override void OnConfirm()
        {
            base.OnConfirm();
            Core.isInterHUD = interHUD;
        }
    }
    internal static class Settings
    {
        public static OxModSettings options;

        public static void OnLoad()
        { 
            options = new OxModSettings();
            options.AddToModSettings("OxygenLevels");
            Core.isInterHUD = Settings.options.interHUD;

        }
    }
}