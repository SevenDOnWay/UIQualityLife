#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;

namespace UIQualityLife.Editor.Styling
{
    /// <summary>
    /// Editor-only motion simulation driver for <see cref="UIClassAnimator"/>.
    /// Handles interactive state previews, click bounce physics, and keyframe loops
    /// inside the Scene View without entering Play Mode.
    /// Follows the Single Responsibility Principle (SRP) by completely decoupling
    /// editor simulation state from runtime data.
    /// </summary>
    [InitializeOnLoad]
    public class UIClassAnimatorEditorDriver
    {
        private static readonly Dictionary<UIClassAnimator, UIClassAnimatorEditorDriver> s_Drivers =
            new Dictionary<UIClassAnimator, UIClassAnimatorEditorDriver>();

        static UIClassAnimatorEditorDriver()
        {
            UIClassAnimatorMotionBridge.PreviewStateHandler = PreviewState;
            UIClassAnimatorMotionBridge.PlayEntranceHandler = PlayEntrance;
            UIClassAnimatorMotionBridge.PlayHoverHandler = PlayHover;
            UIClassAnimatorMotionBridge.PlayPressedHandler = PlayPressed;
            UIClassAnimatorMotionBridge.PlayExitHandler = PlayExit;
            UIClassAnimatorMotionBridge.ResetHandler = Reset;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // Reset all active editor previews and purge stale destroyed references across Play Mode transitions
            foreach (var driver in s_Drivers.Values)
            {
                if (driver != null)
                {
                    driver.DoReset();
                }
            }
            s_Drivers.Clear();
        }

        public static UIClassAnimatorEditorDriver GetOrCreate(UIClassAnimator target)
        {
            if (target == null || (UnityEngine.Object)target == null) return null;

            if (s_Drivers.TryGetValue(target, out var driver))
            {
                if (driver != null && driver.m_Target != null && (UnityEngine.Object)driver.m_Target == target)
                {
                    return driver;
                }
                s_Drivers.Remove(target);
            }

            driver = new UIClassAnimatorEditorDriver(target);
            s_Drivers[target] = driver;
            return driver;
        }

        public static void PreviewState(UIClassAnimator target, string stateName)
        {
            var driver = GetOrCreate(target);
            driver?.DoPreviewState(stateName);
        }

        public static void PlayEntrance(UIClassAnimator target)
        {
            var driver = GetOrCreate(target);
            driver?.DoPlayEntrance();
        }

        public static void PlayHover(UIClassAnimator target)
        {
            var driver = GetOrCreate(target);
            driver?.DoPlayHover();
        }

        public static void PlayPressed(UIClassAnimator target)
        {
            var driver = GetOrCreate(target);
            driver?.DoPlayPressed();
        }

        public static void PlayExit(UIClassAnimator target)
        {
            var driver = GetOrCreate(target);
            driver?.DoPlayExit();
        }

        public static void Reset(UIClassAnimator target)
        {
            if (target != null && s_Drivers.TryGetValue(target, out var driver))
            {
                driver.DoReset();
            }
        }

        // Instance fields
        private readonly UIClassAnimator m_Target;
        private double m_LastEditorTime;
        private float m_EditorTransitionElapsed = 0f;
        private float m_EditorTransitionDuration = 0.15f;
        private Vector3 m_EditorStartScale = Vector3.one;
        private Vector3 m_EditorTargetScale = Vector3.one;
        private Vector2 m_EditorStartOffset = Vector2.zero;
        private Vector2 m_EditorTargetOffset = Vector2.zero;
        private float m_EditorStartRot = 0f;
        private float m_EditorTargetRot = 0f;
        private Color m_EditorStartColor = Color.white;
        private Color m_EditorTargetColor = Color.white;
        private float m_EditorStartAlpha = 1f;
        private float m_EditorTargetAlpha = 1f;
        private UIEaseType m_EditorEaseType = UIEaseType.OutQuad;
        private AnimationCurve m_EditorTransitionCurve;
        private bool m_IsEditorTransitioning = false;
        private bool m_IsEditorBouncing = false;
        private UIMotionClip m_ActiveEditorClip = null;
        private float m_EditorClipElapsed = 0f;
        private float m_LoopTimer = 0f;

        public UIClassAnimatorEditorDriver(UIClassAnimator target)
        {
            m_Target = target;
        }

        public void DoPlayEntrance()
        {
            if (m_Target == null || m_Target.AnimationClass == null) return;
            DoPlayClip(m_Target.AnimationClass.EntranceClip);
        }

        public void DoPlayHover()
        {
            if (m_Target == null || m_Target.AnimationClass == null) return;
            DoPlayClip(m_Target.AnimationClass.HoverClip);
        }

        public void DoPlayPressed()
        {
            if (m_Target == null || m_Target.AnimationClass == null) return;
            DoPlayClip(m_Target.AnimationClass.PressedClip);
        }

