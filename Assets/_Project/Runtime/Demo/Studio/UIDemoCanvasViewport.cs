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
    /// Hosts live components, allows dual-mode interaction (Interact vs Inspect),
    /// and renders an active selection outline around the inspected element.
    /// </summary>
    public class UIDemoCanvasViewport : MonoBehaviour, IPointerClickHandler
    {
        [Header("Containers")]
        [SerializeField] private RectTransform m_ArtboardContainer;
        [SerializeField] private RectTransform m_SelectionGizmo;

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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!UIDemoStudioHub.IsInspectMode) return;

            // Find clicked object in artboard
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject == gameObject || r.gameObject == m_SelectionGizmo.gameObject) continue;

                // Check if inside artboard
                if (r.gameObject.transform.IsChildOf(m_ArtboardContainer))
                {
                    // Select root component inside artboard or immediate selectable
                    var target = r.gameObject;
                    while (target.transform.parent != null && target.transform.parent != m_ArtboardContainer)
                    {
                        if (target.GetComponent<UIStudioSelectable>() != null || target.GetComponent<Button>() != null)
                            break;
                        target = target.transform.parent.gameObject;
                    }

                    UIStudioSelection.Select(target);
                    eventData.Use();
                    return;
                }
            }

            // Clicked empty artboard space -> clear selection
            UIStudioSelection.Clear();
        }

        public void SpawnPreset(GameObject prefab)
        {
            if (prefab == null || m_ArtboardContainer == null) return;

            var instance = Instantiate(prefab, m_ArtboardContainer);
            instance.name = prefab.name;

            // Attach selectable helper if missing
            if (instance.GetComponent<UIStudioSelectable>() == null)
            {
                instance.AddComponent<UIStudioSelectable>();
            }

            // Auto-select newly spawned instance
            UIStudioSelection.Select(instance);
        }

        public void ClearCanvas()
        {
            UIStudioSelection.Clear();
            if (m_ArtboardContainer == null) return;

            for (int i = m_ArtboardContainer.childCount - 1; i >= 0; i--)
            {
                var child = m_ArtboardContainer.GetChild(i);
                if (child != m_SelectionGizmo)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void ResetDefaultLayout()
        {
            ClearCanvas();
            if (m_PresetCatalog == null || m_PresetCatalog.Items.Count == 0) return;

            // Spawn a balanced demo layout: Button, Slider/Input, Card, HUD Bar
            var sampleNames = new[] { "ButtonPrimary", "SliderWithInput", "QuestionnaireMultiple", "HUDHealthBar" };

            foreach (var sName in sampleNames)
            {
                foreach (var item in m_PresetCatalog.Items)
                {
                    if (item != null && item.Name == sName && item.Prefab != null)
                    {
                        var go = Instantiate(item.Prefab, m_ArtboardContainer);
                        go.name = item.Name;
                        if (go.GetComponent<UIStudioSelectable>() == null)
                            go.AddComponent<UIStudioSelectable>();
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
}
