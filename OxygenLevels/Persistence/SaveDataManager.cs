using ModData;
using Newtonsoft.Json;

namespace OxygenLevels.Persistence
{
    internal static class SaveDataManager
    {
        private const string ModDataName = "OxygenLevels";
        private const string DataSuffix = "oldata";

        private static readonly ModDataManager s_ModDataManager = new(ModDataName, false);

        private static bool s_Loaded;
        private static bool s_Dirty;

        internal static bool IsLoaded => s_Loaded;
        internal static bool IsDirty => s_Dirty;

        private static void EnsureState()
        {
            Core.State ??= new OLState();
        }

        private static string GetLastKnownOutdoorAltitudeLog()
        {
            return Core.State.HasLastKnownOutdoorAltitude ? $"{Core.State.LastKnownOutdoorAltitude:0.##}m" : "None";
        }

        private static string GetRestForCureOverrideLog(bool hasOverride, float hours)
        {
            return hasOverride ? $"{hours:0.###}h" : "None";
        }

        internal static void EnsureLoaded()
        {
            if (s_Loaded)
                return;

            Load();
        }

        internal static void MarkDirty()
        {
            s_Dirty = true;
        }

        internal static void MarkDirtyAndQueueSurvivalSave()
        {
            MarkDirty();
            AfflictionSaveHelper.QueueSurvivalSave();
        }

        internal static void SaveIfDirty(bool log = true)
        {
            if (!s_Loaded || !s_Dirty)
                return;

            if (SaveInternal(log))
                s_Dirty = false;
        }

        private static bool SaveInternal(bool log)
        {
            try
            {
                EnsureState();
                Core.NormalizeState();

                string data = JsonConvert.SerializeObject(Core.State);
                bool saved = s_ModDataManager.Save(data, DataSuffix);

                if (!saved)
                {
                    Core.Warn("Failed to save OxygenLevels data: ModDataManager.Save returned false.");
                    return false;
                }

                if (log)
                {
                    Core.Log(
                        $"Saved -> " +
                        $"Critical:{Core.State.CriticalAcclimatizationHours:0.###}({Core.State.CriticalAcclimatized}) | " +
                        $"Insufficient:{Core.State.InsufficientAcclimatizationHours:0.###}({Core.State.InsufficientAcclimatized}) | " +
                        $"UnpreparedInsu:{Core.State.UnpreparedInsufficientO2Hours:0.###} | " +
                        $"LastOutdoorAltitude:{GetLastKnownOutdoorAltitudeLog()} | " +
                        $"DysRest:{GetRestForCureOverrideLog(Core.State.HasDysenteryRestForCureOverride, Core.State.DysenteryRestForCureHours)} | " +
                        $"FoodRest:{GetRestForCureOverrideLog(Core.State.HasFoodPoisoningRestForCureOverride, Core.State.FoodPoisoningRestForCureHours)}");
                }

                return true;
            }
            catch (Exception e)
            {
                Core.Warn($"Failed to save OxygenLevels data: {e}");
                return false;
            }
        }

        internal static void Load()
        {
            try
            {
                string? data = s_ModDataManager.Load(DataSuffix);

                if (string.IsNullOrEmpty(data))
                {
                    Core.State = new OLState();
                    Core.NormalizeState();
                    Core.RestoreRuntimeStateFromLoadedSave();

                    s_Loaded = true;
                    s_Dirty = false;

                    Core.Log("Loaded -> empty data (fresh slot)");
                    return;
                }

                OLState? loaded = null;

                try
                {
                    loaded = JsonConvert.DeserializeObject<OLState>(data);
                }
                catch (Exception e)
                {
                    Core.Warn($"Failed to deserialize OxygenLevels data, using fresh state: {e}");
                }

                Core.State = loaded ?? new OLState();
                Core.NormalizeState();
                Core.RestoreRuntimeStateFromLoadedSave();

                s_Loaded = true;
                s_Dirty = false;

                Core.Log(
                    $"Loaded -> " +
                    $"Critical:{Core.State.CriticalAcclimatizationHours:0.###}({Core.State.CriticalAcclimatized}) | " +
                    $"Insufficient:{Core.State.InsufficientAcclimatizationHours:0.###}({Core.State.InsufficientAcclimatized}) | " +
                    $"UnpreparedInsu:{Core.State.UnpreparedInsufficientO2Hours:0.###} | " +
                    $"LastOutdoorAltitude:{GetLastKnownOutdoorAltitudeLog()} | " +
                    $"DysRest:{GetRestForCureOverrideLog(Core.State.HasDysenteryRestForCureOverride, Core.State.DysenteryRestForCureHours)} | " +
                    $"FoodRest:{GetRestForCureOverrideLog(Core.State.HasFoodPoisoningRestForCureOverride, Core.State.FoodPoisoningRestForCureHours)}");
            }
            catch (Exception e)
            {
                Core.State = new OLState();
                Core.NormalizeState();
                Core.RestoreRuntimeStateFromLoadedSave();

                s_Loaded = true;
                s_Dirty = false;

                Core.Warn($"Failed to load OxygenLevels data, using fresh state: {e}");
            }
        }

        internal static void OnNewGame()
        {
            Core.ClearRuntimeStateForSaveTransition();
            Core.State = new OLState();
            Core.NormalizeState();

            s_Loaded = true;
            s_Dirty = true;

            Core.Log("Clearing data for new game");
        }

        internal static void ClearRuntimeState()
        {
            s_Loaded = false;
            s_Dirty = false;

            Core.ClearRuntimeStateForSaveTransition();
            Core.State = new OLState();
        }
    }

    [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.WriteSlotToDisk), [typeof(SlotData), typeof(SaveGameSlots.Timestamp)])]
    internal class OxygenLevels_SavePatch
    {
        private static void Prefix()
        {
            SaveDataManager.SaveIfDirty(log: true);
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), [typeof(string), typeof(int)])]
    internal class OxygenLevels_LoadPatch
    {
        private static void Postfix()
        {
            SaveDataManager.ClearRuntimeState();
            SaveDataManager.Load();
        }
    }

    [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.CreateSlot), [typeof(string), typeof(SaveSlotType), typeof(uint), typeof(Episode)])]
    internal class OxygenLevels_NewGamePatch
    {
        private static void Postfix()
        {
            SaveDataManager.OnNewGame();
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.DoExitToMainMenu))]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadMainMenu))]
    internal class OxygenLevels_MainMenuPatch
    {
        private static void Postfix()
        {
            SaveDataManager.ClearRuntimeState();
        }
    }
}