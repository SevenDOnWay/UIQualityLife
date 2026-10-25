using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Runtime.Demo.Studio
{
    /// <summary>
    /// Middle Panel: Interactive Canvas Artboard & Selection Gizmo.
    /// Uses raycasting to directly select any clicked GameObject and reflect its components in the left inspector.
    /// In Inspect Mode, pointer events are caught by an overlay to prevent accidental button clicks while inspecting.
    /// </summary>
    public class UIDemoCanvasViewport : MonoBehaviour, IPointerClickHandler
    {
        [Header("Containers")]
        [SerializeField] private RectTransform m_ArtboardContainer;
        [SerializeField] private RectTransform m_SelectionGizmo;
        [SerializeField] private RectTransform m_RaycastOverlay;

        [Header("Catalog Reference")]
        [SerializeField] private UIPresetCatalog m_PresetCatalog;

        private void Awake()
        {
            if (m_PresetCatalog == null)
            {
                m_PresetCatalog = Resources.Load<UIPresetCatalog>("Demo/UIPresetCatalog");
            }
        }

        private void Start()
        {
            UIStudioSelection.OnSelectionChanged += UpdateGizmoPosition;
            ResetDefaultLayout();
        }

        private void OnDestroy()
        {
            UIStudioSelection.OnSelectionChanged -= UpdateGizmoPosition;
        }

        private void LateUpdate()
        {
            // Keep gizmo aligned if selected element changes size or position
            if (UIStudioSelection.Current != null && m_SelectionGizmo != null && m_SelectionGizmo.gameObject.activeSelf)
            {
                SyncGizmoToTarget(UIStudioSelection.Current);
            }
        }

        public void SetInspectOverlayActive(bool active)
        {
            if (m_RaycastOverlay != null)
            {
                m_RaycastOverlay.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Raycasts at the click position, finds the topmost GameObject inside the artboard,
        /// and selects it directly so its components are read by the Runtime Inspector.
        /// </summary>
        public void PerformRaycastSelection(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject == null) continue;
                if (r.gameObject == gameObject || (m_SelectionGizmo != null && r.gameObject == m_SelectionGizmo.gameObject)) continue;
                if (m_RaycastOverlay != null && r.gameObject == m_RaycastOverlay.gameObject) continue;

                // Check if hit object is inside artboard container
                if (m_ArtboardContainer != null && r.gameObject.transform.IsChildOf(m_ArtboardContainer))
                {
                    // Exact game object hit by raycast
                    UIStudioSelection.Select(r.gameObject);
                    eventData.Use();
                    return;
                }
            }

            // Clicked empty artboard space -> clear selection
            UIStudioSelection.Clear();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // In Interact mode, right-click can also trigger instant raycast inspection
            if (eventData.button == PointerEventData.InputButton.Right || UIDemoStudioHub.IsInspectMode)
            {
                PerformRaycastSelection(eventData);
            }
        }

        private int m_SpawnCounter = 0;

        private Vector2 GetNextSpawnPosition()
        {
            int col = m_SpawnCounter % 3;
            int row = (m_SpawnCounter / 3) % 3;
            m_SpawnCounter++;
            float x = (col - 1) * 180f;
            float y = (1 - row) * 110f;
            return new Vector2(x, y);
        }

        public void SpawnPreset(GameObject prefab)
        {
            if (prefab == null || m_ArtboardContainer == null) return;

            var instance = Instantiate(prefab, m_ArtboardContainer);
            instance.name = prefab.name;

            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 currentSize = rt.rect.size;
                if (currentSize.x <= 10) currentSize.x = 200;
                if (currentSize.y <= 10) currentSize.y = 80;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = currentSize;
                rt.anchoredPosition = GetNextSpawnPosition();

                var animators = instance.GetComponentsInChildren<UIQualityLife.Styling.UIClassAnimator>(true);
                foreach (var anim in animators)
                {
                    if (anim != null && (anim.TargetTransform == rt || anim.transform == instance.transform))
                    {
                        anim.BaseAnchoredPosition = rt.anchoredPosition;
                    }
                }
            }

            if (instance.GetComponent<UIStudioDraggable>() == null)
            {
                instance.AddComponent<UIStudioDraggable>();
            }

            // Auto-select newly spawned instance
            UIStudioSelection.Select(instance);
        }

        public void ClearCanvas()
        {
            UIStudioSelection.Clear();
            m_SpawnCounter = 0;
            if (m_ArtboardContainer == null) return;

            for (int i = m_ArtboardContainer.childCount - 1; i >= 0; i--)
            {
                var child = m_ArtboardContainer.GetChild(i);
                if (child != m_SelectionGizmo && child != m_RaycastOverlay)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void ResetDefaultLayout()
        {
            ClearCanvas();
            m_SpawnCounter = 0;
            if (m_PresetCatalog == null || m_PresetCatalog.Items.Count == 0) return;

            var defaults = new (string name, Vector2 pos)[]
            {
                ("ButtonPrimary", new Vector2(-220f, 100f)),
                ("SliderWithInput", new Vector2(160f, 100f)),
                ("ButtonSuccess", new Vector2(-220f, -60f)),
                ("HUDHealthBar", new Vector2(160f, -60f))
            };

            foreach (var def in defaults)
            {
                foreach (var item in m_PresetCatalog.Items)
                {
                    if (item != null && item.Name == def.name && item.Prefab != null)
                    {
                        var go = Instantiate(item.Prefab, m_ArtboardContainer);
                        go.name = item.Name;
                        var rt = go.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            Vector2 currentSize = rt.rect.size;
                            if (currentSize.x <= 10) currentSize.x = 200;
                            if (currentSize.y <= 10) currentSize.y = 80;

                            rt.anchorMin = new Vector2(0.5f, 0.5f);
                            rt.anchorMax = new Vector2(0.5f, 0.5f);
                            rt.pivot = new Vector2(0.5f, 0.5f);
                            rt.sizeDelta = currentSize;
                            rt.anchoredPosition = def.pos;

                            var animators = go.GetComponentsInChildren<UIQualityLife.Styling.UIClassAnimator>(true);
                            foreach (var anim in animators)
                            {
                                if (anim != null && (anim.TargetTransform == rt || anim.transform == go.transform))
                                {
                                    anim.BaseAnchoredPosition = rt.anchoredPosition;
                                }
                            }
                        }
                        if (go.GetComponent<UIStudioDraggable>() == null)
                        {
                            go.AddComponent<UIStudioDraggable>();
                        }
                        break;
                    }
                }
            }
        }

        private void UpdateGizmoPosition(GameObject selected)
        {
            if (m_SelectionGizmo == null) return;

            if (selected == null || !selected.activeInHierarchy)
            {
                m_SelectionGizmo.gameObject.SetActive(false);
                return;
            }

            m_SelectionGizmo.gameObject.SetActive(true);
            SyncGizmoToTarget(selected);
        }

        private void SyncGizmoToTarget(GameObject target)
        {
            if (target == null || m_SelectionGizmo == null) return;

            var targetRt = target.GetComponent<RectTransform>();
            if (targetRt == null) return;

            m_SelectionGizmo.position = targetRt.position;
            m_SelectionGizmo.rotation = targetRt.rotation;
            m_SelectionGizmo.sizeDelta = targetRt.rect.size + new Vector2(8, 8);
        }
    }

    /// <summary>
    /// Attached to the inspect overlay to catch pointer clicks and forward to UIDemoCanvasViewport.
    /// </summary>
    public class UIRaycastInterceptor : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UIDemoCanvasViewport m_Viewport;

        public void SetViewport(UIDemoCanvasViewport vp)
        {
            m_Viewport = vp;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_Viewport != null)
            {
                m_Viewport.PerformRaycastSelection(eventData);
            }
        }
    }
}
