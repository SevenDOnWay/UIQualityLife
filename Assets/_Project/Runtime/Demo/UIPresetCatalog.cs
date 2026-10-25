using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIQualityLife.Runtime.Demo
{
    [Serializable]
    public class PresetCatalogItem
    {
        public string Name;
        public string Category;
        [TextArea(2, 4)]
        public string Description;
        public GameObject Prefab;
        public Sprite Thumbnail;
        public Texture2D ThumbnailTexture;
    }

    /// <summary>
    /// Runtime catalog of UI presets available for spawning and testing in the showcase demo.
    /// </summary>
    [CreateAssetMenu(fileName = "UIPresetCatalog", menuName = "UI Quality of Life/Preset Catalog")]
    public class UIPresetCatalog : ScriptableObject
    {
        [SerializeField] private List<PresetCatalogItem> m_Items = new List<PresetCatalogItem>();

        public IReadOnlyList<PresetCatalogItem> Items => m_Items;

        public List<string> GetCategories()
        {
            var categories = new HashSet<string>();
            foreach (var item in m_Items)
            {
                if (!string.IsNullOrEmpty(item.Category))
                    categories.Add(item.Category);
            }
            return new List<string>(categories);
        }

        public List<PresetCatalogItem> GetItemsByCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || category == "All")
                return new List<PresetCatalogItem>(m_Items);

            return m_Items.FindAll(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        public void AddItem(PresetCatalogItem item)
        {
            if (item != null && !m_Items.Contains(item))
            {
                m_Items.Add(item);
            }
        }
    }
}
