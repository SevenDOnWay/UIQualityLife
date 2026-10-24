using System;
using UnityEngine;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Motion direction for entrance/exit transitions.
    /// </summary>
    public enum MotionPlayMode
    {
        /// <summary>Interpolates from configured offset/scale/rotation towards rest state (1.0 scale, 0 offset, 0 rotation).</summary>
        Entrance = 0,
        /// <summary>Interpolates from rest state towards configured offset/scale/rotation.</summary>
        Exit = 1
    }

    /// <summary>
    /// Cyclic loop modes matching CSS animation-iteration-count and direction.
    /// </summary>
    public enum MotionLoopType
    {
        /// <summary>Plays once and terminates at destination.</summary>
        None = 0,
        /// <summary>Restarts from 0% each cycle (e.g. continuous spin).</summary>
        Restart = 1,
        /// <summary>Ping-pongs back and forth between 0% and 100% (e.g. breathing pulse, hover floating).</summary>
        Yoyo = 2
    }

    /// <summary>
    /// Standard slide directions relative to the UI element or canvas viewport.
    /// </summary>
    public enum SlideDirection
    {
        Custom = 0,
        Left = 1,
        Right = 2,
        Top = 3,
        Bottom = 4
    }

    /// <summary>
    /// Allows any atomic channel to decouple its own delay, duration, and easing from the clip's global timing.
    /// Matches CSS independent property transitions (e.g. 'transition: opacity 0.15s ease, transform 0.4s cubic-bezier(...)').
    /// </summary>
    [Serializable]
    public struct ChannelTimingOverride
    {
        [Tooltip("If true, this channel runs with its own delay, duration, and curve rather than the clip's global timing.")]
        public bool overrideTiming;
        [Tooltip("Start delay in seconds before this channel starts tweening.")]
        public float delay;
        [Tooltip("Duration in seconds for this channel's transition.")]
        public float duration;
        [Tooltip("Easing formula for this channel.")]
        public UIEaseType easeType;
        [Tooltip("Custom animation curve if easeType is CustomCurve.")]
        public AnimationCurve customCurve;

        public float GetDuration(float defaultDuration) =>
            overrideTiming && duration > 0.001f ? duration : defaultDuration;

        public float GetDelay() => overrideTiming ? Mathf.Max(0f, delay) : 0f;

        public float EvaluateProgress(float normalizedTime, UIEaseType defaultEase, AnimationCurve defaultCurve)
        {
            UIEaseType activeEase = overrideTiming ? easeType : defaultEase;
            AnimationCurve activeCurve = overrideTiming ? customCurve : defaultCurve;

            if (activeEase == UIEaseType.CustomCurve && activeCurve != null)
            {
                return activeCurve.Evaluate(normalizedTime);
            }
            return UIEasing.Evaluate(activeEase, normalizedTime);
        }
    }

    /// <summary>
    /// Atomic translation / slide channel for RectTransform anchoredPosition.
    /// </summary>
    [Serializable]
    public struct UISlideConfig
    {
        public bool enabled;
        public MotionPlayMode playMode;
        public SlideDirection direction;
        public Vector2 customOffset;
        [Tooltip("If true, multiplies direction by Screen width/height to slide fully off-screen.")]
        public bool useScreenRelative;
        public ChannelTimingOverride timing;

        public Vector2 CalculateStartOffset(RectTransform rectTransform)
        {
            if (direction == SlideDirection.Custom) return customOffset;

            float w = rectTransform != null ? rectTransform.rect.width : 200f;
            float h = rectTransform != null ? rectTransform.rect.height : 80f;

            if (useScreenRelative)
            {
                w = Screen.width;
                h = Screen.height;
            }

            switch (direction)
            {
                case SlideDirection.Left: return new Vector2(-w, 0f);
                case SlideDirection.Right: return new Vector2(w, 0f);
                case SlideDirection.Top: return new Vector2(0f, h);
                case SlideDirection.Bottom: return new Vector2(0f, -h);
                default: return customOffset;
            }
        }
    }

    /// <summary>
    /// Atomic scaling channel with support for independent axis curves (squash & stretch volume preservation).
    /// </summary>
    [Serializable]
    public struct UIScaleConfig
    {
        public bool enabled;
        public MotionPlayMode playMode;
        public Vector3 fromScale;
        public Vector3 toScale;

        [Header("Independent Axis Curves (Squash & Stretch)")]
        [Tooltip("If true, X and Y scale use independent curves (e.g. for rubberBand / jello volume preservation).")]
        public bool useIndependentAxisCurves;
        public AnimationCurve curveX;
        public AnimationCurve curveY;

        public ChannelTimingOverride timing;

        public static UIScaleConfig CreateDefaultEntrance(float startUniform = 0.5f)
        {
            return new UIScaleConfig
            {
                enabled = true,
                playMode = MotionPlayMode.Entrance,
                fromScale = new Vector3(startUniform, startUniform, 1f),
                toScale = Vector3.one,
                curveX = AnimationCurve.EaseInOut(0, 0, 1, 1),
                curveY = AnimationCurve.EaseInOut(0, 0, 1, 1)
            };
        }
    }

    /// <summary>
    /// Atomic 3D rotation channel supporting Euler rotations (e.g. 3D Card Flip on Y axis, Door Swing on Y, or 2D tilt on Z)
    /// and decaying harmonic pendulum oscillation.
    /// </summary>
    [Serializable]
    public struct UIRotateConfig
    {
        public bool enabled;
        public MotionPlayMode playMode;
        [Tooltip("Starting Euler angles in degrees (e.g. (0, 90, 0) for 3D card flip, (0, 0, 30) for tilt/swing).")]
        public Vector3 fromEuler;
        [Tooltip("Destination Euler angles in degrees (typically (0, 0, 0)).")]
        public Vector3 toEuler;

        [Header("Harmonic Pendulum Oscillation (Swing)")]
        [Tooltip("If true, performs a decaying sinusoidal swing instead of linear interpolation.")]
        public bool isOscillating;
        [Tooltip("Number of back-and-forth swing cycles.")]
        public float oscillationCycles;
        [Tooltip("Damping decay exponent. Higher values stop oscillating sooner.")]
        [Range(0.5f, 4f)]
        public float damping;

        public ChannelTimingOverride timing;

        public static UIRotateConfig CreateDefaultSwing(float maxAngleZ = 25f, float cycles = 2.5f)
        {
            return new UIRotateConfig
            {
                enabled = true,
                playMode = MotionPlayMode.Entrance,
                fromEuler = new Vector3(0f, 0f, maxAngleZ),
                toEuler = Vector3.zero,
                isOscillating = true,
                oscillationCycles = cycles,
                damping = 1.5f
            };
        }

        public static UIRotateConfig CreateDefaultCardFlipY(float startAngleY = 90f)
        {
            return new UIRotateConfig
            {
                enabled = true,
                playMode = MotionPlayMode.Entrance,
                fromEuler = new Vector3(0f, startAngleY, 0f),
                toEuler = Vector3.zero,
                isOscillating = false
            };
        }
    }

    /// <summary>
    /// Atomic CanvasGroup alpha fade channel.
    /// </summary>
    [Serializable]
    public struct UIFadeConfig
    {
        public bool enabled;
        public MotionPlayMode playMode;
        [Range(0f, 1f)] public float fromAlpha;
        [Range(0f, 1f)] public float toAlpha;
        public ChannelTimingOverride timing;

        public static UIFadeConfig CreateDefaultEntrance(float startAlpha = 0f)
        {
            return new UIFadeConfig
            {
                enabled = true,
                playMode = MotionPlayMode.Entrance,
                fromAlpha = startAlpha,
                toAlpha = 1f
            };
        }
    }

    /// <summary>
    /// Atomic Graphic color multiplier channel.
    /// </summary>
    [Serializable]
    public struct UIColorConfig
    {
        public bool enabled;
        public MotionPlayMode playMode;
        public Color fromColor;
        public Color toColor;
        public ChannelTimingOverride timing;
    }

    /// <summary>
    /// Additive procedural damped punch & shake channel.
    /// Ideal for error headShake, button click rubberBand, and attention wobbles.
    /// </summary>
    [Serializable]
    public struct UIPunchShakeConfig
    {
        public bool enabled;
        [Tooltip("Additive positional vibration (e.g. (15, 0, 0) for horizontal error headShake).")]
        public Vector3 punchPosition;
        [Tooltip("Additive scale impulse (e.g. (0.25, -0.25, 0) for rubberBand squash & stretch).")]
        public Vector3 punchScale;
        [Tooltip("Additive Z-axis rotation vibration in degrees.")]
        public float punchRotation;
        [Tooltip("Vibration frequency / oscillations per second (default ~10).")]
        [Range(1, 30)]
        public int vibrato;
        [Tooltip("Elasticity decay factor in range [0, 1]. 1 = bouncy spring, 0 = rapid deadening.")]
        [Range(0f, 1f)]
        public float elasticity;
        [Tooltip("Duration of the punch/shake effect in seconds.")]
        public float duration;
        public float delay;

        public static UIPunchShakeConfig CreateDefaultClickBounce(float magnitude = 0.08f, float duration = 0.22f)
        {
            return new UIPunchShakeConfig
            {
                enabled = true,
                punchScale = new Vector3(magnitude, -magnitude * 0.5f, 0f),
                vibrato = 8,
                elasticity = 0.85f,
                duration = duration
            };
        }

        public static UIPunchShakeConfig CreateDefaultHeadShake(float distance = 16f, float duration = 0.35f)
        {
            return new UIPunchShakeConfig
            {
                enabled = true,
                punchPosition = new Vector3(distance, 0f, 0f),
                vibrato = 12,
                elasticity = 0.7f,
                duration = duration
            };
        }
    }

    /// <summary>
    /// Configures transform-origin (pivot) with automatic runtime position compensation to eliminate visual snapping.
    /// </summary>
    [Serializable]
    public struct UIPivotConfig
    {
        [Tooltip("If true, temporarily overrides RectTransform.pivot during animation with automatic delta position compensation.")]
        public bool overridePivot;
        [Tooltip("Pivot target (e.g. (0.5, 1) for top swing pendulum, (0, 0.5) for left drawer).")]
        public Vector2 targetPivot;
    }

    /// <summary>
    /// Complete composite motion clip combining atomic channels into a reusable animation block.
    /// </summary>
    [Serializable]
    public class UIMotionClip
    {
        public bool enabled = false;
        [Tooltip("Global transition duration in seconds (overridden if individual channels specify timing).")]
        public float duration = 0.35f;
        [Tooltip("Initial start delay before animation begins.")]
        public float startDelay = 0f;
        [Tooltip("Global easing formula.")]
        public UIEaseType easeType = UIEaseType.OutCubic;
        public AnimationCurve customCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Loop Settings")]
        public MotionLoopType loopType = MotionLoopType.None;
        [Tooltip("Number of loop cycles. Set to -1 for infinite loops.")]
        public int loopCount = 1;
        [Tooltip("Delay between repeat loops in seconds.")]
        public float loopDelay = 0f;

        [Header("Transform Origin")]
        public UIPivotConfig pivot;

        [Header("Atomic Channels")]
        public UISlideConfig slide;
        public UIScaleConfig scale;
        public UIRotateConfig rotate;
        public UIFadeConfig fade;
        public UIColorConfig color;
        public UIPunchShakeConfig punch;

        public float GetTotalDuration()
        {
            float max = duration;
            if (slide.enabled && slide.timing.overrideTiming) max = Mathf.Max(max, slide.timing.delay + slide.timing.duration);
            if (scale.enabled && scale.timing.overrideTiming) max = Mathf.Max(max, scale.timing.delay + scale.timing.duration);
            if (rotate.enabled && rotate.timing.overrideTiming) max = Mathf.Max(max, rotate.timing.delay + rotate.timing.duration);
            if (fade.enabled && fade.timing.overrideTiming) max = Mathf.Max(max, fade.timing.delay + fade.timing.duration);
            if (color.enabled && color.timing.overrideTiming) max = Mathf.Max(max, color.timing.delay + color.timing.duration);
            if (punch.enabled) max = Mathf.Max(max, punch.delay + punch.duration);
            return startDelay + max;
        }
    }
}
