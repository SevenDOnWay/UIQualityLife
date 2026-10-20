#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Centralized UI styling utilities, design tokens, and recurring element generators.
    /// Eliminates code duplication across the shelf, modal wizards, and card renderers (DRY principle).
    /// </summary>
    public static class UIElementStyleUtils
    {
        // Design Tokens
        public static readonly Color NeutralDarkBg = new Color(0.12f, 0.125f, 0.145f, 1f); // #1E2025
        public static readonly Color WindowBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        public static readonly Color CardBg = new Color(0.21f, 0.21f, 0.21f, 1f);
        public static readonly Color CardHoverBg = new Color(0.28f, 0.28f, 0.28f, 1f);
        public static readonly Color BorderSubtle = new Color(0.26f, 0.26f, 0.26f, 1f);
        public static readonly Color BorderHighlight = new Color(0.35f, 0.35f, 0.35f, 1f);

        public static readonly Color TextPrimary = new Color(0.9f, 0.9f, 0.9f, 1f);
        public static readonly Color TextSecondary = new Color(0.6f, 0.6f, 0.6f, 1f);
        public static readonly Color TextMuted = new Color(0.45f, 0.45f, 0.45f, 1f);
        public static readonly Color AccentBlue = new Color(0.18f, 0.45f, 0.85f, 1f);

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

        public static void SetBorderRadius(VisualElement elem, float radius)
        {
            elem.style.borderTopLeftRadius = radius;
            elem.style.borderTopRightRadius = radius;
            elem.style.borderBottomLeftRadius = radius;
            elem.style.borderBottomRightRadius = radius;
        }

        /// <summary>
        /// Applies consistent square thumbnail styling with #1E2025 background and fallback color.
        /// </summary>
        public static VisualElement CreateThumbnailElement(float size, Texture2D previewTex, Color fallbackColor)
        {
            var thumb = new VisualElement();
            thumb.style.width = size;
            thumb.style.height = size;
            thumb.style.backgroundColor = NeutralDarkBg;
            SetBorderRadius(thumb, Mathf.Max(3f, size * 0.08f));
            SetBorder(thumb, 1f, new Color(0.22f, 0.22f, 0.22f));

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
        /// Creates a category badge button that triggers filtering when clicked.
        /// </summary>
        public static Button CreateCategoryTag(string text, Action onClick, bool isActive = false)
        {
            var tag = new Button(onClick) { text = text };
            tag.style.fontSize = 9;
            tag.style.height = 17;
            tag.style.paddingLeft = 5;
            tag.style.paddingRight = 5;
            tag.style.marginRight = 3;
            SetBorderRadius(tag, 3);

            if (isActive)
            {
                tag.style.backgroundColor = AccentBlue;
                tag.style.color = Color.white;
                tag.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                tag.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
                tag.style.color = TextSecondary;
            }

            return tag;
        }
    }
}
#endif
