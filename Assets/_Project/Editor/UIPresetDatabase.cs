#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UIQualityLife.Styling;
using UIQualityLife.Styling.Editor;

namespace UIQualityLife.Editor
{
    public static class UIPresetDatabase
    {
        public static string PresetsRootFolder => UIQualityLifeSettings.PresetsFolder;
        private static string DefaultButtonsFolder => $"{PresetsRootFolder}/Buttons";

        /// <summary>
        /// Dynamically loads all UIPresetItem ScriptableObject assets across the project.
        /// </summary>
        public static List<UIPresetItem> GetPresets()
        {
            var list = new List<UIPresetItem>();

            string[] guids = AssetDatabase.FindAssets("t:UIPresetItem");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<UIPresetItem>(path);
                if (preset != null && preset.prefab != null)
                {
                    list.Add(preset);
                }
            }

            // Sort alphabetically by primary category, then by name
            list.Sort((a, b) =>
            {
                int catCompare = string.Compare(a.PrimaryCategory, b.PrimaryCategory, System.StringComparison.OrdinalIgnoreCase);
                if (catCompare != 0) return catCompare;
                return string.Compare(a.displayName, b.displayName, System.StringComparison.OrdinalIgnoreCase);
            });

            return list;
        }

        /// <summary>
        /// Dynamically loads all UI items across the library, including component presets,
        /// CSS theme palettes, animation classes, and style classes.
        /// </summary>
        public static List<IUIComponentItem> GetAllLibraryItems()
        {
            var list = new List<IUIComponentItem>();

            // 1. UGUI Component Presets
            var presets = GetPresets();
            foreach (var p in presets)
            {
                list.Add(p);
            }

            // 2. CSS Theme Palettes
            string[] themeGuids = AssetDatabase.FindAssets("t:UIThemePalette");
            foreach (string guid in themeGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var palette = AssetDatabase.LoadAssetAtPath<UIThemePalette>(path);
                if (palette != null)
                {
                    list.Add(new UIThemeShelfItem(palette));
                }
            }

            // 3. CSS Animation Classes
            string[] animGuids = AssetDatabase.FindAssets("t:UIAnimationClass");
            foreach (string guid in animGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var anim = AssetDatabase.LoadAssetAtPath<UIAnimationClass>(path);
                if (anim != null)
                {
                    list.Add(new UIAnimationShelfItem(anim));
                }
            }

            // 4. CSS Style Classes
            string[] styleGuids = AssetDatabase.FindAssets("t:UIStyleClass");
            foreach (string guid in styleGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var style = AssetDatabase.LoadAssetAtPath<UIStyleClass>(path);
                if (style != null)
                {
                    list.Add(new UIStyleClassShelfItem(style));
                }
            }

            return list;
        }

        /// <summary>
        /// Returns all unique category names currently present in project preset assets and styling assets.
        /// </summary>
        public static List<string> GetAllCategories()
        {
            var categories = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "All" };
            var items = GetAllLibraryItems();
            foreach (var item in items)
            {
                if (item.Categories != null)
                {
                    foreach (var cat in item.Categories)
                    {
                        if (!string.IsNullOrWhiteSpace(cat))
                            categories.Add(cat.Trim());
                    }
                }
            }

            var result = new List<string>(categories);
            result.Sort();
            return result;
        }

        /// <summary>
        /// Returns category counts for quick badge display.
        /// </summary>
        public static Dictionary<string, int> GetCategoryCounts()
        {
            var counts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            var items = GetAllLibraryItems();
            counts["All"] = items.Count;

            foreach (var item in items)
            {
                if (item.Categories != null)
                {
                    foreach (var cat in item.Categories)
                    {
                        if (string.IsNullOrWhiteSpace(cat)) continue;
                        string trimmed = cat.Trim();
                        counts[trimmed] = counts.TryGetValue(trimmed, out int c) ? c + 1 : 1;
                    }
                }
            }
            return counts;
        }

        /// <summary>
        /// Returns matching categories for search suggestions.
        /// </summary>
        public static List<string> GetMatchingCategories(string query)
        {
            var all = GetAllCategories();
            all.Remove("All");
            if (string.IsNullOrWhiteSpace(query))
                return all;

            query = query.Trim().ToLowerInvariant();
            if (query.StartsWith("#")) query = query.Substring(1);
            if (query.StartsWith("cat:")) query = query.Substring(4);

            return all.FindAll(c => c.ToLowerInvariant().Contains(query));
        }

        /// <summary>
        /// Creates a brand-new component preset with multiple categories from a GameObject.
        /// </summary>
        public static UIPresetItem CreatePresetFromGameObject(GameObject sourceGO, string presetName, string[] categories, string description, Color previewColor)
        {
            if (sourceGO == null)
                return null;

            if (string.IsNullOrWhiteSpace(presetName))
                presetName = sourceGO.name;

            if (categories == null || categories.Length == 0)
                categories = new string[] { "General" };

            string primaryCat = categories[0];
            string safeName = SanitizeFileName(presetName);
            string componentFolder = $"{PresetsRootFolder}/{SanitizeFileName(primaryCat)}/{safeName}";
            if (!Directory.Exists(componentFolder))
            {
                Directory.CreateDirectory(componentFolder);
                AssetDatabase.Refresh();
            }

            string prefabPath = $"{componentFolder}/{safeName}.prefab";
            string assetPath = $"{componentFolder}/{safeName}.asset";

            // Save as Prefab asset
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(sourceGO, prefabPath, InteractionMode.UserAction);
            if (savedPrefab == null)
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(sourceGO, prefabPath);
            }

            // Create or update the UIPresetItem ScriptableObject
            var preset = AssetDatabase.LoadAssetAtPath<UIPresetItem>(assetPath);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<UIPresetItem>();
                AssetDatabase.CreateAsset(preset, assetPath);
            }

            preset.displayName = presetName;
            preset.categories = categories;
            preset.description = description;
            preset.previewColor = previewColor;
            preset.prefab = savedPrefab;
            preset.systemType = UISystemType.UGUI;

            // Auto-bake crisp 256x256 thumbnail directly into the component folder
            Texture2D bakedThumb = UIThumbnailBaker.BakeThumbnail(savedPrefab, safeName, 256, componentFolder);
            if (bakedThumb != null)
            {
                preset.customThumbnail = bakedThumb;
            }

            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return preset;
        }

        public static Canvas GetOrCreateActiveCanvas() => CanvasService.GetOrCreateActiveCanvas();

        public static void EnsureEventSystem() => CanvasService.EnsureEventSystem();

        public static GameObject SpawnPresetInScene(UIPresetItem preset, Vector2? screenPosition = null) => SceneSpawner.SpawnPreset(preset);

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}
#endif
