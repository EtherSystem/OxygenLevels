﻿using Description = ModSettings.DescriptionAttribute;
using UnityEngine;
using ModSettings;
using System.ComponentModel;
using System.Reflection;

namespace OxygenLevels
{
    internal class OxModSettings : JsonModSettings
    {
        //Low o₂ effects
        [Section("Low o₂ effects")]

        [Name("Stamina recovery speed")]
        [Description("Base = 0.6")]
        [Slider(0, 1)]
        public float lowStaminaMultiplier = 0.6f;

        [Name("Stamina consumption speed")]
        [Description("Base = 1.5")]
        [Slider(1, 10)]
        public float lowStaminaConsumptionMultiplier = 1.5f;

        [Name("Minimum fatigue consumption speed")]
        [Description("Base = 4")]
        [Slider(1, 100)]
        public float lowMinFatigueBurnMultiplier = 4f;

        [Name("Maximum fatigue consumption speed")]
        [Description("Base = 4")]
        [Slider(1, 100)]
        public float lowMaxFatigueBurnMultiplier = 4f;


        //Critical o₂ effects
        [Section("Critical o₂ effects")]

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


        //Insufficient o₂ effects
        [Section("Insufficient o₂ effects")]

        [Name("Stamina recovery speed")]
        [Description("Base = 0.01")]
        [Slider(0.01f, 1)]
        public float InsuStaminaMultiplier = 0.01f;

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