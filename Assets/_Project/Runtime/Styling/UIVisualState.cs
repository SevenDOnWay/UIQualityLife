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
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public UIVisualState() { }

        public UIVisualState(string name, float transitionDuration = 0.15f)
        {
            stateName = name;
            duration = transitionDuration;
            curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }
}
