using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIQualityLife.Runtime.Demo
{
    /// <summary>
    /// Displays presets in the Demo Scene with live filtering by category and search query.
    /// Supports dynamic discovery of all Button, Badge, Card, HUD, Input, and Modal presets.
    /// </summary>
    public class UIDemoPresetGallery : MonoBehaviour
    {
        [Header("Catalog Reference")]
        [SerializeField] private UIPresetCatalog m_PresetCatalog;

        [Header("Containers")]
        [SerializeField] private Transform m_CardContainer;
        [SerializeField] private Transform m_FilterContainer;

        [Header("Search Filter")]
        [SerializeField] private TMP_InputField m_SearchInputField;

        [Header("Card Template")]
        [SerializeField] private GameObject m_PresetCardPrefab;
        [SerializeField] private GameObject m_FilterButtonPrefab;

        private string m_ActiveCategory = "All";
        private string m_SearchQuery = "";

        private void Awake()
        {
            if (m_PresetCatalog == null)
            {
                m_PresetCatalog = Resources.Load<UIPresetCatalog>("Demo/UIPresetCatalog");
            }
        }

        private void Start()
        {
            if (m_SearchInputField != null)
            {
                m_SearchInputField.onValueChanged.AddListener(OnSearchChanged);
            }

            SetupFilters();
            RenderPresets();
        }

        private void OnSearchChanged(string query)
        {
            m_SearchQuery = query != null ? query.Trim().ToLowerInvariant() : "";
            RenderPresets();
        }

        private void SetupFilters()
        {
            if (m_FilterContainer == null || m_PresetCatalog == null) return;

            var categories = m_PresetCatalog.GetCategories();
            categories.Insert(0, "All");

            foreach (var category in categories)
            {
                if (m_FilterButtonPrefab != null)
                {
                    var btnObj = Instantiate(m_FilterButtonPrefab, m_FilterContainer);
                    var btnText = btnObj.GetComponentInChildren<TMP_Text>();
                    if (btnText != null) btnText.text = category;

                    var btn = btnObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        string cat = category;
                        btn.onClick.AddListener(() => SetCategory(cat));
                    }
                }
            }
        }

        public void SetCategory(string category)
        {
            m_ActiveCategory = category;
            RenderPresets();
        }

        private void RenderPresets()
        {
            if (m_CardContainer == null || m_PresetCatalog == null) return;

            // Clear existing spawned cards
            for (int i = m_CardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_CardContainer.GetChild(i).gameObject);
            }

            var items = m_PresetCatalog.GetItemsByCategory(m_ActiveCategory);

            foreach (var item in items)
            {
                if (item.Prefab == null) continue;

                // Apply search filter
                if (!string.IsNullOrEmpty(m_SearchQuery))
                {
                    bool matchName = item.Name != null && item.Name.ToLowerInvariant().Contains(m_SearchQuery);
                    bool matchCat = item.Category != null && item.Category.ToLowerInvariant().Contains(m_SearchQuery);
                    if (!matchName && !matchCat) continue;
                }

                if (m_PresetCardPrefab != null)
                {
                    var card = Instantiate(m_PresetCardPrefab, m_CardContainer);
                    var nameText = card.transform.Find("Title")?.GetComponent<TMP_Text>();
                    if (nameText != null) nameText.text = item.Name;

                    var catText = card.transform.Find("Category")?.GetComponent<TMP_Text>();
                    if (catText != null) catText.text = item.Category;

                    var previewContainer = card.transform.Find("PreviewArea");
                    if (previewContainer != null)
                    {
                        Instantiate(item.Prefab, previewContainer);
                    }
                }
                else
                {
                    // Fallback direct instantiate
                    Instantiate(item.Prefab, m_CardContainer);
                }
            }
        }
    }
}
