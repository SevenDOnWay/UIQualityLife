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
        /// <summary>State identifier (e.g. Normal, Hover, Pressed, Focused, Disabled, Selected, or custom state).</summary>
        [Tooltip("State identifier (e.g. Normal, Hover, Pressed, Focused, Disabled, Selected, or any custom name)")]
        public string stateName = "Normal";

        /// <summary>If true, overrides the localScale when entering this state.</summary>
        [Header("Scale Override")]
        public bool overrideScale = false;
        /// <summary>Target 3D scale multiplier relative to the element's base scale.</summary>
        public Vector3 targetScale = Vector3.one;

        /// <summary>If true, overrides the anchored position offset when entering this state.</summary>
        [Header("Position Offset Override (Anchored)")]
        public bool overrideOffset = false;
        /// <summary>Target anchored position offset in pixels relative to base position.</summary>
        public Vector2 targetOffset = Vector2.zero;

        /// <summary>If true, overrides the Z euler rotation when entering this state.</summary>
        [Header("Z-Rotation Override")]
        public bool overrideRotation = false;
        /// <summary>Target Z-axis rotation angle in degrees.</summary>
        public float targetRotation = 0f;

        /// <summary>If true, multiplies Graphic color by <see cref="colorMultiplier"/> when entering this state.</summary>
        [Header("Graphic Color Multiplier Override")]
        public bool overrideColor = false;
        /// <summary>Color multiplier applied to target Graphic.</summary>
        public Color colorMultiplier = Color.white;

        /// <summary>If true, overrides CanvasGroup alpha when entering this state.</summary>
        [Header("CanvasGroup Alpha Override")]
        public bool overrideAlpha = false;
        /// <summary>Target CanvasGroup alpha in range [0, 1].</summary>
        [Range(0f, 1f)]
        public float targetAlpha = 1f;

        /// <summary>Transition interpolation duration in seconds.</summary>
        [Header("Transition Settings")]
        public float duration = 0.15f;
        /// <summary>Standard CSS / Penner easing curve type (OutQuad, OutBack, OutBounce, InOutCubic, etc.).</summary>
        [Tooltip("Standard CSS / Penner easing curve type (OutQuad, OutBack, OutBounce, InOutCubic, etc.)")]
        public UIEaseType easeType = UIEaseType.OutQuad;
        /// <summary>Optional custom animation curve override if easeType is set to CustomCurve.</summary>
        [Tooltip("Optional custom animation curve override if easeType is set to CustomCurve")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>Creates a default visual state named 'Normal'.</summary>
        public UIVisualState() { }

        /// <summary>
        /// Creates a visual state with a specified identifier, transition duration, and easing type.
        /// </summary>
        /// <param name="name">State identifier name (e.g. "Hover", "Pressed", "Selected").</param>
        /// <param name="transitionDuration">Transition duration in seconds.</param>
        /// <param name="easing">Easing curve type.</param>
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
        /// <param name="progress">Normalized time progress [0, 1].</param>
        /// <returns>Evaluated ease value.</returns>
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
