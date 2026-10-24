#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Declarative specification for a theme color design token.
    /// </summary>
    [Serializable]
    public class ColorTokenSpec
    {
        /// <summary>Token identifier (e.g. "primary", "surface").</summary>
        public string id;
        /// <summary>Hex color string (#RRGGBB or #RRGGBBAA).</summary>
        public string hex;
        /// <summary>Optional documentation or usage note.</summary>
        public string description;
    }

    /// <summary>
    /// Declarative specification for a theme dimension/float design token.
    /// </summary>
    [Serializable]
    public class FloatTokenSpec
    {
        /// <summary>Token identifier (e.g. "radius-md", "border-thin").</summary>
        public string id;
        /// <summary>Numeric float dimension value.</summary>
        public float value;
        /// <summary>Optional documentation or usage note.</summary>
        public string description;
    }

    /// <summary>
    /// Declarative specification payload for building a UIThemePalette asset.
    /// </summary>
    [Serializable]
    public class ThemeBuildSpec
    {
        /// <summary>Theme display name.</summary>
        public string name;
        /// <summary>Theme description.</summary>
        public string description;
        /// <summary>Collection of color tokens.</summary>
        public List<ColorTokenSpec> colorTokens = new List<ColorTokenSpec>();
        /// <summary>Collection of float/radius tokens.</summary>
        public List<FloatTokenSpec> floatTokens = new List<FloatTokenSpec>();
    }

    /// <summary>
    /// Declarative specification for a single CSS visual state override.
    /// </summary>
    [Serializable]
    public class VisualStateSpec
    {
        /// <summary>State name identifier (e.g. "Hover", "Pressed", "Selected").</summary>
        public string stateName = "Normal";
        /// <summary>Whether to override localScale.</summary>
        public bool overrideScale;
        /// <summary>Target scale components [x, y, z].</summary>
        public float[] scale;
        /// <summary>Whether to override anchored position offset.</summary>
        public bool overrideOffset;
        /// <summary>Target offset components [x, y].</summary>
        public float[] offset;
        /// <summary>Whether to override rotation.</summary>
        public bool overrideRotation;
        /// <summary>Target Z euler rotation in degrees (backward-compatible 2D accessor).</summary>
        public float rotation;
        /// <summary>Target 3D Euler rotation components [x, y, z] in degrees.</summary>
        public float[] rotationEuler;
        /// <summary>Whether to override Graphic color multiplier.</summary>
        public bool overrideColor;
        /// <summary>Target color tint hex string.</summary>
        public string colorMultiplierHex;
        /// <summary>Whether to override CanvasGroup alpha.</summary>
        public bool overrideAlpha;
        /// <summary>Target CanvasGroup alpha in range [0, 1].</summary>
        public float alpha = 1f;
        /// <summary>Transition duration in seconds.</summary>
        public float duration = 0.15f;
        /// <summary>Easing curve formula identifier (e.g. "linear", "easeInOut", "outQuad", "outBack", "bounce").</summary>
        public string ease = "easeInOut";
    }

    /// <summary>
    /// Declarative specification payload for building a UIAnimationClass asset.
    /// </summary>
    [Serializable]
    public class AnimationBuildSpec
    {
        /// <summary>Animation class asset file name.</summary>
        public string name;
        /// <summary>CSS class selector name (e.g. ".btn-interactive").</summary>
        public string className;
        /// <summary>Description and documentation.</summary>
        public string description;
        /// <summary>Interactive visual states.</summary>
        public List<VisualStateSpec> states = new List<VisualStateSpec>();

        /// <summary>Entrance / Mount motion clip specification.</summary>
        public MotionClipSpec entranceClip;
        /// <summary>Hover (:hover) motion clip specification.</summary>
        public MotionClipSpec hoverClip;
        /// <summary>Pressed (:active) motion clip specification.</summary>
        public MotionClipSpec pressedClip;
        /// <summary>Exit motion clip specification.</summary>
        public MotionClipSpec exitClip;
        /// <summary>Looping motion clip specification.</summary>
        public MotionClipSpec loopClip;
        /// <summary>Procedural click punch / shake specification.</summary>
        public PunchShakeSpec clickPunch;

        // Direct shortcut properties for quick scripting and backward-compatible CLI args
        public bool enableClickBounce = true;
        public float bounceMagnitude = 0.08f;
        public float bounceDuration = 0.22f;
        public string loopType = "None";
        public float loopSpeed = 1.5f;
        public float loopMagnitude = 0.05f;
        public bool playOnEnable = false;
        public float enterScaleFrom = 0.85f;
        public float enterFadeFrom = 0.0f;
        public float enterDuration = 0.25f;

        // Shortcut atomic channel configs
        public SlideSpec slide;
        public ScaleSpec scale;
        public RotateSpec rotate3d;
        public FadeSpec fade;
        public PunchShakeSpec punchShake;
        public float[] pivot;
    }

    [Serializable]
    public class TimingSpec
    {
        public bool overrideTiming;
        public float delay;
        public float duration;
        public string ease = "outCubic";
    }

    [Serializable]
    public class SlideSpec
    {
        public bool enabled;
        public string direction = "Custom";
        public float[] offset;
        public bool useScreenRelative;
        public TimingSpec timing;
    }

    [Serializable]
    public class ScaleSpec
    {
        public bool enabled;
        public float[] fromScale;
        public float[] toScale;
        public bool useIndependentAxisCurves;
        public TimingSpec timing;
    }

    [Serializable]
    public class RotateSpec
    {
        public bool enabled;
        public float[] fromEuler;
        public float[] toEuler;
        public bool isOscillating;
        public float oscillationCycles = 2.5f;
        public float damping = 1.5f;
        public TimingSpec timing;
    }

    [Serializable]
    public class FadeSpec
    {
        public bool enabled;
        public float fromAlpha;
        public float toAlpha = 1f;
        public TimingSpec timing;
    }

    [Serializable]
    public class PunchShakeSpec
    {
        public bool enabled;
        public float[] punchPosition;
        public float[] punchScale;
        public float punchRotation;
        public int vibrato = 8;
        public float elasticity = 0.85f;
        public float duration = 0.22f;
        public float delay;
    }

    [Serializable]
    public class MotionClipSpec
    {
        public bool enabled = true;
        public float duration = 0.35f;
        public float startDelay;
        public string ease = "outCubic";
        public string loopType = "None";
        public int loopCount = 1;
        public float loopDelay;
        public float[] pivot;
        public SlideSpec slide;
        public ScaleSpec scale;
        public RotateSpec rotate;
        public FadeSpec fade;
        public PunchShakeSpec punch;
    }

    /// <summary>
    /// Declarative specification payload for building a compound UIStyleClass asset.
    /// </summary>
    [Serializable]
    public class StyleBuildSpec
    {
        /// <summary>Style class asset file name.</summary>
        public string name;
        /// <summary>CSS class selector name (e.g. ".btn-primary").</summary>
        public string className;
        /// <summary>Description and documentation.</summary>
        public string description;
        /// <summary>Color token ID in active theme.</summary>
        public string colorToken = "primary";
        /// <summary>Border color token ID in active theme.</summary>
        public string borderColorToken = "border";
        /// <summary>Gradient end color token ID in active theme.</summary>
        public string gradientEndColorToken = "";
        /// <summary>Corner radius token ID in active theme.</summary>
        public string radiusToken = "radius-md";
        /// <summary>Border width token ID in active theme.</summary>
        public string borderWidthToken = "border-thin";
        /// <summary>Name of associated UIAnimationClass asset.</summary>
        public string animationClassName;
    }

    /// <summary>
    /// General result payload returned by styling pipeline builders.
    /// </summary>
    [Serializable]
    public class StylingBuildResult
    {
        /// <summary>True if asset creation and thumbnail baking succeeded.</summary>
        public bool success;
        /// <summary>Sanitized asset name.</summary>
        public string name;
        /// <summary>Project-relative asset path.</summary>
        public string assetPath;
        /// <summary>Project-relative thumbnail path.</summary>
        public string thumbPath;
        /// <summary>Error message if failed.</summary>
        public string error;
    }

    /// <summary>
    /// Backward-compatible alias for styling build result.
    /// </summary>
    [Serializable]
    public class BuildResult : StylingBuildResult
    {
    }
}
#endif