        public void DoPlayExit()
        {
            if (m_Target == null || m_Target.AnimationClass == null) return;
            DoPlayClip(m_Target.AnimationClass.ExitClip);
        }

        public void DoPlayClip(UIMotionClip clip)
        {
            if (m_Target == null) return;
            m_Target.Initialize();
            if (clip == null || !clip.enabled) return;
            m_ActiveEditorClip = clip;
            m_EditorClipElapsed = 0f;
            EnsureEditorUpdateHook();
        }

        public void DoPreviewState(string stateName)
        {
            if (m_Target == null) return;
            m_Target.Initialize();
            m_Target.SetCurrentStateName(stateName);

            UIVisualState state = (m_Target.AnimationClass != null) ? m_Target.AnimationClass.GetState(stateName) : null;
            m_EditorTargetScale = (state != null && state.overrideScale) ? state.targetScale : Vector3.one;
            m_EditorTargetOffset = (state != null && state.overrideOffset) ? state.targetOffset : Vector2.zero;
            m_EditorTargetRot = (state != null && state.overrideRotation) ? state.targetRotation : 0f;
            m_EditorTargetColor = (state != null && state.overrideColor) ? state.colorMultiplier : Color.white;
            m_EditorTargetAlpha = (state != null && state.overrideAlpha) ? state.targetAlpha : m_Target.BaseAlpha;

            m_EditorStartScale = m_Target.CurrentScaleMultiplier;
            m_EditorStartOffset = m_Target.CurrentOffset;
            m_EditorStartRot = m_Target.CurrentRotationZ;
            m_EditorStartColor = m_Target.CurrentColorTint;
            m_EditorStartAlpha = m_Target.CurrentAlpha;

            m_EditorTransitionDuration = (state != null) ? state.duration : 0.15f;
            m_EditorEaseType = (state != null) ? state.easeType : UIEaseType.OutQuad;
            m_EditorTransitionCurve = (state != null) ? state.curve : null;
            m_EditorTransitionElapsed = 0f;
            m_IsEditorTransitioning = true;

            EnsureEditorUpdateHook();
        }

        public void DoReset()
        {
            m_IsEditorTransitioning = false;
            m_ActiveEditorClip = null;
            m_EditorClipElapsed = 0f;
            EditorApplication.update -= EditorUpdateTick;
            if (m_Target != null && (UnityEngine.Object)m_Target != null)
            {
                m_Target.ResetToRest();
                if (m_Target.TargetTransform != null) EditorUtility.SetDirty(m_Target.TargetTransform);
            }
            SceneView.RepaintAll();
        }

        private void EnsureEditorUpdateHook()
        {
            m_LastEditorTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= EditorUpdateTick;
            EditorApplication.update += EditorUpdateTick;
        }

