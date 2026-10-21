#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Editor;

namespace UIQualityLife.Styling.Editor
{
    /// <summary>
    /// Adapts a UIStyleClass asset into an IUIComponentItem so it appears seamlessly in the UI Asset Shelf.
    /// </summary>
    public class UIStyleClassShelfItem : IUIComponentItem
    {
        private readonly UIStyleClass m_StyleClass;
        private Texture2D m_CachedThumbnail;

        public UIStyleClass StyleClass => m_StyleClass;

        public string DisplayName => m_StyleClass != null ? m_StyleClass.ClassName : "Unnamed Style";
        public string[] Categories => new string[] { "Styles", "CSS" };
        public string PrimaryCategory => "Styles";
        public string Description => "Compound CSS class combining colors, radius, and animation.";
        public Color PreviewColor => new Color(0.13f, 0.77f, 0.37f);
        public UISystemType SystemType => UISystemType.UGUI;
        public GameObject Prefab => null;

        public UIStyleClassShelfItem(UIStyleClass styleClass)
        {
            m_StyleClass = styleClass;
        }

        public Texture2D GetPreviewTexture()
        {
            if (m_CachedThumbnail != null) return m_CachedThumbnail;

            // 1. Look in the component's own folder first
            if (m_StyleClass != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(m_StyleClass);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string dir = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                    string localThumbPath = $"{dir}/{m_StyleClass.name}_thumb.png";
                    var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(localThumbPath);
                    if (existing != null)
                    {
                        m_CachedThumbnail = existing;
                        return m_CachedThumbnail;
                    }
                }
            }

            // 2. Fallback to default animation thumbnail
            m_CachedThumbnail = UIAnimationThumbnailBaker.GetOrCreateDefaultAnimationThumbnail();
            return m_CachedThumbnail;
        }

        public bool HasCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var c in Categories)
            {
                if (string.Equals(c, category, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public bool MatchesSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            query = query.Trim().ToLowerInvariant();
            if (query.StartsWith("#")) query = query.Substring(1);
            if (query.StartsWith("cat:")) query = query.Substring(4);

            if (DisplayName.ToLowerInvariant().Contains(query)) return true;
            if (Description.ToLowerInvariant().Contains(query)) return true;
            foreach (var c in Categories)
            {
                if (c.ToLowerInvariant().Contains(query)) return true;
            }

            if (m_StyleClass != null)
            {
                if (!string.IsNullOrEmpty(m_StyleClass.ColorToken) && m_StyleClass.ColorToken.ToLowerInvariant().Contains(query)) return true;
                if (!string.IsNullOrEmpty(m_StyleClass.RadiusToken) && m_StyleClass.RadiusToken.ToLowerInvariant().Contains(query)) return true;
            }

            return false;
        }
    }
}
#endif
