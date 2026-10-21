#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Editor;

namespace UIQualityLife.Styling.Editor
{
    /// <summary>
    /// Adapts a UIAnimationClass asset into an IUIComponentItem so it appears seamlessly in the UI Asset Shelf.
    /// </summary>
    public class UIAnimationShelfItem : IUIComponentItem
    {
        private readonly UIAnimationClass m_AnimClass;
        private Texture2D m_CachedThumbnail;

        public UIAnimationClass AnimClass => m_AnimClass;

        public string DisplayName => m_AnimClass != null ? m_AnimClass.ClassName : "Unnamed Animation";
        public string[] Categories => new string[] { "Animations", "CSS" };
        public string PrimaryCategory => "Animations";
        public string Description => "CSS-like interaction transitions and keyframe loops.";
        public Color PreviewColor => new Color(0.55f, 0.34f, 0.96f);
        public UISystemType SystemType => UISystemType.UGUI;
        public GameObject Prefab => null;

        public UIAnimationShelfItem(UIAnimationClass animClass)
        {
            m_AnimClass = animClass;
        }

        public Texture2D GetPreviewTexture()
        {
            if (m_CachedThumbnail != null) return m_CachedThumbnail;

            // Use default animation icon fallback
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

            if (m_AnimClass != null)
            {
                if (m_AnimClass.LoopType.ToString().ToLowerInvariant().Contains(query)) return true;
                foreach (var st in m_AnimClass.States)
                {
                    if (st.stateName.ToLowerInvariant().Contains(query)) return true;
                }
            }

            return false;
        }
    }
}
#endif
