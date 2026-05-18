using static OxygenLevels.Core;

namespace OxygenLevels.Patches
{
    internal static class Patches
    {
        [HarmonyPatch(typeof(StatusBar), nameof(StatusBar.Update))]
        private static class AltitudeMeter
        {
            private static readonly Color DefaultTextColor = new(0.9f, 0.95f, 1f, 1f);
            private static readonly Color WarningTextColor = new(1f, 0.85f, 0.2f, 1f);
            private static readonly Color DangerTextColor = new(0.8f, 0.2f, 0.23f, 1f);
            private static readonly Color OutlineColor = new(0.125f, 0.094f, 0.094f, 0.6f);

            private static double _lastUpdateMinutes = 0d;
            private static UILabel _altitudeLabel;
            private static GameObject _altitudeObject;

            private static Vector3 AltitudeHudPosition => new(Settings.options.AltitudeHudX, Settings.options.AltitudeHudY, 0f);

            private static void Postfix(StatusBar __instance)
            {
                if (__instance == null || !__instance.m_IsOnHUD)
                    return;

                if (__instance.m_StatusBarType != StatusBar.StatusBarType.Cold)
                    return;

                if (!Settings.options.ShowHUD)
                {
                    HideAltitudeHud();
                    return;
                }

                UILabel label = GetOrCreateAltitudeLabel(__instance);
                if (label == null)
                    return;

                RefreshAltitudeHudLayout(label);

                double now = GameManager.GetHighResolutionTimerManager().GetElapsedMinutes();
                if (now - _lastUpdateMinutes < 0.1d)
                    return;

                label.text = GetAltitudeHudText();
                label.color = GetAltitudeHudColor();
                label.gameObject.SetActive(true);

                _lastUpdateMinutes = now;
            }

            private static UILabel GetOrCreateAltitudeLabel(StatusBar statusBar)
            {
                if (statusBar.m_OuterBoxSprite == null)
                    return null;

                UISprite outerBoxSprite = statusBar.m_OuterBoxSprite.GetComponent<UISprite>();
                if (outerBoxSprite == null)
                    return null;

                Transform targetParent = outerBoxSprite.transform.parent;
                if (targetParent == null)
                    return null;

                if (_altitudeLabel != null)
                {
                    if (_altitudeLabel.transform.parent == targetParent)
                        return _altitudeLabel;

                    UnityEngine.Object.Destroy(_altitudeLabel.gameObject);
                    _altitudeLabel = null;
                    _altitudeObject = null;
                }

                GameObject existingObject = targetParent.Find("AltitudeHudLabel")?.gameObject;
                if (existingObject != null)
                {
                    UILabel existingLabel = existingObject.GetComponent<UILabel>();
                    if (existingLabel != null)
                    {
                        _altitudeObject = existingObject;
                        _altitudeLabel = existingLabel;
                        return _altitudeLabel;
                    }
                }

                _altitudeObject = new GameObject("AltitudeHudLabel");
                _altitudeObject.transform.SetParent(targetParent, false);
                _altitudeObject.transform.localScale = Vector3.one;

                _altitudeLabel = _altitudeObject.AddComponent<UILabel>();
                ConfigureAltitudeLabel(_altitudeLabel);
                _altitudeLabel.text = string.Empty;
                _altitudeLabel.gameObject.SetActive(false);

                return _altitudeLabel;
            }

            private static void ConfigureAltitudeLabel(UILabel label)
            {
                label.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
                label.fontStyle = FontStyle.Normal;
                label.color = DefaultTextColor;
                label.fontSize = Settings.options.AltitudeHudFontSize;
                label.effectStyle = UILabel.Effect.Outline;
                label.effectColor = OutlineColor;
                label.effectDistance = Settings.options.AltitudeHudFontSize >= 32
                    ? new Vector2(1.7f, 1.7f)
                    : new Vector2(1.5f, 1.5f);
                label.overflowMethod = UILabel.Overflow.ResizeFreely;
                label.alignment = NGUIText.Alignment.Left;
                label.pivot = UIWidget.Pivot.Left;
            }

            private static void RefreshAltitudeHudLayout(UILabel label)
            {
                label.transform.localPosition = AltitudeHudPosition;

                int fontSize = Settings.options.AltitudeHudFontSize;
                if (label.fontSize != fontSize)
                {
                    label.fontSize = fontSize;
                    label.effectDistance = fontSize >= 32
                        ? new Vector2(1.7f, 1.7f)
                        : new Vector2(1.5f, 1.5f);
                }
            }

            private static string GetAltitudeHudText()
            {
                return currentState switch
                {
                    AltitudeState.Normal => Localization.Get("GAMEPLAY_NormalDisplay"),
                    AltitudeState.Weakened => Localization.Get("GAMEPLAY_LowDisplay"),
                    AltitudeState.HeavyWeakened => Localization.Get("GAMEPLAY_CriticalDisplay"),
                    AltitudeState.TooWeak => Localization.Get("GAMEPLAY_InsufficientDisplay"),
                    _ => string.Empty
                };
            }

            private static Color GetAltitudeHudColor()
            {
                return currentState switch
                {
                    AltitudeState.Weakened => WarningTextColor,
                    AltitudeState.HeavyWeakened => DangerTextColor,
                    AltitudeState.TooWeak => DangerTextColor,
                    _ => DefaultTextColor
                };
            }

            private static void HideAltitudeHud()
            {
                _altitudeLabel?.gameObject.SetActive(false);
            }
        }
    }
}