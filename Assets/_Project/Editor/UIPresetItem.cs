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

    /// <summary>
    /// Metadata ScriptableObject representing a UI component preset in the UI Shelf library.
    /// Stores prefab references, taxonomy categories, search tags, and baked preview thumbnails.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPreset", menuName = "UI Quality of Life/UI Preset", order = 100)]
    public class UIPresetItem : ScriptableObject, IUIComponentItem
    {
        /// <summary>User-facing display name in shelf cards and headers.</summary>
        public string displayName = "New Preset";

        /// <summary>Taxonomy categories this component belongs to (e.g. Buttons, Cards, Modals).</summary>
        public string[] categories = new string[] { "Buttons" };

        // For backwards compatibility with older serialized assets
        [SerializeField] private string category;

        /// <summary>Target UI technology system (UGUI or UIToolkit).</summary>
        public UISystemType systemType = UISystemType.UGUI;

        /// <summary>Descriptive notes and documentation for developers.</summary>
        [TextArea(2, 4)]
        public string description;

        /// <summary>Card accent color used in UI Shelf cards and fallback thumbnails.</summary>
        public Color previewColor = new Color(0.14f, 0.45f, 0.95f);
        /// <summary>Custom or auto-baked 256x256 thumbnail texture.</summary>
        public Texture2D customThumbnail;
        /// <summary>The prefabricated GameObject asset to instantiate when spawned.</summary>
        public GameObject prefab;
        /// <summary>Keywords used for fuzzy search indexing in the shelf search bar.</summary>
        public string[] searchTags = Array.Empty<string>();

        // Explicit interface implementations for IUIComponentItem
        /// <summary>Display name for shelf cards.</summary>
        public string DisplayName => displayName;
        /// <summary>Array of category tags.</summary>
        public string[] Categories => categories;
        /// <summary>Component description.</summary>
        public string Description => description;
        /// <summary>Card accent color.</summary>
        public Color PreviewColor => previewColor;
        /// <summary>UI framework system type.</summary>
        public UISystemType SystemType => systemType;
        /// <summary>Prefab GameObject reference.</summary>
        public GameObject Prefab => prefab;

        private void OnEnable()
        {
            // Migrate single category to array if needed
            if ((categories == null || categories.Length == 0) && !string.IsNullOrEmpty(category))
            {
                categories = new string[] { category };
            }
        }

        /// <summary>Primary category name used for folder organization.</summary>
        public string PrimaryCategory => (categories != null && categories.Length > 0 && !string.IsNullOrEmpty(categories[0])) 
            ? categories[0] 
            : "General";

        /// <summary>
        /// Checks whether this preset belongs to a specific category.
        /// </summary>
        /// <param name="cat">Category name to test (or "All").</param>
        /// <returns>True if the preset belongs to the category; otherwise false.</returns>
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

        /// <summary>
        /// Returns a comma-separated display string of all assigned categories.
        /// </summary>
        public string GetCategoriesDisplay()
        {
            if (categories == null || categories.Length == 0)
                return "General";
            return string.Join(", ", categories);
        }

        /// <summary>
        /// Matches against displayName, description, searchTags, and ALL categories as search tags.
        /// </summary>
        /// <param name="query">Search query string.</param>
        /// <returns>True if query matches any field; otherwise false.</returns>
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

        /// <summary>
        /// Retrieves the 256x256 thumbnail texture, auto-discovering or baking on-demand if missing.
        /// </summary>
        /// <returns>Preview Texture2D asset or null.</returns>
        public Texture2D GetPreviewTexture()
        {
            if (customThumbnail != null)
                return customThumbnail;

            string assetPath = AssetDatabase.GetAssetPath(this);

            // 1. Auto-load local convention-based baked thumbnail if file exists in the component folder
            if (!string.IsNullOrEmpty(assetPath))
            {
                string dir = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
                string localThumbPath = $"{dir}/{name}_thumb.png";
                var bakedLocal = AssetDatabase.LoadAssetAtPath<Texture2D>(localThumbPath);
                if (bakedLocal != null)
                {
                    customThumbnail = bakedLocal;
                    return customThumbnail;
                }
            }

            // 2. Fallback to global thumbnails folder if exists
            string autoThumbPath = $"{UIThumbnailBaker.ThumbnailsFolder}/{name}_thumb.png";
            var baked = AssetDatabase.LoadAssetAtPath<Texture2D>(autoThumbPath);
            if (baked != null)
            {
                customThumbnail = baked;
                return customThumbnail;
            }

            // 3. On-demand bake: if missing, bake inside the component folder
            if (prefab != null)
            {
                string compFolder = !string.IsNullOrEmpty(assetPath) ? System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/') : null;
                customThumbnail = UIThumbnailBaker.BakeThumbnail(prefab, name, 256, compFolder);
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
