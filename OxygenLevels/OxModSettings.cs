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

        [Name("Elevation")]
        [Description("A game restart will be required. Set it to 100 if you're using InterloperHUDpro. Otherwise, leave it at 20.")]
        [Slider(20, 100, 2)]
        public int elevationHUD = 20; 
        
        //Low o₂
        [Section("Low o₂")]

        [Name("Altitude threshold")]
        [Description("Base = 360")]
        [Slider(0, 600)]
        public float LowThreshold = 360f;

        [Name("Stamina recovery speed")]
        [Description("Base = 0.6")]
        [Slider(0, 1)]
        public float LowStaminaMultiplier = 0.6f;

        [Name("Stamina consumption speed")]
        [Description("Base = 1.5")]
        [Slider(1, 10)]
        public float LowStaminaConsumptionMultiplier = 1.5f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base = 4")]
        [Slider(1, 100)]
        public float LowMinFatigueBurnMultiplier = 4f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base = 4")]
        [Slider(1, 100)]
        public float LowMaxFatigueBurnMultiplier = 4f;

        [Name("Fire ignition multiplier")]
        [Description("Base = 2")]
        [Slider(1, 10)]
        public float LowFireIgnitionMultiplier = 2f;


        //Critical o₂
        [Section("Critical o₂")]

        [Name("Altitude threshold")]
        [Description("Base = 460")]
        [Slider(0, 650)]
        public float CritThreshold = 460f;

        [Name("Stamina recovery speed")]
        [Description("Base = 0.3")]
        [Slider(0, 1)]
        public float CritStaminaMultiplier = 0.3f;

        [Name("Stamina consumption speed")]
        [Description("Base = 2.5")]
        [Slider(1, 10)]
        public float CritStaminaConsumptionMultiplier = 2.5f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base = 10")]
        [Slider(1, 100)]
        public float CritMinFatigueBurnMultiplier = 10f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base = 10")]
        [Slider(1, 100)]
        public float CritMaxFatigueBurnMultiplier = 10f;

        [Name("Fire ignition multiplier")]
        [Description("Base = 3")]
        [Slider(1, 10)]
        public float CritFireIgnitionMultiplier = 3f;


        //Insufficient o₂
        [Section("Insufficient o₂")]

        [Name("Altitude threshold")]
        [Description("Base = 580")]
        [Slider(0, 700)]
        public float InsuThreshold = 580f;

        [Name("Stamina recovery speed")]
        [Description("Base = 0.1 / 10")]
        [Slider(0.1f, 10.00f, 99)]
        public float InsuStaminaMultiplier = 0.1f;

        [Name("Stamina consumption speed")]
        [Description("Base = 3")]
        [Slider(1, 10)]
        public float InsuStaminaConsumptionMultiplier = 3f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base = 20")]
        [Slider(1, 100)]
        public float InsuMinFatigueBurnMultiplier = 20f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base = 20")]
        [Slider(1, 100)]
        public float InsuMaxFatigueBurnMultiplier = 20f;

        [Name("Stamina consumption when walking")]
        [Description("Base = 0.1")]
        [Slider(0.1f, 10, 99)]
        public float InsuStaminaWalkingBurn = 0.1f;

        [Name("Condition lost when walking with no stamina")]
        [Description("Base = 0.5 / 10")]
        [Slider(0.5f, 10f, 95)]
        public float ConditionLostZeroStamina = 0.5f;

        [Name("Fire ignition multiplier")]
        [Description("Base = 5")]
        [Slider(1, 10)]
        public float InsuFireIgnitionMultiplier = 5f;

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
        }
    }
}