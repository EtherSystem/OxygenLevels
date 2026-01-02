using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using UnityEngine.SceneManagement;
using static OxygenLevels.Core;

namespace OxygenLevels
{
    internal static class Patches
    {
        private static Color darkRed = new Color(0.8f, 0.2f, 0.23f, 1.000f);
        [HarmonyPatch(typeof(StatusBar), "Update")]
        private static class AltitudeMetter
        {
            public static double elapsedMinutes = 0d;
            public static GameObject tempObject;

            private static void Postfix(StatusBar __instance)
            {
                //if (__instance.m_StatusBarType != StatusBar.StatusBarType.Hunger) return;
                if (!__instance.m_IsOnHUD) return;

                if (__instance.m_StatusBarType == StatusBar.StatusBarType.Cold)
                {
                    UpdateTempLabel(__instance);
                }
            }

            private static void UpdateTempLabel(StatusBar __instance)
            {
                if (tempObject == null)
                {
                    // init
                    UISprite sprite = __instance.m_OuterBoxSprite.GetComponent<UISprite>();
                    GameObject spriteObject = sprite.gameObject;

                    tempObject = new GameObject("altitude");
                    tempObject.transform.SetParent(spriteObject.transform.parent);
                    tempObject.transform.localScale = spriteObject.transform.localScale;

                    UILabel tempLabel = tempObject.AddComponent<UILabel>();
                    tempLabel.text = "Altitude";
                    // tempLabel.color = Color.white;
                    tempLabel.color = new Color(0.9f, 0.95f, 1f);  // Use an off white
                    tempLabel.fontStyle = FontStyle.Normal;
                    tempLabel.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
                    tempLabel.fontSize = 32;
                    tempLabel.effectStyle = UILabel.Effect.Outline;
                    tempLabel.effectColor = new Color(0.125f, 0.094f, 0.094f, 0.6f);
                    tempLabel.effectDistance = new Vector2(1.7f, 1.7f);

                    tempLabel.overflowMethod = UILabel.Overflow.ResizeFreely;
                    tempLabel.alignment = NGUIText.Alignment.Left;
                    tempLabel.pivot = UIWidget.Pivot.Left;

                    //int x_offset = -sprite.width / 2; // + tempLabel.width/2;
                    int x_offset = -tempLabel.width / 2;
                    int y_offset = 100 + tempLabel.height;
                    tempObject.transform.localPosition = new Vector3(x_offset, y_offset, 0);
                }
                else if (GameManager.GetHighResolutionTimerManager().GetElapsedMinutes() - elapsedMinutes >= 0.1d)
                {
                    // update every 0.1 ingame minutes
                    UILabel tempLabel = tempObject.GetComponent<UILabel>();
                    if (tempLabel != null && GameManager.GetFreezingComponent() != null)
                    {
                        int temp = (int)Math.Round(GameManager.GetFreezingComponent().CalculateBodyTemperature());
                        switch (Core.currentState)
                        {
                            case AltitudeState.Normal:
                                tempLabel.text = "   Normal o₂";
                                tempLabel.color = new Color(0.9f, 0.95f, 1f);  // Use an off white
                                break;
                            case AltitudeState.Weakened:
                                tempLabel.text = "   Low o₂";
                                tempLabel.color = new Color(1f, 0.85f, 0.2f);  // Yellowish
                                break;
                            case AltitudeState.HeavyWeakened:
                                tempLabel.text = "   Critical o₂";
                                tempLabel.color = darkRed;
                                break;
                            case AltitudeState.TooWeak:
                                tempLabel.text = "   Insufficient o₂";
                                tempLabel.color = darkRed;
                                break;
                        }
                        elapsedMinutes = GameManager.GetHighResolutionTimerManager().GetElapsedMinutes();
                    }
                }
            }
        }
    }
}