        private void EditorUpdateTick()
        {
            if (m_Target == null || (UnityEngine.Object)m_Target == null || !m_Target.gameObject || Application.isPlaying)
            {
                EditorApplication.update -= EditorUpdateTick;
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;
            float dt = (float)(currentTime - m_LastEditorTime);
            m_LastEditorTime = currentTime;
            if (dt <= 0f || dt > 0.2f) dt = 0.016f;

            bool activeWork = false;

            // 1. Transition
            if (m_IsEditorTransitioning)
            {
                activeWork = true;
                m_EditorTransitionElapsed += dt;
                float progress = Mathf.Clamp01(m_EditorTransitionElapsed / m_EditorTransitionDuration);
                float t = UIEasing.Evaluate(m_EditorEaseType, progress, (m_EditorEaseType == UIEaseType.CustomCurve) ? m_EditorTransitionCurve : null);

                m_Target.CurrentScaleMultiplier = Vector3.LerpUnclamped(m_EditorStartScale, m_EditorTargetScale, t);
                m_Target.CurrentOffset = Vector2.LerpUnclamped(m_EditorStartOffset, m_EditorTargetOffset, t);
                m_Target.CurrentRotationZ = Mathf.LerpUnclamped(m_EditorStartRot, m_EditorTargetRot, t);
                m_Target.CurrentColorTint = Color.LerpUnclamped(m_EditorStartColor, m_EditorTargetColor, t);
                m_Target.CurrentAlpha = Mathf.LerpUnclamped(m_EditorStartAlpha, m_EditorTargetAlpha, t);

                m_Target.ApplyImmediate(
                    Vector3.Scale(m_Target.BaseScale, m_Target.CurrentScaleMultiplier),
                    m_Target.BaseAnchoredPosition + m_Target.CurrentOffset,
                    m_Target.CurrentRotationZ,
                    m_Target.BaseColor * m_Target.CurrentColorTint,
                    m_Target.CurrentAlpha);

                if (progress >= 1f)
                {
                    m_IsEditorTransitioning = false;
                }
            }

            // 2. Motion Clip Simulation (Entrance, Hover, Pressed, Exit)
            if (m_ActiveEditorClip != null && m_Target.AnimationClass != null)
            {
                var clip = m_ActiveEditorClip;
                activeWork = true;
                m_EditorClipElapsed += dt;
                float totalDur = Mathf.Max(0.05f, clip.GetTotalDuration() - clip.startDelay);
                float progress = Mathf.Clamp01(m_EditorClipElapsed / totalDur);

                // Scale (supports independent axis curves for squash & stretch)
                if (clip.scale.enabled)
                {
                    Vector3 fromS = (clip.scale.playMode == MotionPlayMode.Entrance) ? clip.scale.fromScale : clip.scale.toScale;
                    Vector3 toS = (clip.scale.playMode == MotionPlayMode.Entrance) ? clip.scale.toScale : clip.scale.fromScale;
                    if (clip.scale.useIndependentAxisCurves && clip.scale.curveX != null && clip.scale.curveY != null)
                    {
                        float tX = clip.scale.curveX.Evaluate(progress);
                        float tY = clip.scale.curveY.Evaluate(progress);
                        float tZ = clip.scale.timing.EvaluateProgress(progress, clip.easeType, clip.customCurve);
                        m_Target.CurrentScaleMultiplier = new Vector3(
                            Mathf.LerpUnclamped(fromS.x, toS.x, tX),
                            Mathf.LerpUnclamped(fromS.y, toS.y, tY),
                            Mathf.LerpUnclamped(fromS.z, toS.z, tZ)
                        );
                    }
                    else
                    {
                        float tScale = clip.scale.timing.EvaluateProgress(progress, clip.easeType, clip.customCurve);
                        m_Target.CurrentScaleMultiplier = Vector3.LerpUnclamped(fromS, toS, tScale);
                    }
                }

                // Rotate (supports oscillating pendulum and 3D euler)
                if (clip.rotate.enabled)
                {
                    if (clip.rotate.isOscillating)
                    {
                        float decay = Mathf.Pow(1f - progress, clip.rotate.damping > 0 ? clip.rotate.damping : 1.5f);
                        float angleZ = Mathf.Sin(progress * Mathf.PI * 2f * (clip.rotate.oscillationCycles > 0 ? clip.rotate.oscillationCycles : 2.5f)) * decay * clip.rotate.fromEuler.z;
                        m_Target.CurrentRotationEuler = new Vector3(0, 0, angleZ);
                    }
                    else
                    {
                        float tRot = clip.rotate.timing.EvaluateProgress(progress, clip.easeType, clip.customCurve);
                        Vector3 fromR = (clip.rotate.playMode == MotionPlayMode.Entrance) ? clip.rotate.fromEuler : clip.rotate.toEuler;
                        Vector3 toR = (clip.rotate.playMode == MotionPlayMode.Entrance) ? clip.rotate.toEuler : clip.rotate.fromEuler;
                        m_Target.CurrentRotationEuler = Vector3.LerpUnclamped(fromR, toR, tRot);
                    }
                }

                // Slide
                if (clip.slide.enabled)
                {
                    float tSlide = clip.slide.timing.EvaluateProgress(progress, clip.easeType, clip.customCurve);
                    Vector2 slideOffset = clip.slide.CalculateStartOffset(m_Target.TargetTransform);
                    Vector2 fromPos = (clip.slide.playMode == MotionPlayMode.Entrance) ? slideOffset : Vector2.zero;
                    Vector2 toPos = (clip.slide.playMode == MotionPlayMode.Entrance) ? Vector2.zero : slideOffset;
                    m_Target.CurrentOffset = Vector2.LerpUnclamped(fromPos, toPos, tSlide);
                }

                // Fade
                if (clip.fade.enabled)
                {
                    float tFade = clip.fade.timing.EvaluateProgress(progress, clip.easeType, clip.customCurve);
                    float fromA = (clip.fade.playMode == MotionPlayMode.Entrance) ? clip.fade.fromAlpha : clip.fade.toAlpha;
                    float toA = (clip.fade.playMode == MotionPlayMode.Entrance) ? clip.fade.toAlpha : clip.fade.fromAlpha;
                    m_Target.CurrentAlpha = Mathf.LerpUnclamped(fromA, toA, tFade);
                }

                // Color
                if (clip.color.enabled)
                {
                    float tCol = clip.color.timing.EvaluateProgress(progress, clip.easeType, clip.customCurve);
                    Color fromC = (clip.color.playMode == MotionPlayMode.Entrance) ? clip.color.fromColor : clip.color.toColor;
                    Color toC = (clip.color.playMode == MotionPlayMode.Entrance) ? clip.color.toColor : clip.color.fromColor;
                    m_Target.CurrentColorTint = Color.LerpUnclamped(fromC, toC, tCol);
                }

                m_Target.ApplyCompositeTransform();

                if (progress >= 1f)
                {
                    m_ActiveEditorClip = null;
                }
            }

            if (activeWork)
            {
                SceneView.RepaintAll();
            }
            else
            {
                EditorApplication.update -= EditorUpdateTick;
            }
        }
    }
}
#endif
