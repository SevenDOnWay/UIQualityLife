using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Generalized CSS-like UI State Animator.
    /// Drives interactive state transitions (:hover, :active, :focus, :disabled, :checked, or any custom state)
    /// defined in an extensible UIAnimationClass profile.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI Quality of Life/Styling/UI Class Animator")]
    public class UIClassAnimator : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler,
        ISelectHandler, IDeselectHandler,
        ISubmitHandler
    {
        [Tooltip("The shared CSS Animation Class applied to this element.")]
        [SerializeField] private UIAnimationClass m_AnimationClass;

        [Header("Target Overrides")]
        [Tooltip("Optional target transform to animate. Defaults to this RectTransform.")]
        [SerializeField] private RectTransform m_TargetTransform;

        [Tooltip("Optional target graphic for color tinting.")]
        [SerializeField] private Graphic m_TargetGraphic;

        [Tooltip("Optional canvas group for alpha transitions.")]
        [SerializeField] private CanvasGroup m_TargetCanvasGroup;

        // Base resting values
        private Vector3 m_BaseScale = Vector3.one;
        private Vector2 m_BaseAnchoredPos = Vector2.zero;
        private Quaternion m_BaseRotation = Quaternion.identity;
        private Color m_BaseColor = Color.white;
        private float m_BaseAlpha = 1f;
        private bool m_Initialized = false;

        // State Machine Flags
        private bool m_IsHovered = false;
        private bool m_IsPressed = false;
        private bool m_IsFocused = false;
        private bool m_IsSelected = false;
        private bool m_IsDisabled = false;
        private string m_ActiveCustomState = null;
        private string m_CurrentStateName = "Normal";

        // Runtime timers & tween
        private float m_ClickBounceTimer = 0f;
        private float m_LoopTimer = 0f;
        private Coroutine m_CurrentTransition;

        // Current interpolated values
        private Vector3 m_CurrentScaleMultiplier = Vector3.one;
        private Vector2 m_CurrentOffset = Vector2.zero;
        private float m_CurrentRotationZ = 0f;
        private Color m_CurrentColorTint = Color.white;
        private float m_CurrentAlpha = 1f;

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
        public bool IsHovered => m_IsHovered;
        public bool IsPressed => m_IsPressed;
        public bool IsFocused => m_IsFocused;
        public bool IsSelected => m_IsSelected;
        public bool IsDisabled => m_IsDisabled;

        /// <summary>
        /// Manually transitions to any CSS state (e.g. 'Normal', 'Hover', 'Pressed', 'Cooldown', 'Warning').
        /// </summary>
        public void SetState(string stateName, bool instant = false)
        {
            m_ActiveCustomState = stateName;
            EvaluateTargetState(instant);
        }

        public void ClearCustomState()
        {
            m_ActiveCustomState = null;
            EvaluateTargetState(instant: false);
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
        #endregion

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            UIAnimationClass.OnAnimationClassChanged += HandleClassChanged;

            if (m_AnimationClass != null && m_AnimationClass.PlayOnEnable)
            {
                PlayEnterAnimation();
            }
            else
            {
                ResetToRest();
            }
        }

        private void OnDisable()
        {
            UIAnimationClass.OnAnimationClassChanged -= HandleClassChanged;
            if (m_CurrentTransition != null)
            {
                StopCoroutine(m_CurrentTransition);
                m_CurrentTransition = null;
            }
            ApplyImmediate(m_BaseScale, m_BaseAnchoredPos, 0f, m_BaseColor, m_BaseAlpha);
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

        private void Update()
        {
            if (m_AnimationClass == null) return;

            // Handle continuous CSS keyframe loops (@keyframes)
            if (m_AnimationClass.LoopType != UILoopAnimationType.None && !m_IsPressed && !m_IsDisabled)
            {
                m_LoopTimer += Time.unscaledDeltaTime * m_AnimationClass.LoopSpeed * Mathf.PI * 2f;
                float sinValue = Mathf.Sin(m_LoopTimer);

                switch (m_AnimationClass.LoopType)
                {
                    case UILoopAnimationType.Pulse:
                    {
                        float loopScale = 1f + (sinValue * m_AnimationClass.LoopMagnitude);
                        if (m_TargetTransform != null)
                        {
                            m_TargetTransform.localScale = Vector3.Scale(m_BaseScale, m_CurrentScaleMultiplier * loopScale);
                        }
                        break;
                    }
                    case UILoopAnimationType.Float:
                    {
                        float yOffset = sinValue * (m_AnimationClass.LoopMagnitude * 100f);
                        if (m_TargetTransform != null)
                        {
                            m_TargetTransform.anchoredPosition = m_BaseAnchoredPos + m_CurrentOffset + new Vector2(0f, yOffset);
                        }
                        break;
                    }
                    case UILoopAnimationType.Shimmer:
                    {
                        if (m_TargetCanvasGroup != null)
                        {
                            m_TargetCanvasGroup.alpha = Mathf.Clamp01(m_CurrentAlpha + sinValue * m_AnimationClass.LoopMagnitude);
                        }
                        break;
                    }
                    case UILoopAnimationType.Shake:
                    {
                        float xOffset = Mathf.Sin(m_LoopTimer * 3f) * (m_AnimationClass.LoopMagnitude * 50f);
                        if (m_TargetTransform != null)
                        {
                            m_TargetTransform.anchoredPosition = m_BaseAnchoredPos + m_CurrentOffset + new Vector2(xOffset, 0f);
                        }
                        break;
                    }
                }
            }
        }

        #region Event Trigger Handlers
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (m_IsDisabled) return;
            m_IsHovered = true;
            EvaluateTargetState(instant: false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_IsHovered = false;
            m_IsPressed = false;
            EvaluateTargetState(instant: false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_IsDisabled) return;
            m_IsPressed = true;
            EvaluateTargetState(instant: false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_IsPressed = false;
            EvaluateTargetState(instant: false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_IsDisabled) return;
            if (m_AnimationClass != null && m_AnimationClass.EnableClickBounce)
            {
                TriggerClickBounce();
            }
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

        public void OnSubmit(BaseEventData eventData)
        {
            if (m_IsDisabled) return;
            if (m_AnimationClass != null && m_AnimationClass.EnableClickBounce)
            {
                TriggerClickBounce();
            }
        }
        #endregion

        /// <summary>
        /// Evaluates active state with priority hierarchy:
        /// Disabled > CustomState > Pressed > Hover > Focused > Selected > Normal
        /// </summary>
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
            float targetRotation = (state != null && state.overrideRotation) ? state.targetRotation : 0f;
            Color targetTint = (state != null && state.overrideColor) ? state.colorMultiplier : Color.white;
            float targetAlpha = (state != null && state.overrideAlpha) ? state.targetAlpha : m_BaseAlpha;

            float duration = (state != null) ? state.duration : 0.15f;
            AnimationCurve curve = (state != null) ? state.curve : null;

            if (instant)
            {
                m_CurrentScaleMultiplier = targetScale;
                m_CurrentOffset = targetOffset;
                m_CurrentRotationZ = targetRotation;
                m_CurrentColorTint = targetTint;
                m_CurrentAlpha = targetAlpha;

                ApplyImmediate(Vector3.Scale(m_BaseScale, m_CurrentScaleMultiplier),
                               m_BaseAnchoredPos + m_CurrentOffset,
                               m_CurrentRotationZ,
                               m_BaseColor * m_CurrentColorTint,
                               m_CurrentAlpha);
            }
            else
            {
                if (m_CurrentTransition != null) StopCoroutine(m_CurrentTransition);
                m_CurrentTransition = StartCoroutine(TransitionRoutine(targetScale, targetOffset, targetRotation, targetTint, targetAlpha, duration, state));
            }
        }

        private IEnumerator TransitionRoutine(Vector3 targetScale, Vector2 targetOffset, float targetRot, Color targetTint, float targetAlpha, float duration, UIVisualState state)
        {
            float elapsed = 0f;
            Vector3 startScale = m_CurrentScaleMultiplier;
            Vector2 startOffset = m_CurrentOffset;
            float startRot = m_CurrentRotationZ;
            Color startTint = m_CurrentColorTint;
            float startAlpha = m_CurrentAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float t = (state != null) ? state.EvaluateProgress(progress) : progress;

                m_CurrentScaleMultiplier = Vector3.LerpUnclamped(startScale, targetScale, t);
                m_CurrentOffset = Vector2.LerpUnclamped(startOffset, targetOffset, t);
                m_CurrentRotationZ = Mathf.LerpUnclamped(startRot, targetRot, t);
                m_CurrentColorTint = Color.LerpUnclamped(startTint, targetTint, t);
                m_CurrentAlpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, t);

                ApplyImmediate(Vector3.Scale(m_BaseScale, m_CurrentScaleMultiplier),
                               m_BaseAnchoredPos + m_CurrentOffset,
                               m_CurrentRotationZ,
                               m_BaseColor * m_CurrentColorTint,
                               m_CurrentAlpha);

                yield return null;
            }

            m_CurrentScaleMultiplier = targetScale;
            m_CurrentOffset = targetOffset;
            m_CurrentRotationZ = targetRot;
            m_CurrentColorTint = targetTint;
            m_CurrentAlpha = targetAlpha;

            ApplyImmediate(Vector3.Scale(m_BaseScale, m_CurrentScaleMultiplier),
                           m_BaseAnchoredPos + m_CurrentOffset,
                           m_CurrentRotationZ,
                           m_BaseColor * m_CurrentColorTint,
                           m_CurrentAlpha);

            m_CurrentTransition = null;
        }

        public void TriggerClickBounce()
        {
            if (m_CurrentTransition != null) StopCoroutine(m_CurrentTransition);
            m_CurrentTransition = StartCoroutine(BounceRoutine());
        }

        private IEnumerator BounceRoutine()
        {
            float duration = m_AnimationClass.BounceDuration;
            float elapsed = 0f;
            float mag = m_AnimationClass.BounceMagnitude;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float bounce = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t) * mag;
                float scale = m_CurrentScaleMultiplier.x + bounce;

                if (m_TargetTransform != null)
                {
                    m_TargetTransform.localScale = m_BaseScale * scale;
                }
                yield return null;
            }

            EvaluateTargetState(instant: false);
        }

        private void PlayEnterAnimation()
        {
            if (m_CurrentTransition != null) StopCoroutine(m_CurrentTransition);
            m_CurrentTransition = StartCoroutine(EnterRoutine());
        }

        private IEnumerator EnterRoutine()
        {
            float duration = m_AnimationClass.EnterDuration;
            float elapsed = 0f;
            float startScale = m_AnimationClass.EnterScaleFrom;
            float startFade = m_AnimationClass.EnterFadeFrom;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float t = m_AnimationClass.EvaluateEnterProgress(progress);

                float currentScale = Mathf.LerpUnclamped(startScale, 1f, t);
                if (m_TargetTransform != null)
                {
                    m_TargetTransform.localScale = m_BaseScale * currentScale;
                }

                if (m_TargetCanvasGroup != null)
                {
                    m_TargetCanvasGroup.alpha = Mathf.LerpUnclamped(startFade, m_BaseAlpha, t);
                }

                yield return null;
            }

            ResetToRest();
            m_CurrentTransition = null;
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
            m_CurrentRotationZ = 0f;
            m_CurrentColorTint = Color.white;
            m_CurrentAlpha = m_BaseAlpha;

            ApplyImmediate(m_BaseScale, m_BaseAnchoredPos, 0f, m_BaseColor, m_BaseAlpha);
        }

        private void ApplyImmediate(Vector3 scale, Vector2 pos, float rotZ, Color col, float alpha)
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

