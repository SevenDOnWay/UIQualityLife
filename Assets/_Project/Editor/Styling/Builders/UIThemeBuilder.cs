#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;
using UIQualityLife.Styling.Editor;

namespace UIQualityLife.Editor.Styling.Builders
{
    /// <summary>
    /// Builder responsible for creating, updating, and baking thumbnails for <see cref="UIThemePalette"/> assets.
    /// Follows the Single Responsibility Principle (SRP).
    /// </summary>
    public static class UIThemeBuilder
    {
        private static string ThemesRoot => UIQualityLifeSettings.ThemesFolder;

        /// <summary>
        /// Builds a <see cref="UIThemePalette"/> asset and 8x8 swatch thumbnail from a serialized ThemeBuildSpec JSON string.
        /// </summary>
        /// <param name="json">JSON specification string.</param>
        /// <returns>StylingBuildResult detailing status and created asset paths.</returns>
        public static StylingBuildResult BuildThemeFromJson(string json)
        {
            try
            {
                var spec = JsonUtility.FromJson<ThemeBuildSpec>(json);
                if (spec == null || string.IsNullOrWhiteSpace(spec.name))
                {
                    return new StylingBuildResult { success = false, error = "Invalid theme specification: 'name' is required." };
                }

                return BuildTheme(spec);
            }
            catch (Exception ex)
            {
                return new StylingBuildResult { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Creates or updates a <see cref="UIThemePalette"/> asset and generates its auto-framed 8x8 swatch thumbnail.
        /// </summary>
        /// <param name="spec">Specification containing color and float tokens.</param>
        /// <returns>StylingBuildResult detailing status and created asset paths.</returns>
        public static StylingBuildResult BuildTheme(ThemeBuildSpec spec)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.name))
            {
                return new StylingBuildResult { success = false, error = "Invalid theme specification: 'name' is required." };
            }

            string cleanName = SanitizeName(spec.name);
            string folder = $"{ThemesRoot}/{cleanName}";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string assetPath = $"{folder}/{cleanName}.asset";
            var palette = AssetDatabase.LoadAssetAtPath<UIThemePalette>(assetPath);
            bool isNew = palette == null;

            if (isNew)
            {
                palette = ScriptableObject.CreateInstance<UIThemePalette>();
                palette.name = cleanName;
            }

            var so = new SerializedObject(palette);
            so.FindProperty("m_ThemeName").stringValue = !string.IsNullOrEmpty(spec.name) ? spec.name : cleanName;
            if (!string.IsNullOrEmpty(spec.description))
            {
                so.FindProperty("m_Description").stringValue = spec.description;
            }
            so.ApplyModifiedProperties();

            // Populate color tokens
            if (spec.colorTokens != null)
            {
                foreach (var ct in spec.colorTokens)
                {
                    if (string.IsNullOrWhiteSpace(ct.id)) continue;
                    Color c = Color.white;
                    if (!string.IsNullOrEmpty(ct.hex))
                    {
                        ColorUtility.TryParseHtmlString(ct.hex.StartsWith("#") ? ct.hex : "#" + ct.hex, out c);
                    }
                    palette.SetColor(ct.id.Trim(), c);
                }
            }

            // Populate float tokens
            if (spec.floatTokens != null)
            {
                foreach (var ft in spec.floatTokens)
                {
                    if (string.IsNullOrWhiteSpace(ft.id)) continue;
                    palette.SetFloat(ft.id.Trim(), ft.value);
                }
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(palette, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(palette);
            }

            AssetDatabase.SaveAssets();

            // Bake Palette Thumbnail (Square dynamic grid up to 64 colors)
            var thumb = UIThemeThumbnailBaker.BakePaletteThumbnail(palette, 256);
            string thumbPath = $"{folder}/{cleanName}_thumb.png";

            AssetDatabase.Refresh();

            return new StylingBuildResult
            {
                success = true,
                name = cleanName,
                assetPath = assetPath,
                thumbPath = thumbPath
            };
        }

        private static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Unnamed";
            char[] arr = raw.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (!char.IsLetterOrDigit(arr[i]) && arr[i] != '_' && arr[i] != '-')
                {
                    arr[i] = '_';
                }
            }
            return new string(arr).Trim('_');
        }
    }
}
#endif
