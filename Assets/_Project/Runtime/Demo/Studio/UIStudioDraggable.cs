using UnityEngine;
using UnityEngine.EventSystems;

namespace UIQualityLife.Runtime.Demo.Studio
{
    /// <summary>
    /// Enables spawned UI components to be freely repositioned across the Canvas Artboard by dragging.
    /// Constrains element movement strictly within the Canvas Artboard bounds.
    /// </summary>
    public class UIStudioDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private RectTransform m_RectTransform;
        private RectTransform m_Container;
        private Canvas m_Canvas;

        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_Container = transform.parent as RectTransform;
            m_Canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!UIDemoStudioHub.IsInspectMode && eventData.button == PointerEventData.InputButton.Left)
            {
                UIStudioSelection.Select(gameObject);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (m_RectTransform == null) m_RectTransform = GetComponent<RectTransform>();
            if (m_Container == null) m_Container = transform.parent as RectTransform;
            if (m_Canvas == null) m_Canvas = GetComponentInParent<Canvas>();

            UIStudioSelection.Select(gameObject);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_RectTransform == null || m_Container == null) return;

            float scale = m_Canvas != null ? m_Canvas.scaleFactor : 1f;
            Vector2 delta = eventData.delta / scale;
            Vector2 newPos = m_RectTransform.anchoredPosition + delta;

            // Keep element constrained within the container panel
            Rect containerRect = m_Container.rect;
            Rect myRect = m_RectTransform.rect;

            float minX = containerRect.xMin + myRect.width * m_RectTransform.pivot.x + 8f;
            float maxX = containerRect.xMax - myRect.width * (1f - m_RectTransform.pivot.x) - 8f;
            float minY = containerRect.yMin + myRect.height * m_RectTransform.pivot.y + 8f;
            float maxY = containerRect.yMax - myRect.height * (1f - m_RectTransform.pivot.y) - 8f;

            if (minX <= maxX) newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            if (minY <= maxY) newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

            m_RectTransform.anchoredPosition = newPos;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }
    }
}
