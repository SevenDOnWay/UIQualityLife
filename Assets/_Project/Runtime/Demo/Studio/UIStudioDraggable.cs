using UnityEngine;
using UnityEngine.EventSystems;

namespace UIQualityLife.Runtime.Demo.Studio
{
    /// <summary>
    /// Enables spawned UI components to be freely repositioned across the Canvas Artboard by dragging.
    /// Constrains element movement strictly within the Canvas Artboard bounds without layout snapping.
    /// </summary>
    public class UIStudioDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private RectTransform m_TargetTransform;
        private RectTransform m_Container;
        private Canvas m_Canvas;

        private void Awake()
        {
            ResolveTargetAndContainer();
        }

        private void ResolveTargetAndContainer()
        {
            // Find root under ArtboardContainer
            Transform curr = transform;
            while (curr.parent != null && curr.parent.name != "ArtboardContainer")
            {
                curr = curr.parent;
            }
            m_TargetTransform = curr as RectTransform;
            m_Container = curr.parent as RectTransform;
            m_Canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!UIDemoStudioHub.IsInspectMode && eventData.button == PointerEventData.InputButton.Left)
            {
                if (m_TargetTransform == null) ResolveTargetAndContainer();
                UIStudioSelection.Select(m_TargetTransform != null ? m_TargetTransform.gameObject : gameObject);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ResolveTargetAndContainer();
            if (m_TargetTransform != null)
            {
                UIStudioSelection.Select(m_TargetTransform.gameObject);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_TargetTransform == null || m_Container == null) return;

            float scale = m_Canvas != null ? m_Canvas.scaleFactor : 1f;
            Vector2 delta = eventData.delta / scale;
            m_TargetTransform.anchoredPosition += delta;

            // Clamping within container panel
            Rect containerRect = m_Container.rect;
            float halfW = m_TargetTransform.rect.width * 0.5f;
            float halfH = m_TargetTransform.rect.height * 0.5f;

            float limitX = Mathf.Max(0f, (containerRect.width * 0.5f) - halfW);
            float limitY = Mathf.Max(0f, (containerRect.height * 0.5f) - halfH);

            Vector2 pos = m_TargetTransform.anchoredPosition;
            if (limitX > 0) pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
            if (limitY > 0) pos.y = Mathf.Clamp(pos.y, -limitY, limitY);
            m_TargetTransform.anchoredPosition = pos;

            // Sync with all UIClassAnimator instances targeting this transform so animators don't snap back to spawn origin
            SyncAnimatorsBasePosition(pos);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_TargetTransform != null)
            {
                SyncAnimatorsBasePosition(m_TargetTransform.anchoredPosition);
            }
        }

        public void SyncAnimatorsBasePosition(Vector2 pos)
        {
            if (m_TargetTransform == null) return;
            var animators = m_TargetTransform.GetComponentsInChildren<UIQualityLife.Styling.UIClassAnimator>(true);
            foreach (var anim in animators)
            {
                if (anim != null && (anim.TargetTransform == m_TargetTransform || anim.transform == m_TargetTransform))
                {
                    anim.BaseAnchoredPosition = pos;
                }
            }
        }
    }
}
