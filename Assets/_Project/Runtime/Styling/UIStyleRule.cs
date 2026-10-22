using System;
using UnityEngine;
using TMPro;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Represents a single CSS-like selector ruleset (e.g. .btn-danger, .card-surface, #hud-bar).
    /// Can specify background colors, borders, corner radii, typography, and micro-animation classes.
    /// Properties reference design tokens or specify raw values.
    /// </summary>
    [Serializable]
    public class UIStyleRule
    {
        [Tooltip("The CSS selector string (e.g. '.btn-danger', '.rounded-lg', '#submit-button')")]
        public string selector = ".style-rule";

        [TextArea(1, 2)]
        public string description = "";

        [Header("Background & Fill")]
        public bool overrideBackground = false;
        [Tooltip("Token name in UIStyleSheet/Theme or hex color")]
        public string backgroundColorToken = "primary";

        [Header("Border")]
        public bool overrideBorder = false;
        public string borderColorToken = "border";
        public string borderWidthToken = "border-thin";

        [Header("Corner Geometry")]
        public bool overrideCornerRadius = false;
        public string cornerRadiusToken = "radius-md";
        public bool overrideCornerStyle = false;
        public CornerStyle cornerStyle = CornerStyle.Round;

        [Header("Gradients")]
        public bool overrideGradient = false;
        public string gradientEndColorToken = "";
        [Range(0f, 360f)]
        public float gradientAngle = 0f;

        [Header("Drop Shadow")]
        public bool overrideShadow = false;
        public string shadowToken = "";

        [Header("Typography")]
        public bool overrideTypography = false;
        public string textColorToken = "text-primary";
        public string fontSizeToken = "font-md";
        public TMP_FontAsset fontAsset;

        [Header("Animation Profile")]
        public bool overrideAnimation = false;
        public UIAnimationClass animationClass;

        public UIStyleRule() { }

        public UIStyleRule(string selector, string description = "")
        {
            this.selector = selector;
            this.description = description;
        }

        public string CleanSelectorName
        {
            get
            {
                if (string.IsNullOrEmpty(selector)) return "";
                string s = selector.Trim();
                if (s.StartsWith(".") || s.StartsWith("#")) return s.Substring(1).Trim();
                return s;
            }
        }

        public bool IsIdSelector => !string.IsNullOrEmpty(selector) && selector.Trim().StartsWith("#");
    }
}
