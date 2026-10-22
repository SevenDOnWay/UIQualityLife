using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIQualityLife.Runtime.Demo
{
    /// <summary>
    /// Displays presets in the Demo Scene with live filtering by category.
    /// </summary>
    public class UIDemoPresetGallery : MonoBehaviour
    {
        [Header("Catalog Reference")]
        [SerializeField] private UIPresetCatalog m_PresetCatalog;

        [Header("Containers")]
        [SerializeField] private Transform m_CardContainer;
        [SerializeField] private Transform m_FilterContainer;

        [Header("Card Template")]
        [SerializeField] private GameObject m_PresetCardPrefab;
        [SerializeField] private GameObject m_FilterButtonPrefab;

        private string m_ActiveCategory = "All";

        private void Start()
        {
            SetupFilters();
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

                GameObject card;
                if (m_PresetCardPrefab != null)
                {
                    card = Instantiate(m_PresetCardPrefab, m_CardContainer);
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
