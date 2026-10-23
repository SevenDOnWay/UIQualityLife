using System;
using UnityEngine;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Represents a CSS-like visual state (:hover, :active, :focus, :disabled, :checked, or custom).
    /// All property overrides are optional: if not overridden, the property rests at its base value.
    /// </summary>
    [Serializable]
    public class UIVisualState
    {
        [Tooltip("State identifier (e.g. Normal, Hover, Pressed, Focused, Disabled, Selected, or any custom name)")]
        public string stateName = "Normal";

        [Header("Scale Override")]
        public bool overrideScale = false;
        public Vector3 targetScale = Vector3.one;

        [Header("Position Offset Override (Anchored)")]
        public bool overrideOffset = false;
        public Vector2 targetOffset = Vector2.zero;

        [Header("Z-Rotation Override")]
        public bool overrideRotation = false;
        public float targetRotation = 0f;

        [Header("Graphic Color Multiplier Override")]
        public bool overrideColor = false;
        public Color colorMultiplier = Color.white;

        [Header("CanvasGroup Alpha Override")]
        public bool overrideAlpha = false;
        [Range(0f, 1f)]
        public float targetAlpha = 1f;

        [Header("Transition Settings")]
        public float duration = 0.15f;
        [Tooltip("Standard CSS / Penner easing curve type (OutQuad, OutBack, OutBounce, InOutCubic, etc.)")]
        public UIEaseType easeType = UIEaseType.OutQuad;
        [Tooltip("Optional custom animation curve override if easeType is set to CustomCurve")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public UIVisualState() { }

        public UIVisualState(string name, float transitionDuration = 0.15f, UIEaseType easing = UIEaseType.OutQuad)
        {
            stateName = name;
            duration = transitionDuration;
            easeType = easing;
            curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        /// <summary>
        /// Evaluates normalized transition progress [0, 1] through the configured easing curve.
        /// Zero allocation and high performance.
        /// </summary>
        public float EvaluateProgress(float progress)
        {
            if (progress <= 0f) return 0f;
            if (progress >= 1f) return 1f;

            if (easeType == UIEaseType.CustomCurve && curve != null)
            {
                return curve.Evaluate(progress);
            }

            return UIEasing.Evaluate(easeType, progress);
        }
    }
}
