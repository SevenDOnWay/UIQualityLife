#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIQualityLife.Editor.Configurators
{
    /// <summary>
    /// Shared utility helpers for node configurators.
    /// </summary>
    public static class NodeFormattingUtils
    {
        public static Color ParseColor(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color parsed))
            {
                return parsed;
            }
            return fallback;
        }

        public static TextAlignmentOptions ParseAlignment(string alignment)
        {
            if (string.IsNullOrEmpty(alignment)) return TextAlignmentOptions.Center;
            switch (alignment.ToLowerInvariant())
            {
                case "left":
                case "midlineleft": return TextAlignmentOptions.MidlineLeft;
                case "right":
                case "midlineright": return TextAlignmentOptions.MidlineRight;
                case "top":
                case "topcenter": return TextAlignmentOptions.Top;
                case "topleft": return TextAlignmentOptions.TopLeft;
                case "topright": return TextAlignmentOptions.TopRight;
                case "bottom":
                case "bottomleft": return TextAlignmentOptions.BottomLeft;
                case "bottomright": return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        public static FontStyles ParseFontStyle(string style)
        {
            if (string.IsNullOrEmpty(style)) return FontStyles.Normal;
            switch (style.ToLowerInvariant())
            {
                case "bold": return FontStyles.Bold;
                case "italic": return FontStyles.Italic;
                case "bolditalic": return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        public static TextAnchor ParseTextAnchor(string alignment)
        {
            if (string.IsNullOrEmpty(alignment)) return TextAnchor.UpperLeft;
            switch (alignment.ToLowerInvariant())
            {
                case "upperleft":
                case "topleft": return TextAnchor.UpperLeft;
                case "uppercenter":
                case "topcenter":
                case "top": return TextAnchor.UpperCenter;
                case "upperright":
                case "topright": return TextAnchor.UpperRight;
                case "middleleft":
                case "left": return TextAnchor.MiddleLeft;
                case "middlecenter":
                case "center": return TextAnchor.MiddleCenter;
                case "middleright":
                case "right": return TextAnchor.MiddleRight;
                case "lowerleft":
                case "bottomleft": return TextAnchor.LowerLeft;
                case "lowercenter":
                case "bottomcenter":
                case "bottom": return TextAnchor.LowerCenter;
                case "lowerright":
                case "bottomright": return TextAnchor.LowerRight;
                default: return TextAnchor.UpperLeft;
            }
        }

        public static Sprite LoadBuiltinSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            return AssetDatabase.GetBuiltinExtraResource<Sprite>($"UI/Skin/{spriteName}.psd");
        }

        public static Color? FindAncestorBackgroundColor(Transform t)
        {
            Transform curr = t != null ? t.parent : null;
            while (curr != null)
            {
                var img = curr.GetComponent<Image>();
                if (img != null && img.color.a > 0.05f)
                {
                    return img.color;
                }
                curr = curr.parent;
            }
            return null;
        }

        public static float CalculateLuminance(Color color)
        {
            float r = color.r <= 0.03928f ? color.r / 12.92f : Mathf.Pow((color.r + 0.055f) / 1.055f, 2.4f);
            float g = color.g <= 0.03928f ? color.g / 12.92f : Mathf.Pow((color.g + 0.055f) / 1.055f, 2.4f);
            float b = color.b <= 0.03928f ? color.b / 12.92f : Mathf.Pow((color.b + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        public static float CalculateContrastRatio(Color c1, Color c2)
        {
            float l1 = CalculateLuminance(c1);
            float l2 = CalculateLuminance(c2);
            return (Mathf.Max(l1, l2) + 0.05f) / (Mathf.Min(l1, l2) + 0.05f);
        }

        public static Color EnsureContrast(Color foreground, Color background, float minRatio = 3.5f)
        {
            float ratio = CalculateContrastRatio(foreground, background);
            if (ratio >= minRatio) return foreground;

            float bgLum = CalculateLuminance(background);
            if (bgLum < 0.35f)
            {
                // Dark background -> Off-white foreground
                return new Color(0.96f, 0.97f, 0.98f, foreground.a > 0 ? foreground.a : 1f);
            }
            else
            {
                // Light background -> Dark slate foreground
                return new Color(0.06f, 0.09f, 0.16f, foreground.a > 0 ? foreground.a : 1f);
            }
        }
    }

    /// <summary>
    /// Configures container panels and basic Image elements.
    /// </summary>
    public class ContainerNodeConfigurator : IUINodeConfigurator
    {
        public bool CanHandle(string resolvedType, UINodeSpec node)
        {
            return resolvedType == "container" || resolvedType == "image";
        }

        public void Configure(GameObject go, UINodeSpec node)
        {
            string colorHex = node.ResolvedColorHex;
            if (!string.IsNullOrEmpty(colorHex))
            {
                Color parsed = NodeFormattingUtils.ParseColor(colorHex, Color.clear);
                if (parsed.a > 0f)
                {
                    Image img = go.AddComponent<Image>();
                    img.color = parsed;
                    img.raycastTarget = true;

                    Sprite bgSprite = NodeFormattingUtils.LoadBuiltinSprite(node.spriteResource);
                    if (bgSprite != null)
                    {
                        img.sprite = bgSprite;
                        img.type = Image.Type.Sliced;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Configures TextMeshProUGUI text components.
    /// </summary>
    public class TextNodeConfigurator : IUINodeConfigurator
    {
        public bool CanHandle(string resolvedType, UINodeSpec node)
        {
            return resolvedType == "text";
        }

        public void Configure(GameObject go, UINodeSpec node)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = node.text ?? "Text Label";
            tmp.fontSize = node.fontSize > 0 ? node.fontSize : 14f;

            Color textColor = NodeFormattingUtils.ParseColor(node.textColorHex, Color.white);
            Color? ancestorBg = NodeFormattingUtils.FindAncestorBackgroundColor(go.transform);
            if (ancestorBg.HasValue)
            {
                textColor = NodeFormattingUtils.EnsureContrast(textColor, ancestorBg.Value);
            }

            tmp.color = textColor;
            tmp.alignment = NodeFormattingUtils.ParseAlignment(node.alignment);
            tmp.fontStyle = NodeFormattingUtils.ParseFontStyle(node.fontStyle);
            tmp.textWrappingMode = node.wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }
        }
    }

    /// <summary>
    /// Configures UGUI Button components with color blocks and child labels.
    /// </summary>
    public class ButtonNodeConfigurator : IUINodeConfigurator
    {
        public bool CanHandle(string resolvedType, UINodeSpec node)
        {
            return resolvedType == "button" || node.isButton;
        }

        public void Configure(GameObject go, UINodeSpec node)
        {
            Image bg = go.AddComponent<Image>();
            Color normal = NodeFormattingUtils.ParseColor(node.normalColorHex, new Color(0.14f, 0.45f, 0.95f));
            bg.color = normal;
            bg.type = Image.Type.Sliced;
            Sprite sprite = NodeFormattingUtils.LoadBuiltinSprite("UISprite");
            if (sprite != null) bg.sprite = sprite;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = normal;
            cb.highlightedColor = NodeFormattingUtils.ParseColor(node.highlightColorHex, new Color(0.23f, 0.51f, 0.96f));
            cb.pressedColor = NodeFormattingUtils.ParseColor(node.pressedColorHex, new Color(0.11f, 0.31f, 0.85f));
            cb.disabledColor = NodeFormattingUtils.ParseColor(node.disabledColorHex, new Color(0.4f, 0.4f, 0.4f));
            btn.colors = cb;

            if (!string.IsNullOrEmpty(node.text))
            {
                GameObject labelGO = new GameObject("Text (TMP)");
                labelGO.transform.SetParent(go.transform, false);

                RectTransform textRt = labelGO.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(8, 4);
                textRt.offsetMax = new Vector2(-8, -4);

                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text = node.text;
                tmp.fontSize = node.fontSize > 0 ? node.fontSize : 14f;
                tmp.fontStyle = NodeFormattingUtils.ParseFontStyle(node.fontStyle);
                Color btnTextColor = NodeFormattingUtils.ParseColor(node.textColorHex, Color.white);
                btnTextColor = NodeFormattingUtils.EnsureContrast(btnTextColor, normal);
                tmp.color = btnTextColor;
                tmp.alignment = TextAlignmentOptions.Center;
                if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            }
        }
    }

    /// <summary>
    /// Configures TMP_InputField components with text area, placeholder, and caret.
    /// </summary>
    public class InputFieldNodeConfigurator : IUINodeConfigurator
    {
        public bool CanHandle(string resolvedType, UINodeSpec node)
        {
            return resolvedType == "inputfield" || resolvedType == "input" || node.isInputField;
        }

        public void Configure(GameObject go, UINodeSpec node)
        {
            Image bg = go.AddComponent<Image>();
            bg.color = NodeFormattingUtils.ParseColor(node.ResolvedColorHex ?? node.backgroundColorHex, new Color(0.14f, 0.14f, 0.16f));
            bg.type = Image.Type.Sliced;
            Sprite sprite = NodeFormattingUtils.LoadBuiltinSprite("InputFieldBackground");
            if (sprite != null) bg.sprite = sprite;

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(go.transform, false);
            RectTransform areaRt = textArea.GetComponent<RectTransform>();
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(10, 6);
            areaRt.offsetMax = new Vector2(-10, -6);
            textArea.AddComponent<RectMask2D>();

            GameObject phGO = new GameObject("Placeholder", typeof(RectTransform));
            phGO.transform.SetParent(textArea.transform, false);
            RectTransform phRt = phGO.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;

            var phTMP = phGO.AddComponent<TextMeshProUGUI>();
            phTMP.text = node.placeholderText ?? "Enter text...";
            phTMP.fontSize = node.fontSize > 0 ? node.fontSize : 14f;
            phTMP.color = NodeFormattingUtils.ParseColor(node.placeholderColorHex, new Color(0.6f, 0.6f, 0.6f));
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (TMP_Settings.defaultFontAsset != null) phTMP.font = TMP_Settings.defaultFontAsset;

            GameObject textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(textArea.transform, false);
            RectTransform textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.text = node.text ?? "";
            textTMP.fontSize = node.fontSize > 0 ? node.fontSize : 14f;
            textTMP.color = NodeFormattingUtils.ParseColor(node.textColorHex, Color.white);
            textTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (TMP_Settings.defaultFontAsset != null) textTMP.font = TMP_Settings.defaultFontAsset;

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = textTMP;
            input.placeholder = phTMP;
        }
    }

    /// <summary>
    /// Configures UGUI Slider components with track and fill area.
    /// </summary>
    public class SliderNodeConfigurator : IUINodeConfigurator
    {
        public bool CanHandle(string resolvedType, UINodeSpec node)
        {
            return resolvedType == "slider" || node.isSlider;
        }

        public void Configure(GameObject go, UINodeSpec node)
        {
            var slider = go.AddComponent<Slider>();
            slider.minValue = node.sliderMinValue;
            slider.maxValue = node.sliderMaxValue > node.sliderMinValue ? node.sliderMaxValue : 1f;
            slider.value = node.sliderValue;

            Image bg = go.AddComponent<Image>();
            bg.color = NodeFormattingUtils.ParseColor(node.ResolvedColorHex ?? node.backgroundColorHex, new Color(0.2f, 0.22f, 0.25f));
            slider.targetGraphic = bg;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = NodeFormattingUtils.ParseColor(node.fillColorHex, new Color(0.14f, 0.45f, 0.95f));
            slider.fillRect = fillRt;
        }
    }

    /// <summary>
    /// Configures UGUI Toggle components with background box and checkmark graphic.
    /// </summary>
    public class ToggleNodeConfigurator : IUINodeConfigurator
    {
        public bool CanHandle(string resolvedType, UINodeSpec node)
        {
            return resolvedType == "toggle" || node.isToggle;
        }

        public void Configure(GameObject go, UINodeSpec node)
        {
            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = node.toggleState;

            Image bg = go.AddComponent<Image>();
            bg.color = NodeFormattingUtils.ParseColor(node.ResolvedColorHex ?? node.backgroundColorHex, new Color(0.18f, 0.2f, 0.24f));
            toggle.targetGraphic = bg;

            GameObject checkmarkGO = new GameObject("Checkmark", typeof(RectTransform));
            checkmarkGO.transform.SetParent(go.transform, false);
            RectTransform checkRt = checkmarkGO.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.2f, 0.2f);
            checkRt.anchorMax = new Vector2(0.8f, 0.8f);
            checkRt.sizeDelta = Vector2.zero;

            Image checkImg = checkmarkGO.AddComponent<Image>();
            checkImg.color = NodeFormattingUtils.ParseColor(node.checkmarkColorHex, new Color(0.14f, 0.45f, 0.95f));
            Sprite checkSprite = NodeFormattingUtils.LoadBuiltinSprite("Checkmark");
            if (checkSprite != null) checkImg.sprite = checkSprite;

            toggle.graphic = checkImg;
        }
    }

    /// <summary>
    /// Configures progress bars with track and normalized fill bar child.
    /// </summary>
    public class ProgressBarNodeConfigurator : IUINodeConfigurator
    {
        public bool CanHandle(string resolvedType, UINodeSpec node)
        {
            return resolvedType == "progressbar" || resolvedType == "progress" || node.isProgressBar;
        }

        public void Configure(GameObject go, UINodeSpec node)
        {
            Image trackImg = go.AddComponent<Image>();
            string trackColor = node.ResolvedColorHex;
            trackImg.color = NodeFormattingUtils.ParseColor(!string.IsNullOrEmpty(trackColor) ? trackColor : "#1E293B", new Color(0.12f, 0.16f, 0.22f));
            Sprite trackSprite = NodeFormattingUtils.LoadBuiltinSprite("UISprite");
            if (trackSprite != null)
            {
                trackImg.sprite = trackSprite;
                trackImg.type = Image.Type.Sliced;
            }

            float fillFraction = Mathf.Clamp01(node.value > 0 ? node.value : node.sliderValue);

            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(go.transform, false);
            RectTransform fillRt = fillGO.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(fillFraction, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            Image fillImg = fillGO.AddComponent<Image>();
            fillImg.color = NodeFormattingUtils.ParseColor(node.fillColorHex, new Color(0.39f, 0.40f, 0.95f));
            Sprite fillSprite = NodeFormattingUtils.LoadBuiltinSprite("UISprite");
            if (fillSprite != null)
            {
                fillImg.sprite = fillSprite;
                fillImg.type = Image.Type.Sliced;
            }
        }
    }
}
#endif
