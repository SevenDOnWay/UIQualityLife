#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Editor;

namespace UIQualityLife.Styling.Editor
{
    /// <summary>
    /// Adapts a UIThemePalette asset into an IUIComponentItem so it appears seamlessly in the UI Asset Shelf.
    /// </summary>
    public class UIThemeShelfItem : IUIComponentItem
    {
        private readonly UIThemePalette m_Palette;
        private Texture2D m_CachedThumbnail;

        public UIThemePalette Palette => m_Palette;

        public string DisplayName => m_Palette != null ? m_Palette.ThemeName : "Unnamed Palette";
        public string[] Categories => new string[] { "Themes", "CSS" };
        public string PrimaryCategory => "Themes";
        public string Description => "CSS Theme Palette holding colors and design tokens.";
        public Color PreviewColor => m_Palette != null ? m_Palette.GetColor("primary", Color.cyan) : Color.cyan;
        public UISystemType SystemType => UISystemType.UGUI;
        public GameObject Prefab => null;

        public UIThemeShelfItem(UIThemePalette palette)
        {
            m_Palette = palette;
        }

        public Texture2D GetPreviewTexture()
        {
            if (m_CachedThumbnail != null) return m_CachedThumbnail;

            if (m_Palette != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(m_Palette);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string folder = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                    string localThumbPath = $"{folder}/{m_Palette.name}_thumb.png";
                    var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(localThumbPath);
                    if (existing != null)
                    {
                        m_CachedThumbnail = existing;
                        return m_CachedThumbnail;
                    }
                }

                // Bake on-demand
                m_CachedThumbnail = UIThemeThumbnailBaker.BakePaletteThumbnail(m_Palette);
                return m_CachedThumbnail;
            }

            return null;
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

            // Also search inside color token names!
            if (m_Palette != null)
            {
                foreach (var col in m_Palette.Colors)
                {
                    if (col.id.ToLowerInvariant().Contains(query)) return true;
                }
            }

            return false;
        }
    }
}
#endif
