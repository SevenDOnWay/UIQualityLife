using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Generalized CSS-like Animation Class definition.
    /// Manages an extensible list of states (Normal, Hover, Pressed, Focused, Disabled, Selected, and custom states)
    /// plus atomic motion clips for Hover (:hover), Pressed (:active), Entrance (Mount), Exit, and Looping.
    /// Employs a Hybrid storage model: directly embeds atomic channel clips for zero-indirection Inspector editing,
    /// while also supporting links to shared <see cref="UIMotionProfile"/> assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUIAnimationClass", menuName = "UI Quality of Life/Styling/Animation Class")]
    public class UIAnimationClass : ScriptableObject
    {
        [Header("Class Metadata")]
        [Tooltip("CSS-like class name identifier (e.g. .btn-interactive, .card-elevated, .badge-pulse)")]
        [SerializeField] private string m_ClassName = ".btn-interactive";
        [TextArea(1, 3)]
        [SerializeField] private string m_Description = "Atomic multi-channel animation profile.";

        [Header("Hover Motion Clip (:hover)")]
        [Tooltip("If enabled, hovering over this element plays this full dynamic motion clip (e.g. RotateScaleUp, Swing, Pop).")]
        [SerializeField] private UIMotionProfile m_SharedHoverProfile;
        [SerializeField] private UIMotionClip m_HoverClip = new UIMotionClip();

        [Header("Pressed Motion Clip (:active)")]
        [Tooltip("If enabled, pressing down on this element plays this dynamic motion clip (e.g. RotateScaleDown, Squish).")]
        [SerializeField] private UIMotionProfile m_SharedPressedProfile;
        [SerializeField] private UIMotionClip m_PressedClip = new UIMotionClip();

        [Header("Entrance / Mount Motion (@keyframes / OnEnable)")]
        [Tooltip("If enabled, automatically plays this motion clip when the element is mounted/enabled.")]
        [SerializeField] private UIMotionProfile m_SharedEntranceProfile;
        [SerializeField] private UIMotionClip m_EntranceClip = new UIMotionClip();

        [Header("Exit Motion Clip")]
        [SerializeField] private UIMotionProfile m_SharedExitProfile;
        [SerializeField] private UIMotionClip m_ExitClip = new UIMotionClip();

        [Header("Continuous Loop Motion (@keyframes)")]
        [SerializeField] private UIMotionProfile m_SharedLoopProfile;
        [SerializeField] private UIMotionClip m_LoopClip = new UIMotionClip();

        [Header("Click Micro-Interaction (Punch / Shake)")]
        [SerializeField] private UIPunchShakeConfig m_ClickPunch = UIPunchShakeConfig.CreateDefaultClickBounce();

        [Header("Extensible Static Visual States (Fallback if clips disabled)")]
        [SerializeField] private List<UIVisualState> m_States = new List<UIVisualState>();

        /// <summary>
        /// Event broadcast whenever any animation class profile parameters change in editor or runtime.
        /// </summary>
        public static event Action OnAnimationClassChanged;

        public static void NotifyChanged() => OnAnimationClassChanged?.Invoke();

        #region Properties
        public string ClassName => m_ClassName;
        public string Description => m_Description;
        public IReadOnlyList<UIVisualState> States => m_States;

        /// <summary>Effective Hover motion clip (resolves shared profile or embedded clip).</summary>
        public UIMotionClip HoverClip => m_SharedHoverProfile != null ? m_SharedHoverProfile.Clip : m_HoverClip;
        /// <summary>Effective Pressed motion clip (resolves shared profile or embedded clip).</summary>
        public UIMotionClip PressedClip => m_SharedPressedProfile != null ? m_SharedPressedProfile.Clip : m_PressedClip;
        /// <summary>Effective Entrance motion clip (resolves shared profile or embedded clip).</summary>
        public UIMotionClip EntranceClip => m_SharedEntranceProfile != null ? m_SharedEntranceProfile.Clip : m_EntranceClip;
        /// <summary>Effective Exit motion clip (resolves shared profile or embedded clip).</summary>
        public UIMotionClip ExitClip => m_SharedExitProfile != null ? m_SharedExitProfile.Clip : m_ExitClip;
        /// <summary>Effective Loop motion clip (resolves shared profile or embedded clip).</summary>
        public UIMotionClip LoopClip => m_SharedLoopProfile != null ? m_SharedLoopProfile.Clip : m_LoopClip;
        public UIPunchShakeConfig ClickPunch => m_ClickPunch;

        public UIMotionProfile SharedHoverProfile => m_SharedHoverProfile;
        public UIMotionProfile SharedPressedProfile => m_SharedPressedProfile;
        public UIMotionProfile SharedEntranceProfile => m_SharedEntranceProfile;
        public UIMotionProfile SharedExitProfile => m_SharedExitProfile;
        public UIMotionProfile SharedLoopProfile => m_SharedLoopProfile;

        public float HoverScale => GetState("Hover") != null && GetState("Hover").overrideScale ? GetState("Hover").targetScale.x : 1.05f;
        public float PressScale => GetState("Pressed") != null && GetState("Pressed").overrideScale ? GetState("Pressed").targetScale.x : 0.95f;
        public MotionLoopType LoopType => LoopClip != null && LoopClip.enabled ? LoopClip.loopType : MotionLoopType.None;
        #endregion

        #region Default Initialization
        public void PopulateDefaultStatesIfEmpty()
        {
            if (m_States == null) m_States = new List<UIVisualState>();
            if (m_States.Count == 0)
            {
                m_States.Add(new UIVisualState("Normal", 0.15f));
                m_States.Add(new UIVisualState("Hover", 0.15f) { overrideScale = true, targetScale = new Vector3(1.05f, 1.05f, 1f), overrideOffset = true, targetOffset = new Vector2(0f, 2f) });
                m_States.Add(new UIVisualState("Pressed", 0.08f) { overrideScale = true, targetScale = new Vector3(0.95f, 0.95f, 1f), overrideOffset = true, targetOffset = new Vector2(0f, -2f) });
                m_States.Add(new UIVisualState("Focused", 0.12f) { overrideScale = true, targetScale = new Vector3(1.03f, 1.03f, 1f) });
                m_States.Add(new UIVisualState("Disabled", 0.15f) { overrideAlpha = true, targetAlpha = 0.45f, overrideScale = true, targetScale = new Vector3(0.98f, 0.98f, 1f) });
            }
        }
        #endregion

        #region Visual State Helpers
        public UIVisualState GetState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName) || m_States == null) return null;
            for (int i = 0; i < m_States.Count; i++)
            {
                if (string.Equals(m_States[i].stateName, stateName, StringComparison.OrdinalIgnoreCase))
                {
                    return m_States[i];
                }
            }
            return null;
        }

        public bool HasState(string stateName) => GetState(stateName) != null;

        public void SetState(UIVisualState state)
        {
            if (state == null || string.IsNullOrEmpty(state.stateName)) return;

            for (int i = 0; i < m_States.Count; i++)
            {
                if (string.Equals(m_States[i].stateName, state.stateName, StringComparison.OrdinalIgnoreCase))
                {
                    m_States[i] = state;
                    OnValidate();
                    return;
                }
            }
            m_States.Add(state);
            OnValidate();
        }

        public bool RemoveState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName)) return false;
            for (int i = 0; i < m_States.Count; i++)
            {
                if (string.Equals(m_States[i].stateName, stateName, StringComparison.OrdinalIgnoreCase))
                {
                    m_States.RemoveAt(i);
                    OnValidate();
                    return true;
                }
            }
            return false;
        }
        #endregion

        private void OnValidate()
        {
            OnAnimationClassChanged?.Invoke();
        }
    }
}
