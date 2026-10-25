using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Runtime component driving CSS-like visual state transitions and atomic multi-channel motion clips.
    /// Supports decoupled per-channel timing (Slide, Scale, 3D Rotation, Fade, Color), pivot delta compensation,
    /// additive punch & shake physics, and continuous loops (Restart, Yoyo).
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [SelectionBase]
    public class UIClassAnimator : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("Animation Profile")]
        [SerializeField] private UIAnimationClass m_AnimationClass;

        [Header("Target Element References")]
        [SerializeField] private RectTransform m_TargetTransform;
        [SerializeField] private Graphic m_TargetGraphic;
        [SerializeField] private CanvasGroup m_TargetCanvasGroup;

        // Baseline resting captures
        private bool m_Initialized = false;
        private Vector3 m_BaseScale = Vector3.one;
        private Vector2 m_BaseAnchoredPos = Vector2.zero;
        private Quaternion m_BaseRotation = Quaternion.identity;
        private Vector2 m_BasePivot = new Vector2(0.5f, 0.5f);
        private Color m_BaseColor = Color.white;
        private float m_BaseAlpha = 1f;

        // Active interaction flags
        private bool m_IsHovered = false;
        private bool m_IsPressed = false;
        private bool m_IsFocused = false;
        private bool m_IsSelected = false;
        private bool m_IsDisabled = false;
        private string m_ActiveCustomState = null;
        private string m_CurrentStateName = "Normal";

        // Runtime coroutines
        private Coroutine m_CurrentTransition;
        private Coroutine m_ActiveLoopRoutine;
        private Coroutine m_ActivePunchRoutine;

        // Current interpolated values (applied on top of base)
        private Vector3 m_CurrentScaleMultiplier = Vector3.one;
        private Vector2 m_CurrentOffset = Vector2.zero;
        private Vector3 m_CurrentRotationEuler = Vector3.zero;
        private Color m_CurrentColorTint = Color.white;
        private float m_CurrentAlpha = 1f;

        // Additive punch/shake offsets
        private Vector3 m_PunchScaleOffset = Vector3.zero;
        private Vector3 m_PunchPosOffset = Vector3.zero;
        private float m_PunchRotZ = 0f;

        #region Public Properties & API
        public UIAnimationClass AnimationClass
        {
            get => m_AnimationClass;
            set
            {
                m_AnimationClass = value;
                ResetToRest();
            }
        }

        public string CurrentStateName => m_CurrentStateName;
        public void SetCurrentStateName(string name) => m_CurrentStateName = name;
        public bool IsHovered => m_IsHovered;
        public bool IsPressed => m_IsPressed;
        public bool IsFocused => m_IsFocused;
        public bool IsSelected => m_IsSelected;
        public bool IsDisabled => m_IsDisabled;

        public Vector3 BaseScale => m_BaseScale;
        public Vector2 BaseAnchoredPosition
        {
            get => m_BaseAnchoredPos;
            set => m_BaseAnchoredPos = value;
        }
        public Quaternion BaseRotation => m_BaseRotation;
        public Vector2 BasePivot => m_BasePivot;
        public Color BaseColor => m_BaseColor;
        public float BaseAlpha => m_BaseAlpha;

        public RectTransform TargetTransform => m_TargetTransform;
        public Graphic TargetGraphic => m_TargetGraphic;
        public CanvasGroup TargetCanvasGroup => m_TargetCanvasGroup;

        public Vector3 CurrentScaleMultiplier { get => m_CurrentScaleMultiplier; set => m_CurrentScaleMultiplier = value; }
        public Vector2 CurrentOffset { get => m_CurrentOffset; set => m_CurrentOffset = value; }
        public Vector3 CurrentRotationEuler { get => m_CurrentRotationEuler; set => m_CurrentRotationEuler = value; }
        public float CurrentRotationZ { get => m_CurrentRotationEuler.z; set => m_CurrentRotationEuler = new Vector3(m_CurrentRotationEuler.x, m_CurrentRotationEuler.y, value); }
        public Color CurrentColorTint { get => m_CurrentColorTint; set => m_CurrentColorTint = value; }
        public float CurrentAlpha { get => m_CurrentAlpha; set => m_CurrentAlpha = value; }
        #endregion

        private void Reset()
        {
            Initialize();
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            UIAnimationClass.OnAnimationClassChanged += HandleClassChanged;

            if (m_AnimationClass != null && m_AnimationClass.EntranceClip != null && m_AnimationClass.EntranceClip.enabled)
            {
                PlayEntranceAnimation();
            }
            else
            {
                ResetToRest();
                StartLoopIfConfigured();
            }
        }

        private void OnDisable()
        {
            UIAnimationClass.OnAnimationClassChanged -= HandleClassChanged;
            StopAllMotionCoroutines();
            RestoreBaseTransformImmediate();
        }

        public void Initialize()
        {
            if (m_TargetTransform == null)
                m_TargetTransform = GetComponent<RectTransform>();

            if (m_TargetGraphic == null)
                m_TargetGraphic = GetComponent<Graphic>();

            if (m_TargetCanvasGroup == null)
                m_TargetCanvasGroup = GetComponent<CanvasGroup>();

            if (!m_Initialized && m_TargetTransform != null)
            {
                m_BaseScale = m_TargetTransform.localScale;
                m_BaseAnchoredPos = m_TargetTransform.anchoredPosition;
                m_BaseRotation = m_TargetTransform.localRotation;
                m_BasePivot = m_TargetTransform.pivot;

                if (m_TargetGraphic != null)
                    m_BaseColor = m_TargetGraphic.color;

                if (m_TargetCanvasGroup != null)
                    m_BaseAlpha = m_TargetCanvasGroup.alpha;

                m_CurrentAlpha = m_BaseAlpha;
                m_Initialized = true;
            }
        }

        private void HandleClassChanged()
        {
            if (isActiveAndEnabled)
            {
                EvaluateTargetState(instant: false);
            }
        }

        #region State Transitions
        public void SetState(string stateName, bool instant = false)
        {
            m_ActiveCustomState = stateName;
            EvaluateTargetState(instant);
        }

        public void ClearCustomState(bool instant = false)
        {
            m_ActiveCustomState = null;
            EvaluateTargetState(instant);
        }

        public void SetSelected(bool selected, bool instant = false)
        {
            m_IsSelected = selected;
            EvaluateTargetState(instant);
        }

        public void SetDisabled(bool disabled, bool instant = false)
        {
            m_IsDisabled = disabled;
            EvaluateTargetState(instant);
        }

        public void EvaluateTargetState(bool instant)
        {
            if (m_AnimationClass == null) return;

            string targetStateName = "Normal";

            if (m_IsDisabled && m_AnimationClass.HasState("Disabled"))
            {
                targetStateName = "Disabled";
            }
            else if (!string.IsNullOrEmpty(m_ActiveCustomState) && m_AnimationClass.HasState(m_ActiveCustomState))
            {
                targetStateName = m_ActiveCustomState;
            }
            else if (m_IsPressed && m_AnimationClass.HasState("Pressed"))
            {
                targetStateName = "Pressed";
            }
            else if (m_IsHovered && m_AnimationClass.HasState("Hover"))
            {
                targetStateName = "Hover";
            }
            else if (m_IsFocused && m_AnimationClass.HasState("Focused"))
            {
                targetStateName = "Focused";
            }
            else if (m_IsSelected && m_AnimationClass.HasState("Selected"))
            {
                targetStateName = "Selected";
            }

            m_CurrentStateName = targetStateName;
            ApplyStateTransition(targetStateName, instant);
        }

        private void ApplyStateTransition(string stateName, bool instant)
        {
            UIVisualState state = m_AnimationClass.GetState(stateName);

            Vector3 targetScale = (state != null && state.overrideScale) ? state.targetScale : Vector3.one;
            Vector2 targetOffset = (state != null && state.overrideOffset) ? state.targetOffset : Vector2.zero;
            Vector3 targetRot = (state != null && state.overrideRotation) ? state.targetRotationEuler : Vector3.zero;
            Color targetTint = (state != null && state.overrideColor) ? state.colorMultiplier : Color.white;
            float targetAlpha = (state != null && state.overrideAlpha) ? state.targetAlpha : m_BaseAlpha;

            float duration = (state != null) ? state.duration : 0.15f;

            if (instant)
            {
                m_CurrentScaleMultiplier = targetScale;
                m_CurrentOffset = targetOffset;
                m_CurrentRotationEuler = targetRot;
                m_CurrentColorTint = targetTint;
                m_CurrentAlpha = targetAlpha;
                ApplyCompositeTransform();
            }
            else
            {
                if (m_CurrentTransition != null) StopCoroutine(m_CurrentTransition);
                m_CurrentTransition = StartCoroutine(VisualStateTransitionRoutine(targetScale, targetOffset, targetRot, targetTint, targetAlpha, duration, state));
            }
        }

        private IEnumerator VisualStateTransitionRoutine(Vector3 targetScale, Vector2 targetOffset, Vector3 targetRot, Color targetTint, float targetAlpha, float duration, UIVisualState state)
        {
            float elapsed = 0f;
            Vector3 startScale = m_CurrentScaleMultiplier;
            Vector2 startOffset = m_CurrentOffset;
            Vector3 startRot = m_CurrentRotationEuler;
            Color startTint = m_CurrentColorTint;
            float startAlpha = m_CurrentAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float t = (state != null) ? state.EvaluateProgress(progress) : progress;

                m_CurrentScaleMultiplier = Vector3.LerpUnclamped(startScale, targetScale, t);
                m_CurrentOffset = Vector2.LerpUnclamped(startOffset, targetOffset, t);
                m_CurrentRotationEuler = Vector3.LerpUnclamped(startRot, targetRot, t);
                m_CurrentColorTint = Color.LerpUnclamped(startTint, targetTint, t);
                m_CurrentAlpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, t);

                ApplyCompositeTransform();
                yield return null;
            }

            m_CurrentScaleMultiplier = targetScale;
            m_CurrentOffset = targetOffset;
            m_CurrentRotationEuler = targetRot;
            m_CurrentColorTint = targetTint;
            m_CurrentAlpha = targetAlpha;

            ApplyCompositeTransform();
            m_CurrentTransition = null;
        }
        #endregion

        #region Atomic Multi-Channel Motion Clips
        public void PlayEntranceAnimation(Action onComplete = null)
        {
            if (m_AnimationClass == null || m_AnimationClass.EntranceClip == null) return;
            PlayMotionClip(m_AnimationClass.EntranceClip, () =>
            {
                onComplete?.Invoke();
                StartLoopIfConfigured();
            });
        }

        public void PlayExitAnimation(Action onComplete = null)
        {
            if (m_AnimationClass == null || m_AnimationClass.ExitClip == null) return;
            StopLoop();
            PlayMotionClip(m_AnimationClass.ExitClip, onComplete);
        }

        public void PlayMotionClip(UIMotionClip clip, Action onComplete = null)
        {
            if (clip == null) return;
            if (m_CurrentTransition != null) StopCoroutine(m_CurrentTransition);
            m_CurrentTransition = StartCoroutine(MotionClipRoutine(clip, onComplete));
        }

        private IEnumerator MotionClipRoutine(UIMotionClip clip, Action onComplete)
        {
            if (clip.startDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(clip.startDelay);
            }

            // Pivot compensation calculation
            Vector2 originalPivot = m_BasePivot;
            if (clip.pivot.overridePivot && m_TargetTransform != null)
            {
                ApplyPivotWithCompensation(clip.pivot.targetPivot);
            }

            int loopsExecuted = 0;
            bool isInfinite = clip.loopCount < 0;

            while (isInfinite || loopsExecuted < Mathf.Max(1, clip.loopCount))
            {
                bool isYoyoReverse = (clip.loopType == MotionLoopType.Yoyo) && (loopsExecuted % 2 == 1);
                float totalDuration = Mathf.Max(0.01f, clip.GetTotalDuration() - clip.startDelay);
                float elapsed = 0f;

                // Cache start parameters for channels
                Vector2 slideOffset = clip.slide.CalculateStartOffset(m_TargetTransform);

                while (elapsed < totalDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / totalDuration);
                    if (isYoyoReverse) progress = 1f - progress;

                    // 1. Slide Channel
                    if (clip.slide.enabled)
                    {
                        float tSlide = EvaluateChannelTime(progress, elapsed, totalDuration, clip.slide.timing, clip.easeType, clip.customCurve);
                        Vector2 fromPos = (clip.slide.playMode == MotionPlayMode.Entrance) ? slideOffset : Vector2.zero;
                        Vector2 toPos = (clip.slide.playMode == MotionPlayMode.Entrance) ? Vector2.zero : slideOffset;
                        m_CurrentOffset = Vector2.LerpUnclamped(fromPos, toPos, tSlide);
                    }

                    // 2. Scale Channel (supports independent axis curves for squash & stretch)
                    if (clip.scale.enabled)
                    {
                        float tScale = EvaluateChannelTime(progress, elapsed, totalDuration, clip.scale.timing, clip.easeType, clip.customCurve);
                        Vector3 fromS = (clip.scale.playMode == MotionPlayMode.Entrance) ? clip.scale.fromScale : clip.scale.toScale;
                        Vector3 toS = (clip.scale.playMode == MotionPlayMode.Entrance) ? clip.scale.toScale : clip.scale.fromScale;

                        if (clip.scale.useIndependentAxisCurves && clip.scale.curveX != null && clip.scale.curveY != null)
                        {
                            float tX = clip.scale.curveX.Evaluate(progress);
                            float tY = clip.scale.curveY.Evaluate(progress);
                            m_CurrentScaleMultiplier = new Vector3(
                                Mathf.LerpUnclamped(fromS.x, toS.x, tX),
                                Mathf.LerpUnclamped(fromS.y, toS.y, tY),
                                Mathf.LerpUnclamped(fromS.z, toS.z, tScale)
                            );
                        }
                        else
                        {
                            m_CurrentScaleMultiplier = Vector3.LerpUnclamped(fromS, toS, tScale);
                        }
                    }

                    // 3. 3D Rotation Channel (supports 3D Euler card flips & pendulum swing)
                    if (clip.rotate.enabled)
                    {
                        float tRot = EvaluateChannelTime(progress, elapsed, totalDuration, clip.rotate.timing, clip.easeType, clip.customCurve);

                        if (clip.rotate.isOscillating)
                        {
                            // Harmonic pendulum decay
                            float cycles = clip.rotate.oscillationCycles > 0f ? clip.rotate.oscillationCycles : 2.5f;
                            float damping = clip.rotate.damping > 0f ? clip.rotate.damping : 1.5f;
                            float decay = Mathf.Pow(1f - progress, damping);
                            float angleZ = Mathf.Sin(progress * Mathf.PI * 2f * cycles) * decay * clip.rotate.fromEuler.z;
                            m_CurrentRotationEuler = new Vector3(0f, 0f, angleZ);
                        }
                        else
                        {
                            Vector3 fromR = (clip.rotate.playMode == MotionPlayMode.Entrance) ? clip.rotate.fromEuler : clip.rotate.toEuler;
                            Vector3 toR = (clip.rotate.playMode == MotionPlayMode.Entrance) ? clip.rotate.toEuler : clip.rotate.fromEuler;
                            m_CurrentRotationEuler = Vector3.LerpUnclamped(fromR, toR, tRot);
                        }
                    }

                    // 4. Fade Channel
                    if (clip.fade.enabled)
                    {
                        float tFade = EvaluateChannelTime(progress, elapsed, totalDuration, clip.fade.timing, clip.easeType, clip.customCurve);
                        float fromA = (clip.fade.playMode == MotionPlayMode.Entrance) ? clip.fade.fromAlpha : clip.fade.toAlpha;
                        float toA = (clip.fade.playMode == MotionPlayMode.Entrance) ? clip.fade.toAlpha : clip.fade.fromAlpha;
                        m_CurrentAlpha = Mathf.LerpUnclamped(fromA, toA, tFade);
                    }

                    // 5. Color Channel
                    if (clip.color.enabled)
                    {
                        float tCol = EvaluateChannelTime(progress, elapsed, totalDuration, clip.color.timing, clip.easeType, clip.customCurve);
                        Color fromC = (clip.color.playMode == MotionPlayMode.Entrance) ? clip.color.fromColor : clip.color.toColor;
                        Color toC = (clip.color.playMode == MotionPlayMode.Entrance) ? clip.color.toColor : clip.color.fromColor;
                        m_CurrentColorTint = Color.LerpUnclamped(fromC, toC, tCol);
                    }

                    ApplyCompositeTransform();
                    yield return null;
                }

                loopsExecuted++;
                if (clip.loopType != MotionLoopType.None && clip.loopDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(clip.loopDelay);
                }
            }

            // Cleanly restore baseline pivot without jumping
            if (clip.pivot.overridePivot && m_TargetTransform != null)
            {
                ApplyPivotWithCompensation(originalPivot);
            }

            m_CurrentTransition = null;
            onComplete?.Invoke();
        }

        private float EvaluateChannelTime(float progress, float elapsed, float globalDuration, ChannelTimingOverride timing, UIEaseType defaultEase, AnimationCurve defaultCurve)
        {
            if (!timing.overrideTiming)
            {
                if (defaultEase == UIEaseType.CustomCurve && defaultCurve != null)
                    return defaultCurve.Evaluate(progress);
                return UIEasing.Evaluate(defaultEase, progress);
            }

            float delay = timing.GetDelay();
            float duration = timing.GetDuration(globalDuration);
            if (elapsed < delay) return 0f;

            float localProg = Mathf.Clamp01((elapsed - delay) / duration);
            return timing.EvaluateProgress(localProg, defaultEase, defaultCurve);
        }

        private void ApplyPivotWithCompensation(Vector2 newPivot)
        {
            if (m_TargetTransform == null) return;
            Vector2 deltaPivot = newPivot - m_TargetTransform.pivot;
            Vector2 size = m_TargetTransform.rect.size;
            Vector2 offset = new Vector2(deltaPivot.x * size.x, deltaPivot.y * size.y);

            m_TargetTransform.pivot = newPivot;
            m_TargetTransform.anchoredPosition += offset;
        }
        #endregion

        #region Procedural Additive Punch & Shake
        public void TriggerClickPunch()
        {
            if (m_AnimationClass == null) return;
            PlayPunchShake(m_AnimationClass.ClickPunch);
        }

        public void PlayPunchShake(UIPunchShakeConfig config)
        {
            if (!config.enabled) return;
            if (m_ActivePunchRoutine != null) StopCoroutine(m_ActivePunchRoutine);
            m_ActivePunchRoutine = StartCoroutine(PunchShakeRoutine(config));
        }

        private IEnumerator PunchShakeRoutine(UIPunchShakeConfig config)
        {
            if (config.delay > 0f)
            {
                yield return new WaitForSecondsRealtime(config.delay);
            }

            float duration = Mathf.Max(0.05f, config.duration);
            float elapsed = 0f;
            int vibrato = Mathf.Max(1, config.vibrato);
            float elasticity = Mathf.Clamp01(config.elasticity);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Damped spring sinusoidal oscillation
                float decay = Mathf.Pow(1f - progress, elasticity * 3f + 1f);
                float sinVal = Mathf.Sin(progress * Mathf.PI * vibrato);

                m_PunchScaleOffset = config.punchScale * (sinVal * decay);
                m_PunchPosOffset = config.punchPosition * (sinVal * decay);
                m_PunchRotZ = config.punchRotation * (sinVal * decay);

                ApplyCompositeTransform();
                yield return null;
            }

            m_PunchScaleOffset = Vector3.zero;
            m_PunchPosOffset = Vector3.zero;
            m_PunchRotZ = 0f;

            ApplyCompositeTransform();
            m_ActivePunchRoutine = null;
        }
        #endregion

        #region Continuous Loops
        private void StartLoopIfConfigured()
        {
            if (m_AnimationClass != null && m_AnimationClass.LoopClip != null && m_AnimationClass.LoopClip.enabled)
            {
                StartLoop(m_AnimationClass.LoopClip);
            }
        }

        public void StartLoop(UIMotionClip loopClip)
        {
            StopLoop();
            if (loopClip == null || !loopClip.enabled) return;
            m_ActiveLoopRoutine = StartCoroutine(MotionClipRoutine(loopClip, null));
        }

        public void StopLoop()
        {
            if (m_ActiveLoopRoutine != null)
            {
                StopCoroutine(m_ActiveLoopRoutine);
                m_ActiveLoopRoutine = null;
            }
        }
        #endregion

        #region Composite Transform Application
        public void ApplyCompositeTransform()
        {
            if (m_TargetTransform != null)
            {
                Vector3 finalScale = Vector3.Scale(m_BaseScale, m_CurrentScaleMultiplier) + m_PunchScaleOffset;
                m_TargetTransform.localScale = finalScale;

                Vector2 finalPos = m_BaseAnchoredPos + m_CurrentOffset + new Vector2(m_PunchPosOffset.x, m_PunchPosOffset.y);
                m_TargetTransform.anchoredPosition = finalPos;

                Vector3 combinedEuler = m_BaseRotation.eulerAngles + m_CurrentRotationEuler + new Vector3(0f, 0f, m_PunchRotZ);
                m_TargetTransform.localRotation = Quaternion.Euler(combinedEuler);
            }

            if (m_TargetGraphic != null)
            {
                m_TargetGraphic.color = m_BaseColor * m_CurrentColorTint;
            }

            if (m_TargetCanvasGroup != null)
            {
                m_TargetCanvasGroup.alpha = Mathf.Clamp01(m_CurrentAlpha);
            }
        }

        public void ResetToRest()
        {
            m_IsHovered = false;
            m_IsPressed = false;
            m_IsFocused = false;
            m_IsSelected = false;
            m_IsDisabled = false;
            m_ActiveCustomState = null;
            m_CurrentStateName = "Normal";

            m_CurrentScaleMultiplier = Vector3.one;
            m_CurrentOffset = Vector2.zero;
            m_CurrentRotationEuler = Vector3.zero;
            m_CurrentColorTint = Color.white;
            m_CurrentAlpha = m_BaseAlpha;

            m_PunchScaleOffset = Vector3.zero;
            m_PunchPosOffset = Vector3.zero;
            m_PunchRotZ = 0f;

            RestoreBaseTransformImmediate();
        }

        public void RestoreBaseTransformImmediate()
        {
            if (m_TargetTransform != null)
            {
                m_TargetTransform.localScale = m_BaseScale;
                m_TargetTransform.anchoredPosition = m_BaseAnchoredPos;
                m_TargetTransform.localRotation = m_BaseRotation;
                m_TargetTransform.pivot = m_BasePivot;
            }
            if (m_TargetGraphic != null)
            {
                m_TargetGraphic.color = m_BaseColor;
            }
            if (m_TargetCanvasGroup != null)
            {
                m_TargetCanvasGroup.alpha = m_BaseAlpha;
            }
        }

        public void ApplyImmediate(Vector3 scale, Vector2 pos, float rotZ, Color col, float alpha)
        {
            if (m_TargetTransform != null)
            {
                m_TargetTransform.localScale = scale;
                m_TargetTransform.anchoredPosition = pos;
                m_TargetTransform.localEulerAngles = new Vector3(m_BaseRotation.eulerAngles.x, m_BaseRotation.eulerAngles.y, m_BaseRotation.eulerAngles.z + rotZ);
            }
            if (m_TargetGraphic != null)
            {
                m_TargetGraphic.color = col;
            }
            if (m_TargetCanvasGroup != null)
            {
                m_TargetCanvasGroup.alpha = alpha;
            }
        }

        private void StopAllMotionCoroutines()
        {
            if (m_CurrentTransition != null) { StopCoroutine(m_CurrentTransition); m_CurrentTransition = null; }
            if (m_ActiveLoopRoutine != null) { StopCoroutine(m_ActiveLoopRoutine); m_ActiveLoopRoutine = null; }
            if (m_ActivePunchRoutine != null) { StopCoroutine(m_ActivePunchRoutine); m_ActivePunchRoutine = null; }
        }
        #endregion

        #region Event Trigger Handlers
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (m_IsDisabled) return;
            m_IsHovered = true;
            if (m_AnimationClass != null && m_AnimationClass.HoverClip != null && m_AnimationClass.HoverClip.enabled)
            {
                PlayMotionClip(m_AnimationClass.HoverClip);
            }
            else
            {
                EvaluateTargetState(instant: false);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_IsHovered = false;
            m_IsPressed = false;
            if (m_CurrentTransition != null)
            {
                StopCoroutine(m_CurrentTransition);
                m_CurrentTransition = null;
            }
            EvaluateTargetState(instant: false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_IsDisabled) return;
            m_IsPressed = true;
            if (m_AnimationClass != null && m_AnimationClass.PressedClip != null && m_AnimationClass.PressedClip.enabled)
            {
                PlayMotionClip(m_AnimationClass.PressedClip);
            }
            else
            {
                EvaluateTargetState(instant: false);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_IsPressed = false;
            if (m_IsHovered)
            {
                if (m_AnimationClass != null && m_AnimationClass.HoverClip != null && m_AnimationClass.HoverClip.enabled)
                {
                    PlayMotionClip(m_AnimationClass.HoverClip);
                }
                else
                {
                    EvaluateTargetState(instant: false);
                }
            }
            else
            {
                EvaluateTargetState(instant: false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_IsDisabled) return;
            EvaluateTargetState(instant: false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (m_IsDisabled) return;
            m_IsFocused = true;
            EvaluateTargetState(instant: false);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            m_IsFocused = false;
            EvaluateTargetState(instant: false);
        }
        #endregion

        #region Context Menu Actions
        [ContextMenu("▶ Play Entrance Motion")]
        public void ContextPlayEntrance() => PlayEntranceAnimation();

        [ContextMenu("▶ Play Hover Motion")]
        public void ContextPlayHover()
        {
            if (m_AnimationClass != null && m_AnimationClass.HoverClip != null)
                PlayMotionClip(m_AnimationClass.HoverClip);
        }

        [ContextMenu("▶ Play Pressed Motion")]
        public void ContextPlayPressed()
        {
            if (m_AnimationClass != null && m_AnimationClass.PressedClip != null)
                PlayMotionClip(m_AnimationClass.PressedClip);
        }

        [ContextMenu("✨ Play Loop Motion")]
        public void ContextPlayLoop() => StartLoopIfConfigured();

        [ContextMenu("🔄 Reset to Rest")]
        public void ContextResetToRest() => ResetToRest();
        #endregion

#if UNITY_EDITOR
        public void EditorPreviewState(string stateName) => UIClassAnimatorMotionBridge.PreviewStateHandler?.Invoke(this, stateName);
        public void EditorPlayEntrance() => UIClassAnimatorMotionBridge.PlayEntranceHandler?.Invoke(this);
        public void EditorPlayHover() => UIClassAnimatorMotionBridge.PlayHoverHandler?.Invoke(this);
        public void EditorPlayPressed() => UIClassAnimatorMotionBridge.PlayPressedHandler?.Invoke(this);
        public void EditorPlayExit() => UIClassAnimatorMotionBridge.PlayExitHandler?.Invoke(this);
        public void EditorReset() => UIClassAnimatorMotionBridge.ResetHandler?.Invoke(this);
#endif
    }
}
