#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Centralized UI styling utilities and design tokens matching the official
    /// Unity Editor UI Toolkit Design System (Figma Community Specification).
    /// Dynamically adapts to Unity Pro (Dark) and Personal (Light) skins.
    /// </summary>
    public static class UIElementStyleUtils
    {
        public static bool IsDarkTheme => EditorGUIUtility.isProSkin;

        // --- Surface & Background Tokens ---
        public static Color WindowBg => IsDarkTheme ? new Color(0.157f, 0.157f, 0.157f, 1f) : new Color(0.784f, 0.784f, 0.784f, 1f); // #282828 / #C8C8C8
        public static Color PanelBg => IsDarkTheme ? new Color(0.220f, 0.220f, 0.220f, 1f) : new Color(0.855f, 0.855f, 0.855f, 1f);  // #383838 / #DADADA
        public static Color CardBg => IsDarkTheme ? new Color(0.188f, 0.188f, 0.188f, 1f) : new Color(0.894f, 0.894f, 0.894f, 1f);   // #303030 / #E4E4E4
        public static Color CardHoverBg => IsDarkTheme ? new Color(0.235f, 0.235f, 0.235f, 1f) : new Color(0.933f, 0.933f, 0.933f, 1f); // #3C3C3C / #EEEEEE
        public static Color NeutralDarkBg => new Color(0.118f, 0.125f, 0.145f, 1f); // #1E2025 standard thumbnail preview background

        // --- Border & Outline Tokens ---
        public static Color BorderDefault => IsDarkTheme ? new Color(0.137f, 0.137f, 0.137f, 1f) : new Color(0.647f, 0.647f, 0.647f, 1f); // #232323 / #A5A5A5
        public static Color BorderSubtle => IsDarkTheme ? new Color(0.188f, 0.188f, 0.188f, 1f) : new Color(0.725f, 0.725f, 0.725f, 1f);  // #303030 / #B9B9B9
        public static Color BorderHighlight => IsDarkTheme ? new Color(0.35f, 0.35f, 0.35f, 1f) : new Color(0.55f, 0.55f, 0.55f, 1f);
        public static Color BorderFocus => new Color(0.220f, 0.545f, 0.992f, 1f); // #388BFD (Unity 6 Accent)

        // --- Accent Blue Tokens (Unity Selection) ---
        public static Color UnityAccentBlue => IsDarkTheme ? new Color(0.173f, 0.365f, 0.533f, 1f) : new Color(0.235f, 0.490f, 0.765f, 1f); // #2C5D88 (Standard Unity Selection)
        public static Color UnityAccentBlueHover => IsDarkTheme ? new Color(0.208f, 0.455f, 0.718f, 1f) : new Color(0.294f, 0.569f, 0.863f, 1f); // #3574B7
        public static Color AccentBlue => UnityAccentBlue;

        // --- Chip & Tag Tokens ---
        public static Color UnitySubtleChip => IsDarkTheme ? new Color(0.235f, 0.235f, 0.235f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f); // #3C3C3C
        public static Color UnityDeleteChip => IsDarkTheme ? new Color(0.35f, 0.20f, 0.20f, 1f) : new Color(0.85f, 0.70f, 0.70f, 1f);
        public static Color UnityWarningChip => IsDarkTheme ? new Color(0.35f, 0.28f, 0.12f, 1f) : new Color(0.88f, 0.82f, 0.65f, 1f);

        // --- Text Tokens ---
        public static Color TextPrimary => IsDarkTheme ? new Color(0.824f, 0.824f, 0.824f, 1f) : new Color(0.035f, 0.035f, 0.035f, 1f); // #D2D2D2 / #090909
        public static Color TextSecondary => IsDarkTheme ? new Color(0.600f, 0.600f, 0.600f, 1f) : new Color(0.294f, 0.294f, 0.294f, 1f); // #999999 / #4B4B4B
        public static Color TextMuted => IsDarkTheme ? new Color(0.431f, 0.431f, 0.431f, 1f) : new Color(0.450f, 0.450f, 0.450f, 1f);     // #6E6E6E / #737373

        // --- Status Colors ---
        public static Color StatusSuccess => new Color(0.247f, 0.725f, 0.314f, 1f); // #3FB950
        public static Color StatusWarning => new Color(0.824f, 0.600f, 0.133f, 1f); // #D29922
        public static Color StatusDanger => new Color(0.973f, 0.318f, 0.286f, 1f);  // #F85149

        /// <summary>
        /// Sets all 4 borders of a UI Toolkit <see cref="VisualElement"/> uniformly.
        /// </summary>
        /// <param name="elem">VisualElement to style.</param>
        /// <param name="width">Border stroke width in pixels.</param>
        /// <param name="color">Border stroke color.</param>
        public static void SetBorder(VisualElement elem, float width, Color color)
        {
            elem.style.borderTopWidth = width;
            elem.style.borderBottomWidth = width;
            elem.style.borderLeftWidth = width;
            elem.style.borderRightWidth = width;

            elem.style.borderTopColor = color;
            elem.style.borderBottomColor = color;
            elem.style.borderLeftColor = color;
            elem.style.borderRightColor = color;
        }

        /// <summary>
        /// Sets uniform corner radius on all 4 corners of a UI Toolkit <see cref="VisualElement"/>.
        /// </summary>
        /// <param name="elem">VisualElement to style.</param>
        /// <param name="radius">Corner radius in pixels.</param>
        public static void SetBorderRadius(VisualElement elem, float radius)
        {
            elem.style.borderTopLeftRadius = radius;
            elem.style.borderTopRightRadius = radius;
            elem.style.borderBottomLeftRadius = radius;
            elem.style.borderBottomRightRadius = radius;
        }

        /// <summary>
        /// Applies consistent square thumbnail styling with standard #1E2025 dark neutral contrast.
        /// </summary>
        public static VisualElement CreateThumbnailElement(float size, Texture2D previewTex, Color fallbackColor)
        {
            var thumb = new VisualElement();
            thumb.style.width = size;
            thumb.style.height = size;
            thumb.style.backgroundColor = NeutralDarkBg;
            SetBorderRadius(thumb, Mathf.Max(3f, size * 0.08f));
            SetBorder(thumb, 1f, BorderDefault);

            if (previewTex != null)
            {
                thumb.style.backgroundImage = new StyleBackground(previewTex);
            }
            else
            {
                thumb.style.backgroundColor = fallbackColor;
            }

            return thumb;
        }

        /// <summary>
        /// Creates a category badge chip matching Unity Editor UI Toolkit standards (3px radius, 18px height).
        /// </summary>
        public static Button CreateCategoryTag(string text, Action onClick, bool isActive = false)
        {
            var tag = new Button(onClick) { text = text };
            tag.style.fontSize = 10;
            tag.style.height = 18;
            tag.style.paddingLeft = 6;
            tag.style.paddingRight = 6;
            tag.style.marginRight = 4;
            SetBorderRadius(tag, 3);
            SetBorder(tag, 1, isActive ? BorderFocus : BorderDefault);

            if (isActive)
            {
                tag.style.backgroundColor = UnityAccentBlue;
                tag.style.color = Color.white;
                tag.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                tag.style.backgroundColor = CardBg;
                tag.style.color = TextSecondary;
            }

            return tag;
        }
    }
}
#endif
