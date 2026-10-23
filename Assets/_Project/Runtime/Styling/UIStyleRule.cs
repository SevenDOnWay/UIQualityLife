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
        /// <summary>The CSS selector string (e.g. '.btn-danger', '.rounded-lg', '#submit-button').</summary>
        [Tooltip("The CSS selector string (e.g. '.btn-danger', '.rounded-lg', '#submit-button')")]
        public string selector = ".style-rule";

        /// <summary>Description and notes regarding the purpose of this rule.</summary>
        [TextArea(1, 2)]
        public string description = "";

        /// <summary>If true, overrides background color.</summary>
        [Header("Background & Fill")]
        public bool overrideBackground = false;
        /// <summary>Token name in UIStyleSheet/Theme or direct hex color.</summary>
        [Tooltip("Token name in UIStyleSheet/Theme or hex color")]
        public string backgroundColorToken = "primary";

        /// <summary>If true, overrides border color and width.</summary>
        [Header("Border")]
        public bool overrideBorder = false;
        /// <summary>Border color token ID.</summary>
        public string borderColorToken = "border";
        /// <summary>Border width token ID.</summary>
        public string borderWidthToken = "border-thin";

        /// <summary>If true, overrides corner radius.</summary>
        [Header("Corner Geometry")]
        public bool overrideCornerRadius = false;
        /// <summary>Corner radius token ID (e.g. "radius-md").</summary>
        public string cornerRadiusToken = "radius-md";
        /// <summary>If true, overrides corner style.</summary>
        public bool overrideCornerStyle = false;
        /// <summary>Geometric corner style (Round, Octagonal, Scooped).</summary>
        public CornerStyle cornerStyle = CornerStyle.Round;

        /// <summary>If true, overrides gradient fill settings.</summary>
        [Header("Gradients")]
        public bool overrideGradient = false;
        /// <summary>Secondary gradient end color token ID.</summary>
        public string gradientEndColorToken = "";
        /// <summary>Linear gradient angle in degrees [0, 360].</summary>
        [Range(0f, 360f)]
        public float gradientAngle = 0f;

        /// <summary>If true, overrides drop shadow / outer glow settings.</summary>
        [Header("Drop Shadow")]
        public bool overrideShadow = false;
        /// <summary>Drop shadow token ID in <see cref="UIStyleSheet"/>.</summary>
        public string shadowToken = "";

        /// <summary>If true, overrides typography / text properties.</summary>
        [Header("Typography")]
        public bool overrideTypography = false;
        /// <summary>Text color token ID.</summary>
        public string textColorToken = "text-primary";
        /// <summary>Font size token ID.</summary>
        public string fontSizeToken = "font-md";
        /// <summary>Optional TextMeshPro font asset override.</summary>
        public TMP_FontAsset fontAsset;

        /// <summary>If true, overrides the interactive animation class.</summary>
        [Header("Animation Profile")]
        public bool overrideAnimation = false;
        /// <summary>Associated interactive micro-animation class.</summary>
        public UIAnimationClass animationClass;

        /// <summary>Default constructor.</summary>
        public UIStyleRule() { }

        /// <summary>
        /// Creates a new style rule with the specified selector and description.
        /// </summary>
        /// <param name="selector">CSS class or ID selector (e.g. ".btn-primary", "#hud-bar").</param>
        /// <param name="description">Documentation notes.</param>
        public UIStyleRule(string selector, string description = "")
        {
            this.selector = selector;
            this.description = description;
        }

        /// <summary>
        /// Returns the selector name stripped of leading '.' or '#' symbols.
        /// </summary>
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

        /// <summary>
        /// True if this rule represents an element ID selector (starts with '#'); otherwise false.
        /// </summary>
        public bool IsIdSelector => !string.IsNullOrEmpty(selector) && selector.Trim().StartsWith("#");
    }
}
