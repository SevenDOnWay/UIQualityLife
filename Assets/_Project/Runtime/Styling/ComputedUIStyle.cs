using System;
using TMPro;
using UnityEngine;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Encapsulates the accumulated styling state computed during the CSS cascade pass.
    /// Eliminates parameter bloat and replaces multiple ref parameters with a unified style model.
    /// </summary>
    public class ComputedUIStyle
    {
        /// <summary>Whether a background color token was resolved.</summary>
        public bool hasBg;
        /// <summary>Resolved background color token or hex string.</summary>
        public string bgToken;

        /// <summary>Whether a border color token was resolved.</summary>
        public bool hasBorderColor;
        /// <summary>Resolved border color token or hex string.</summary>
        public string borderColorToken;

        /// <summary>Whether a border width token was resolved.</summary>
        public bool hasBorderWidth;
        /// <summary>Resolved border width token or numeric string.</summary>
        public string borderWidthToken;

        /// <summary>Whether a corner radius token was resolved.</summary>
        public bool hasRadius;
        /// <summary>Resolved corner radius token or numeric string.</summary>
        public string radiusToken;

        /// <summary>Whether a corner style override was resolved.</summary>
        public bool hasCornerStyle;
        /// <summary>Resolved corner style (Round, Cut, Squircle).</summary>
        public CornerStyle resolvedCornerStyle = CornerStyle.Round;

        /// <summary>Whether a gradient override was resolved.</summary>
        public bool hasGradient;
        /// <summary>Resolved gradient end color token or hex string.</summary>
        public string gradientEndToken;
        /// <summary>Resolved gradient angle in degrees.</summary>
        public float gradientAngle;

        /// <summary>Whether a shadow token was resolved.</summary>
        public bool hasShadow;
        /// <summary>Resolved shadow token identifier.</summary>
        public string shadowToken;

        /// <summary>Whether typography overrides were resolved.</summary>
        public bool hasTypography;
        /// <summary>Resolved text color token.</summary>
        public string textColorToken;
        /// <summary>Resolved font size token.</summary>
        public string fontSizeToken;
        /// <summary>Resolved TMP font asset reference.</summary>
        public TMP_FontAsset fontAsset;

        /// <summary>Whether an animation class was resolved.</summary>
        public bool hasAnimation;
        /// <summary>Resolved UIAnimationClass reference.</summary>
        public UIAnimationClass animationClass;

        /// <summary>
        /// Indicates if any property requires ProceduralImage capabilities (rounded corners, borders, shadows, gradients).
        /// </summary>
        public bool RequiresProceduralImage => hasRadius || hasBorderWidth || hasBorderColor || hasShadow || hasGradient;

        /// <summary>
        /// Applies overrides from a <see cref="UIStyleRule"/> onto this computed style.
        /// Higher-specificity rules override earlier properties.
        /// </summary>
        /// <param name="rule">The CSS style rule to apply.</param>
        public void ApplyRule(UIStyleRule rule)
        {
            if (rule == null) return;

            if (rule.overrideBackground)
            {
                hasBg = true;
                bgToken = rule.backgroundColorToken;
            }

            if (rule.overrideBorder)
            {
                hasBorderColor = true;
                borderColorToken = rule.borderColorToken;
                hasBorderWidth = true;
                borderWidthToken = rule.borderWidthToken;
            }

            if (rule.overrideCornerRadius)
            {
                hasRadius = true;
                radiusToken = rule.cornerRadiusToken;
            }

            if (rule.overrideCornerStyle)
            {
                hasCornerStyle = true;
                resolvedCornerStyle = rule.cornerStyle;
            }

            if (rule.overrideGradient)
            {
                hasGradient = true;
                gradientEndToken = rule.gradientEndColorToken;
                gradientAngle = rule.gradientAngle;
            }

            if (rule.overrideShadow)
            {
                hasShadow = true;
                shadowToken = rule.shadowToken;
            }

            if (rule.overrideTypography)
            {
                hasTypography = true;
                textColorToken = rule.textColorToken;
                fontSizeToken = rule.fontSizeToken;
                if (rule.fontAsset != null)
                {
                    fontAsset = rule.fontAsset;
                }
            }

            if (rule.overrideAnimation && rule.animationClass != null)
            {
                hasAnimation = true;
                animationClass = rule.animationClass;
            }
        }
    }
}
