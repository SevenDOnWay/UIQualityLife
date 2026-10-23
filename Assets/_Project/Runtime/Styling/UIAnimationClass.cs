using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Continuous looping keyframe animation types matching CSS @keyframes animations.
    /// </summary>
    public enum UILoopAnimationType
    {
        /// <summary>No looping keyframe animation.</summary>
        None = 0,
        /// <summary>Smooth harmonic breathing scale loop (e.g. attention pulse).</summary>
        Pulse = 1,
        /// <summary>Gentle sinusoidal vertical floating bobbing.</summary>
        Float = 2,
        /// <summary>Continuous breathing alpha shimmer.</summary>
        Shimmer = 3,
        /// <summary>Horizontal badge wiggle / notification shake.</summary>
        Shake = 4
    }

    /// <summary>
    /// Generalized CSS-like Animation Class definition.
    /// Manages an extensible list of states (Normal, Hover, Pressed, Focused, Disabled, Selected, and custom states)
    /// plus continuous keyframe loops and one-shot click effects.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUIAnimationClass", menuName = "UI Quality of Life/Styling/Animation Class")]
    public class UIAnimationClass : ScriptableObject
    {
        [Header("Class Metadata")]
        [Tooltip("CSS-like class name identifier (e.g. .btn-interactive, .card-elevated, .badge-pulse)")]
        [SerializeField] private string m_ClassName = ".btn-interactive";
        [TextArea(1, 3)]
        [SerializeField] private string m_Description = "Full state animation profile with hover, press, focus, disabled, and bounce.";

        [Header("Extensible CSS Visual States")]
        [SerializeField] private List<UIVisualState> m_States = new List<UIVisualState>();

        [Header("Click Micro-Bounce")]
        [SerializeField] private bool m_EnableClickBounce = true;
        [SerializeField] private float m_BounceMagnitude = 0.08f;
        [SerializeField] private float m_BounceDuration = 0.22f;

        [Header("CSS Keyframe Loop (@keyframes)")]
        [SerializeField] private UILoopAnimationType m_LoopType = UILoopAnimationType.None;
        [Range(0.1f, 5f)]
        [SerializeField] private float m_LoopSpeed = 1.5f;
        [Range(0.01f, 0.5f)]
        [SerializeField] private float m_LoopMagnitude = 0.05f;

        [Header("Mount / Enter Animation")]
        [SerializeField] private bool m_PlayOnEnable = false;
        [SerializeField] private float m_EnterScaleFrom = 0.85f;
        [SerializeField] private float m_EnterFadeFrom = 0.0f;
        [SerializeField] private float m_EnterDuration = 0.25f;
        [SerializeField] private UIEaseType m_EnterEaseType = UIEaseType.OutBack;
        [SerializeField] private AnimationCurve m_EnterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>
        /// Event broadcast whenever any animation class profile parameters change in editor or runtime.
        /// </summary>
        public static event Action OnAnimationClassChanged;

        #region Properties
        /// <summary>CSS class name selector identifier (e.g. ".btn-interactive").</summary>
        public string ClassName => m_ClassName;
        /// <summary>Description and notes for this animation profile.</summary>
        public string Description => m_Description;
        /// <summary>Read-only collection of all visual states configured in this animation class.</summary>
        public IReadOnlyList<UIVisualState> States => m_States;

        /// <summary>Whether a micro spring bounce is triggered on click / pointer down.</summary>
        public bool EnableClickBounce => m_EnableClickBounce;
        /// <summary>Overshoot scale magnitude for click micro-bounce.</summary>
        public float BounceMagnitude => m_BounceMagnitude;
        /// <summary>Duration of click bounce effect in seconds.</summary>
        public float BounceDuration => m_BounceDuration;

        /// <summary>Continuous keyframe loop type (Pulse, Float, Shimmer, Shake, or None).</summary>
        public UILoopAnimationType LoopType => m_LoopType;
        /// <summary>Frequency / cycle speed of continuous keyframe loop.</summary>
        public float LoopSpeed => m_LoopSpeed;
        /// <summary>Displacement or scale magnitude of continuous loop.</summary>
        public float LoopMagnitude => m_LoopMagnitude;

        /// <summary>If true, automatically plays the entrance transition on component mount/enable.</summary>
        public bool PlayOnEnable => m_PlayOnEnable;
        /// <summary>Starting uniform scale multiplier for entrance animation.</summary>
        public float EnterScaleFrom => m_EnterScaleFrom;
        /// <summary>Starting alpha value for entrance animation.</summary>
        public float EnterFadeFrom => m_EnterFadeFrom;
        /// <summary>Duration of entrance animation in seconds.</summary>
        public float EnterDuration => m_EnterDuration;
        /// <summary>Easing type used for entrance animation.</summary>
        public UIEaseType EnterEaseType => m_EnterEaseType;
        /// <summary>Custom animation curve used if EnterEaseType is CustomCurve.</summary>
        public AnimationCurve EnterCurve => m_EnterCurve;

        /// <summary>
        /// Evaluates normalized enter transition progress [0, 1] through the configured entrance easing.
        /// </summary>
        /// <param name="progress">Normalized time progress [0, 1].</param>
        /// <returns>Evaluated ease value.</returns>
        public float EvaluateEnterProgress(float progress)
        {
            if (progress <= 0f) return 0f;
            if (progress >= 1f) return 1f;

            if (m_EnterEaseType == UIEaseType.CustomCurve && m_EnterCurve != null)
            {
                return m_EnterCurve.Evaluate(progress);
            }

            return UIEasing.Evaluate(m_EnterEaseType, progress);
        }
        #endregion

        #region State Queries
        /// <summary>
        /// Attempts to find a configured visual state by name.
        /// </summary>
        /// <param name="stateName">State identifier (e.g. "Hover", "Pressed", "Selected").</param>
        /// <param name="state">Found visual state, or null.</param>
        /// <returns>True if state was found; otherwise false.</returns>
        public bool TryGetState(string stateName, out UIVisualState state)
        {
            if (m_States != null && !string.IsNullOrEmpty(stateName))
            {
                for (int i = 0; i < m_States.Count; i++)
                {
                    if (string.Equals(m_States[i].stateName, stateName, StringComparison.OrdinalIgnoreCase))
                    {
                        state = m_States[i];
                        return true;
                    }
                }
            }
            state = null;
            return false;
        }

        /// <summary>
        /// Retrieves a visual state by name, returning null if not configured.
        /// </summary>
        /// <param name="stateName">State identifier.</param>
        /// <returns>Matching <see cref="UIVisualState"/> or null.</returns>
        public UIVisualState GetState(string stateName)
        {
            TryGetState(stateName, out var s);
            return s;
        }

        /// <summary>
        /// Checks whether this animation class contains a state with the given name.
        /// </summary>
        /// <param name="stateName">State identifier.</param>
        /// <returns>True if configured; otherwise false.</returns>
        public bool HasState(string stateName)
        {
            return TryGetState(stateName, out _);
        }
        #endregion

        #region Backward Compatibility Accessors
        /// <summary>Indicates whether a 'Hover' state is defined.</summary>
        public bool EnableHover => HasState("Hover");
        /// <summary>Scale for hover state, defaulting to 1.05f if not overridden.</summary>
        public float HoverScale => TryGetState("Hover", out var s) && s.overrideScale ? s.targetScale.x : 1.05f;
        /// <summary>Anchored offset for hover state, defaulting to Vector2.zero.</summary>
        public Vector2 HoverOffset => TryGetState("Hover", out var s) && s.overrideOffset ? s.targetOffset : Vector2.zero;
        /// <summary>Duration in seconds for hover transition.</summary>
        public float HoverDuration => TryGetState("Hover", out var s) ? s.duration : 0.15f;
        /// <summary>Animation curve for hover transition.</summary>
        public AnimationCurve HoverCurve => TryGetState("Hover", out var s) ? s.curve : AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>Indicates whether a 'Pressed' state is defined.</summary>
        public bool EnablePress => HasState("Pressed");
        /// <summary>Scale for pressed state, defaulting to 0.95f if not overridden.</summary>
        public float PressScale => TryGetState("Pressed", out var s) && s.overrideScale ? s.targetScale.x : 0.95f;
        /// <summary>Anchored offset for pressed state, defaulting to Vector2.zero.</summary>
        public Vector2 PressOffset => TryGetState("Pressed", out var s) && s.overrideOffset ? s.targetOffset : Vector2.zero;
        /// <summary>Duration in seconds for press transition.</summary>
        public float PressDuration => TryGetState("Pressed", out var s) ? s.duration : 0.08f;
        /// <summary>Animation curve for press transition.</summary>
        public AnimationCurve PressCurve => TryGetState("Pressed", out var s) ? s.curve : AnimationCurve.EaseInOut(0, 0, 1, 1);
        #endregion

        /// <summary>
        /// Populates standard interactive states (Normal, Hover, Pressed, Focused, Selected, Disabled) if list is empty.
        /// </summary>
        public void PopulateDefaultStatesIfEmpty()
        {
            if (m_States.Count > 0) return;

            // 1. Normal State
            m_States.Add(new UIVisualState("Normal", 0.15f));

            // 2. Hover State (:hover)
            var hover = new UIVisualState("Hover", 0.15f)
            {
                overrideScale = true,
                targetScale = new Vector3(1.05f, 1.05f, 1f),
                overrideOffset = true,
                targetOffset = new Vector2(0f, 2f),
                overrideColor = true,
                colorMultiplier = new Color(1.1f, 1.1f, 1.1f, 1f)
            };
            m_States.Add(hover);

            // 3. Pressed State (:active)
            var pressed = new UIVisualState("Pressed", 0.08f)
            {
                overrideScale = true,
                targetScale = new Vector3(0.95f, 0.95f, 1f),
                overrideOffset = true,
                targetOffset = new Vector2(0f, -2f),
                overrideColor = true,
                colorMultiplier = new Color(0.85f, 0.85f, 0.85f, 1f)
            };
            m_States.Add(pressed);

            // 4. Focused State (:focus for gamepad/keyboard)
            var focused = new UIVisualState("Focused", 0.12f)
            {
                overrideScale = true,
                targetScale = new Vector3(1.03f, 1.03f, 1f),
                overrideColor = true,
                colorMultiplier = new Color(1.15f, 1.15f, 1.25f, 1f)
            };
            m_States.Add(focused);

            // 5. Selected State (Active tab / Checked toggle)
            var selected = new UIVisualState("Selected", 0.15f)
            {
                overrideScale = true,
                targetScale = new Vector3(1.02f, 1.02f, 1f),
                overrideColor = true,
                colorMultiplier = new Color(0.9f, 1.15f, 1.2f, 1f)
            };
            m_States.Add(selected);

            // 6. Disabled State (:disabled)
            var disabled = new UIVisualState("Disabled", 0.15f)
            {
                overrideAlpha = true,
                targetAlpha = 0.45f,
                overrideScale = true,
                targetScale = new Vector3(0.98f, 0.98f, 1f)
            };
            m_States.Add(disabled);
        }

        private void Reset()
        {
            PopulateDefaultStatesIfEmpty();
        }

        private void OnValidate()
        {
            if (m_BounceDuration < 0.01f) m_BounceDuration = 0.01f;
            if (m_EnterDuration < 0.01f) m_EnterDuration = 0.01f;

            for (int i = 0; i < m_States.Count; i++)
            {
                if (m_States[i].duration < 0.01f) m_States[i].duration = 0.01f;
            }

            OnAnimationClassChanged?.Invoke();
        }
    }
}
