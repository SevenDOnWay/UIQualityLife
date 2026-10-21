using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIQualityLife.Styling
{
    public enum UILoopAnimationType
    {
        None = 0,
        Pulse = 1,       // Scale breathes smoothly
        Float = 2,       // Gentle up/down bobbing
        Shimmer = 3,     // Alpha/brightness breathes
        Shake = 4        // Subtle horizontal wiggle (e.g. attention/badge)
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
        [SerializeField] private AnimationCurve m_EnterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public static event Action OnAnimationClassChanged;

        #region Properties
        public string ClassName => m_ClassName;
        public IReadOnlyList<UIVisualState> States => m_States;

        public bool EnableClickBounce => m_EnableClickBounce;
        public float BounceMagnitude => m_BounceMagnitude;
        public float BounceDuration => m_BounceDuration;

        public UILoopAnimationType LoopType => m_LoopType;
        public float LoopSpeed => m_LoopSpeed;
        public float LoopMagnitude => m_LoopMagnitude;

        public bool PlayOnEnable => m_PlayOnEnable;
        public float EnterScaleFrom => m_EnterScaleFrom;
        public float EnterFadeFrom => m_EnterFadeFrom;
        public float EnterDuration => m_EnterDuration;
        public AnimationCurve EnterCurve => m_EnterCurve;
        #endregion

        #region State Queries
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

        public UIVisualState GetState(string stateName)
        {
            TryGetState(stateName, out var s);
            return s;
        }

        public bool HasState(string stateName)
        {
            return TryGetState(stateName, out _);
        }
        #endregion

        #region Backward Compatibility Accessors
        public bool EnableHover => HasState("Hover");
        public float HoverScale => TryGetState("Hover", out var s) && s.overrideScale ? s.targetScale.x : 1.05f;
        public Vector2 HoverOffset => TryGetState("Hover", out var s) && s.overrideOffset ? s.targetOffset : Vector2.zero;
        public float HoverDuration => TryGetState("Hover", out var s) ? s.duration : 0.15f;
        public AnimationCurve HoverCurve => TryGetState("Hover", out var s) ? s.curve : AnimationCurve.EaseInOut(0, 0, 1, 1);

        public bool EnablePress => HasState("Pressed");
        public float PressScale => TryGetState("Pressed", out var s) && s.overrideScale ? s.targetScale.x : 0.95f;
        public Vector2 PressOffset => TryGetState("Pressed", out var s) && s.overrideOffset ? s.targetOffset : Vector2.zero;
        public float PressDuration => TryGetState("Pressed", out var s) ? s.duration : 0.08f;
        public AnimationCurve PressCurve => TryGetState("Pressed", out var s) ? s.curve : AnimationCurve.EaseInOut(0, 0, 1, 1);
        #endregion

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
