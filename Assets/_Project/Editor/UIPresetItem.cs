#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Editor
{
    public enum UISystemType
    {
        UGUI,
        UIToolkit
    }

    public enum UIPresetViewMode
    {
        List = 0,
        Content = 1,
        SmallIcon = 2,
        LargeIcon = 3
    }

    [CreateAssetMenu(fileName = "NewPreset", menuName = "UI Quality of Life/UI Preset", order = 100)]
    public class UIPresetItem : ScriptableObject
    {
        public string displayName = "New Preset";

        // Multiple categories support
        public string[] categories = new string[] { "Buttons" };

        // For backwards compatibility with older serialized assets
        [SerializeField] private string category;

        public UISystemType systemType = UISystemType.UGUI;

        [TextArea(2, 4)]
        public string description;

        public Color previewColor = new Color(0.14f, 0.45f, 0.95f);
        public Texture2D customThumbnail;
        public GameObject prefab;
        public string[] searchTags = Array.Empty<string>();

        private void OnEnable()
        {
            // Migrate single category to array if needed
            if ((categories == null || categories.Length == 0) && !string.IsNullOrEmpty(category))
            {
                categories = new string[] { category };
            }
        }

        public string PrimaryCategory => (categories != null && categories.Length > 0 && !string.IsNullOrEmpty(categories[0])) 
            ? categories[0] 
            : "General";

        public bool HasCategory(string cat)
        {
            if (string.IsNullOrEmpty(cat) || string.Equals(cat, "All", StringComparison.OrdinalIgnoreCase))
                return true;

            if (categories == null || categories.Length == 0)
                return false;

            for (int i = 0; i < categories.Length; i++)
            {
                if (string.Equals(categories[i]?.Trim(), cat.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public string GetCategoriesDisplay()
        {
            if (categories == null || categories.Length == 0)
                return "General";
            return string.Join(", ", categories);
        }

        /// <summary>
        /// Matches against displayName, description, searchTags, and ALL categories as search tags.
        /// </summary>
        public bool MatchesSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            query = query.Trim().ToLowerInvariant();

            // Support tag search syntax like "#button" or "cat:hud"
            if (query.StartsWith("#"))
                query = query.Substring(1);
            else if (query.StartsWith("cat:"))
                query = query.Substring(4);

            // 1. Match in Categories (Category as Search Tag)
            if (categories != null)
            {
                foreach (var cat in categories)
                {
                    if (!string.IsNullOrEmpty(cat) && cat.ToLowerInvariant().Contains(query))
                        return true;
                }
            }

            // 2. Match in Display Name
            if (!string.IsNullOrEmpty(displayName) && displayName.ToLowerInvariant().Contains(query))
                return true;

            // 3. Match in Description
            if (!string.IsNullOrEmpty(description) && description.ToLowerInvariant().Contains(query))
                return true;

            // 4. Match in Extra Search Tags
            if (searchTags != null)
            {
                foreach (var tag in searchTags)
                {
                    if (!string.IsNullOrEmpty(tag) && tag.ToLowerInvariant().Contains(query))
                        return true;
                }
            }

            return false;
        }

        public Texture2D GetPreviewTexture()
        {
            if (customThumbnail != null)
                return customThumbnail;

            // 1. Auto-load convention-based baked thumbnail if file exists on disk
            string autoThumbPath = $"{UIThumbnailBaker.ThumbnailsFolder}/{name}_thumb.png";
            var baked = AssetDatabase.LoadAssetAtPath<Texture2D>(autoThumbPath);
            if (baked != null)
            {
                customThumbnail = baked;
                return customThumbnail;
            }

            // 2. On-demand bake: if missing, bake only this individual component right now
            if (prefab != null)
            {
                customThumbnail = UIThumbnailBaker.BakeThumbnail(prefab, name);
                if (customThumbnail != null)
                {
                    EditorUtility.SetDirty(this);
                    return customThumbnail;
                }
            }

            return null;
        }
    }
}
#endif