#if UNITY_EDITOR
        private double m_LastEditorTime;
        private bool m_EditorLoopActive = false;
        private float m_EditorBounceElapsed = 0f;
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

        public bool IsEditorLoopActive => m_EditorLoopActive;

        public void EditorPreviewState(string stateName)
        {
            Initialize();
            m_CurrentStateName = stateName;

            UIVisualState state = (m_AnimationClass != null) ? m_AnimationClass.GetState(stateName) : null;
            m_EditorTargetScale = (state != null && state.overrideScale) ? state.targetScale : Vector3.one;
            m_EditorTargetOffset = (state != null && state.overrideOffset) ? state.targetOffset : Vector2.zero;
            m_EditorTargetRot = (state != null && state.overrideRotation) ? state.targetRotation : 0f;
            m_EditorTargetColor = (state != null && state.overrideColor) ? state.colorMultiplier : Color.white;
            m_EditorTargetAlpha = (state != null && state.overrideAlpha) ? state.targetAlpha : m_BaseAlpha;

            m_EditorStartScale = m_CurrentScaleMultiplier;
            m_EditorStartOffset = m_CurrentOffset;
            m_EditorStartRot = m_CurrentRotationZ;
            m_EditorStartColor = m_CurrentColorTint;
            m_EditorStartAlpha = m_CurrentAlpha;

            m_EditorTransitionDuration = (state != null) ? state.duration : 0.15f;
            m_EditorEaseType = (state != null) ? state.easeType : UIEaseType.OutQuad;
            m_EditorTransitionCurve = (state != null) ? state.curve : null;
            m_EditorTransitionElapsed = 0f;
            m_IsEditorTransitioning = true;

            EnsureEditorUpdateHook();
        }

        public void EditorTriggerClickBounce()
        {
            Initialize();
            if (m_AnimationClass == null) return;
            m_IsEditorBouncing = true;
            m_EditorBounceElapsed = 0f;
            EnsureEditorUpdateHook();
        }

        public void EditorToggleLoop()
        {
            Initialize();
            m_EditorLoopActive = !m_EditorLoopActive;
            if (m_EditorLoopActive)
            {
                m_LoopTimer = 0f;
                EnsureEditorUpdateHook();
            }
            else
            {
                EditorReset();
            }
        }

        public void EditorReset()
        {
            m_EditorLoopActive = false;
            m_IsEditorBouncing = false;
            m_IsEditorTransitioning = false;
            UnityEditor.EditorApplication.update -= EditorUpdateTick;
            ResetToRest();
            if (m_TargetTransform != null) UnityEditor.EditorUtility.SetDirty(m_TargetTransform);
            UnityEditor.SceneView.RepaintAll();
        }

        private void EnsureEditorUpdateHook()
        {
            m_LastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
            UnityEditor.EditorApplication.update -= EditorUpdateTick;
            UnityEditor.EditorApplication.update += EditorUpdateTick;
        }

        private void EditorUpdateTick()
        {
            if (this == null || !gameObject || Application.isPlaying)
            {
                UnityEditor.EditorApplication.update -= EditorUpdateTick;
                return;
            }

            double currentTime = UnityEditor.EditorApplication.timeSinceStartup;
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

                m_CurrentScaleMultiplier = Vector3.LerpUnclamped(m_EditorStartScale, m_EditorTargetScale, t);
                m_CurrentOffset = Vector2.LerpUnclamped(m_EditorStartOffset, m_EditorTargetOffset, t);
                m_CurrentRotationZ = Mathf.LerpUnclamped(m_EditorStartRot, m_EditorTargetRot, t);
                m_CurrentColorTint = Color.LerpUnclamped(m_EditorStartColor, m_EditorTargetColor, t);
                m_CurrentAlpha = Mathf.LerpUnclamped(m_EditorStartAlpha, m_EditorTargetAlpha, t);

                ApplyImmediate(Vector3.Scale(m_BaseScale, m_CurrentScaleMultiplier),
                               m_BaseAnchoredPos + m_CurrentOffset,
                               m_CurrentRotationZ,
                               m_BaseColor * m_CurrentColorTint,
                               m_CurrentAlpha);

                if (progress >= 1f)
                {
                    m_IsEditorTransitioning = false;
                }
            }

            // 2. Click Bounce
            if (m_IsEditorBouncing && m_AnimationClass != null)
            {
                activeWork = true;
                m_EditorBounceElapsed += dt;
                float duration = m_AnimationClass.BounceDuration;
                float t = Mathf.Clamp01(m_EditorBounceElapsed / duration);
                float bounce = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t) * m_AnimationClass.BounceMagnitude;
                float scale = m_CurrentScaleMultiplier.x + bounce;

                if (m_TargetTransform != null)
                {
                    m_TargetTransform.localScale = m_BaseScale * scale;
                }

                if (t >= 1f)
                {
                    m_IsEditorBouncing = false;
                }
            }

            // 3. Keyframe Loop
            if (m_EditorLoopActive && m_AnimationClass != null && m_AnimationClass.LoopType != UILoopAnimationType.None)
            {
                activeWork = true;
                m_LoopTimer += dt * m_AnimationClass.LoopSpeed * Mathf.PI * 2f;
                float sinValue = Mathf.Sin(m_LoopTimer);

                switch (m_AnimationClass.LoopType)
                {
                    case UILoopAnimationType.Pulse:
                    {
                        float loopScale = 1f + (sinValue * m_AnimationClass.LoopMagnitude);
                        if (m_TargetTransform != null)
                        {
                            m_TargetTransform.localScale = Vector3.Scale(m_BaseScale, m_CurrentScaleMultiplier * loopScale);
                        }
                        break;
                    }
                    case UILoopAnimationType.Float:
                    {
                        float yOffset = sinValue * (m_AnimationClass.LoopMagnitude * 100f);
                        if (m_TargetTransform != null)
                        {
                            m_TargetTransform.anchoredPosition = m_BaseAnchoredPos + m_CurrentOffset + new Vector2(0f, yOffset);
                        }
                        break;
                    }
                    case UILoopAnimationType.Shimmer:
                    {
                        if (m_TargetCanvasGroup != null)
                        {
                            m_TargetCanvasGroup.alpha = Mathf.Clamp01(m_CurrentAlpha + sinValue * m_AnimationClass.LoopMagnitude);
                        }
                        break;
                    }
                    case UILoopAnimationType.Shake:
                    {
                        float xOffset = Mathf.Sin(m_LoopTimer * 3f) * (m_AnimationClass.LoopMagnitude * 50f);
                        if (m_TargetTransform != null)
                        {
                            m_TargetTransform.anchoredPosition = m_BaseAnchoredPos + m_CurrentOffset + new Vector2(xOffset, 0f);
                        }
                        break;
                    }
                }
            }

            if (activeWork)
            {
                UnityEditor.SceneView.RepaintAll();
            }
            else
            {
                UnityEditor.EditorApplication.update -= EditorUpdateTick;
            }
        }
#endif
    }
}
