using System;
using UnityEngine;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Complete set of Robert Penner / CSS web easing types matching easings.net standards.
    /// </summary>
    public enum UIEaseType
    {
        Linear = 0,

        // Sine
        InSine = 1,
        OutSine = 2,
        InOutSine = 3,

        // Quad
        InQuad = 4,
        OutQuad = 5,
        InOutQuad = 6,

        // Cubic
        InCubic = 7,
        OutCubic = 8,
        InOutCubic = 9,

        // Quart
        InQuart = 10,
        OutQuart = 11,
        InOutQuart = 12,

        // Quint
        InQuint = 13,
        OutQuint = 14,
        InOutQuint = 15,

        // Expo
        InExpo = 16,
        OutExpo = 17,
        InOutExpo = 18,

        // Circ
        InCirc = 19,
        OutCirc = 20,
        InOutCirc = 21,

        // Back (Overshoot)
        InBack = 22,
        OutBack = 23,
        InOutBack = 24,

        // Elastic (Spring oscillations)
        InElastic = 25,
        OutElastic = 26,
        InOutElastic = 27,

        // Bounce (Decaying collision bounces)
        InBounce = 28,
        OutBounce = 29,
        InOutBounce = 30,

        // Custom curve override
        CustomCurve = 99
    }

    /// <summary>
    /// Dedicated zero-dependency easing calculation engine for UGUI animations, transitions, and fades.
    /// Implements standard Robert Penner & CSS easing functions with 0 memory allocation.
    /// </summary>
    public static class UIEasing
    {
        private const float Pi = Mathf.PI;
        private const float HalfPi = Mathf.PI * 0.5f;
        private const float TwoPi = Mathf.PI * 2f;

        // Standard constants for Back & Elastic easings (matching CSS / easings.net)
        private const float C1 = 1.70158f;
        private const float C2 = C1 * 1.525f;
        private const float C3 = C1 + 1f;
        private const float C4 = (TwoPi) / 3f;
        private const float C5 = (TwoPi) / 4.5f;

        /// <summary>
        /// Evaluates the normalized progress t [0, 1] through the specified easing type.
        /// </summary>
        public static float Evaluate(UIEaseType easeType, float t, AnimationCurve customCurve = null)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            switch (easeType)
            {
                case UIEaseType.Linear:
                    return t;

                // Sine
                case UIEaseType.InSine:
                    return 1f - Mathf.Cos(t * HalfPi);
                case UIEaseType.OutSine:
                    return Mathf.Sin(t * HalfPi);
                case UIEaseType.InOutSine:
                    return -(Mathf.Cos(Pi * t) - 1f) * 0.5f;

                // Quad
                case UIEaseType.InQuad:
                    return t * t;
                case UIEaseType.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case UIEaseType.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

                // Cubic
                case UIEaseType.InCubic:
                    return t * t * t;
                case UIEaseType.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case UIEaseType.InOutCubic:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

                // Quart
                case UIEaseType.InQuart:
                    return t * t * t * t;
                case UIEaseType.OutQuart:
                    return 1f - Mathf.Pow(1f - t, 4f);
                case UIEaseType.InOutQuart:
                    return t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f;

                // Quint
                case UIEaseType.InQuint:
                    return t * t * t * t * t;
                case UIEaseType.OutQuint:
                    return 1f - Mathf.Pow(1f - t, 5f);
                case UIEaseType.InOutQuint:
                    return t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) * 0.5f;

                // Expo
                case UIEaseType.InExpo:
                    return t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case UIEaseType.OutExpo:
                    return t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case UIEaseType.InOutExpo:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return t < 0.5f
                        ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f
                        : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;

                // Circ
                case UIEaseType.InCirc:
                    return 1f - Mathf.Sqrt(1f - Mathf.Pow(t, 2f));
                case UIEaseType.OutCirc:
                    return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
                case UIEaseType.InOutCirc:
                    return t < 0.5f
                        ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) * 0.5f
                        : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) * 0.5f;

                // Back
                case UIEaseType.InBack:
                    return C3 * t * t * t - C1 * t * t;
                case UIEaseType.OutBack:
                    return 1f + C3 * Mathf.Pow(t - 1f, 3f) + C1 * Mathf.Pow(t - 1f, 2f);
                case UIEaseType.InOutBack:
                    return t < 0.5f
                        ? (Mathf.Pow(2f * t, 2f) * ((C2 + 1f) * 2f * t - C2)) * 0.5f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((C2 + 1f) * (t * 2f - 2f) + C2) + 2f) * 0.5f;

                // Elastic
                case UIEaseType.InElastic:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * C4);
                case UIEaseType.OutElastic:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * C4) + 1f;
                case UIEaseType.InOutElastic:
                    if (t == 0f) return 0f;
                    if (t == 1f) return 1f;
                    return t < 0.5f
                        ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * C5)) * 0.5f
                        : (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * C5)) * 0.5f + 1f;

                // Bounce
                case UIEaseType.InBounce:
                    return 1f - OutBounce(1f - t);
                case UIEaseType.OutBounce:
                    return OutBounce(t);
                case UIEaseType.InOutBounce:
                    return t < 0.5f
                        ? (1f - OutBounce(1f - 2f * t)) * 0.5f
                        : (1f + OutBounce(2f * t - 1f)) * 0.5f;

                case UIEaseType.CustomCurve:
                    return customCurve != null ? customCurve.Evaluate(t) : t;

                default:
                    return t;
            }
        }

        private static float OutBounce(float x)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (x < 1f / d1)
            {
                return n1 * x * x;
            }
            if (x < 2f / d1)
            {
                x -= 1.5f / d1;
                return n1 * x * x + 0.75f;
            }
            if (x < 2.5f / d1)
            {
                x -= 2.25f / d1;
                return n1 * x * x + 0.9375f;
            }

            x -= 2.625f / d1;
            return n1 * x * x + 0.984375f;
        }

        #region Interpolation Helpers (Float, Vector, Color)

        public static float Lerp(float from, float to, float t, UIEaseType ease, AnimationCurve curve = null)
        {
            float easedT = Evaluate(ease, t, curve);
            return from + (to - from) * easedT;
        }

        public static Vector2 Lerp(Vector2 from, Vector2 to, float t, UIEaseType ease, AnimationCurve curve = null)
        {
            float easedT = Evaluate(ease, t, curve);
            return Vector2.LerpUnclamped(from, to, easedT);
        }

        public static Vector3 Lerp(Vector3 from, Vector3 to, float t, UIEaseType ease, AnimationCurve curve = null)
        {
            float easedT = Evaluate(ease, t, curve);
            return Vector3.LerpUnclamped(from, to, easedT);
        }

        public static Color Lerp(Color from, Color to, float t, UIEaseType ease, AnimationCurve curve = null)
        {
            float easedT = Evaluate(ease, t, curve);
            return Color.LerpUnclamped(from, to, easedT);
        }

        #endregion

        #region Parsing & Utilities

        /// <summary>
        /// Parses a string (e.g. "easeOutQuad", "ease-in-out-cubic", "outBack", "bounce") into UIEaseType.
        /// </summary>
        public static UIEaseType Parse(string rawName, UIEaseType fallback = UIEaseType.OutQuad)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return fallback;

            string clean = rawName.Replace("-", "").Replace("_", "").Trim();
            if (clean.StartsWith("ease", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(4);
            }

            if (Enum.TryParse<UIEaseType>(clean, true, out var result))
            {
                return result;
            }

            // Common aliases
            if (string.Equals(clean, "easeIn", StringComparison.OrdinalIgnoreCase)) return UIEaseType.InQuad;
            if (string.Equals(clean, "easeOut", StringComparison.OrdinalIgnoreCase)) return UIEaseType.OutQuad;
            if (string.Equals(clean, "easeInOut", StringComparison.OrdinalIgnoreCase)) return UIEaseType.InOutQuad;
            if (string.Equals(clean, "spring", StringComparison.OrdinalIgnoreCase)) return UIEaseType.OutBack;
            if (string.Equals(clean, "bounce", StringComparison.OrdinalIgnoreCase)) return UIEaseType.OutBounce;
            if (string.Equals(clean, "elastic", StringComparison.OrdinalIgnoreCase)) return UIEaseType.OutElastic;

            return fallback;
        }

        /// <summary>
        /// Generates an AnimationCurve approximation of any mathematical ease type.
        /// Useful for inspecting or serializing mathematical curves as native Unity curves.
        /// </summary>
        public static AnimationCurve ToAnimationCurve(UIEaseType easeType, int samples = 20)
        {
            if (easeType == UIEaseType.Linear) return AnimationCurve.Linear(0f, 0f, 1f, 1f);

            Keyframe[] keys = new Keyframe[Mathf.Max(2, samples + 1)];
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float val = Evaluate(easeType, t);
                keys[i] = new Keyframe(t, val);
            }

            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }

            return curve;
        }

        #endregion
    }
}
