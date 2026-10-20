#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UIQualityLife.Editor
{
    public static class UIPresetDatabase
    {
        public const string PresetsRootFolder = "Assets/_Project/Presets";
        private const string DefaultButtonsFolder = "Assets/_Project/Presets/Buttons";

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
        /// Returns all unique category names currently present in project preset assets.
        /// </summary>
        public static List<string> GetAllCategories()
        {
            var categories = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "All" };
            var presets = GetPresets();
            foreach (var p in presets)
            {
                if (p.categories != null)
                {
                    foreach (var cat in p.categories)
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
            var presets = GetPresets();
            counts["All"] = presets.Count;

            foreach (var p in presets)
            {
                if (p.categories != null)
                {
                    foreach (var cat in p.categories)
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
            string folder = $"{PresetsRootFolder}/{SanitizeFileName(primaryCat)}";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            string safeName = SanitizeFileName(presetName);
            string prefabPath = $"{folder}/{safeName}.prefab";
            string assetPath = $"{folder}/{safeName}.asset";

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

            // Auto-bake crisp 256x256 thumbnail
            Texture2D bakedThumb = UIThumbnailBaker.BakeThumbnail(savedPrefab, safeName);
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
