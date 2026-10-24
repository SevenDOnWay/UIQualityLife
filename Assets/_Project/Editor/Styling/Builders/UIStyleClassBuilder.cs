#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;

namespace UIQualityLife.Editor.Styling.Builders
{
    /// <summary>
    /// Builder responsible for creating and linking compound <see cref="UIStyleClass"/> assets.
    /// Follows the Single Responsibility Principle (SRP).
    /// </summary>
    public static class UIStyleClassBuilder
    {
        private static string StylesRoot => UIQualityLifeSettings.StylesFolder;

        /// <summary>
        /// Builds a compound <see cref="UIStyleClass"/> asset from serialized StyleBuildSpec JSON.
        /// </summary>
        /// <param name="json">JSON specification string.</param>
        /// <returns>StylingBuildResult detailing status and created asset paths.</returns>
        public static StylingBuildResult BuildStyleFromJson(string json)
        {
            try
            {
                var spec = JsonUtility.FromJson<StyleBuildSpec>(json);
                if (spec == null || string.IsNullOrWhiteSpace(spec.name))
                {
                    return new StylingBuildResult { success = false, error = "Invalid style specification: 'name' is required." };
                }

                return BuildStyle(spec);
            }
            catch (Exception ex)
            {
                return new StylingBuildResult { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Creates or updates a compound <see cref="UIStyleClass"/> asset bundling theme tokens, corner radii, and animation profiles.
        /// </summary>
        /// <param name="spec">Specification details.</param>
        /// <returns>StylingBuildResult detailing status and created asset paths.</returns>
        public static StylingBuildResult BuildStyle(StyleBuildSpec spec)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.name))
            {
                return new StylingBuildResult { success = false, error = "Invalid style specification: 'name' is required." };
            }

            string cleanName = SanitizeName(spec.name);
            string folder = $"{StylesRoot}/{cleanName}";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string assetPath = $"{folder}/{cleanName}.asset";
            var style = AssetDatabase.LoadAssetAtPath<UIStyleClass>(assetPath);
            bool isNew = style == null;

            if (isNew)
            {
                style = ScriptableObject.CreateInstance<UIStyleClass>();
                style.name = cleanName;
            }

            var so = new SerializedObject(style);
            string cls = !string.IsNullOrEmpty(spec.className) 
                ? (spec.className.StartsWith(".") ? spec.className : "." + spec.className)
                : $".{cleanName.ToLowerInvariant()}";

            so.FindProperty("m_ClassName").stringValue = cls;
            if (!string.IsNullOrEmpty(spec.description)) so.FindProperty("m_Description").stringValue = spec.description;
            if (!string.IsNullOrEmpty(spec.colorToken)) so.FindProperty("m_ColorToken").stringValue = spec.colorToken;
            if (!string.IsNullOrEmpty(spec.borderColorToken)) so.FindProperty("m_BorderColorToken").stringValue = spec.borderColorToken;
            if (!string.IsNullOrEmpty(spec.gradientEndColorToken)) so.FindProperty("m_GradientEndColorToken").stringValue = spec.gradientEndColorToken;
            if (!string.IsNullOrEmpty(spec.radiusToken)) so.FindProperty("m_RadiusToken").stringValue = spec.radiusToken;
            if (!string.IsNullOrEmpty(spec.borderWidthToken)) so.FindProperty("m_BorderWidthToken").stringValue = spec.borderWidthToken;

            // Link Animation Class if provided
            if (!string.IsNullOrEmpty(spec.animationClassName))
            {
                var anim = FindAnimationClassByName(spec.animationClassName);
                if (anim != null)
                {
                    so.FindProperty("m_AnimationClass").objectReferenceValue = anim;
                }
            }

            so.ApplyModifiedProperties();

            if (isNew)
            {
                AssetDatabase.CreateAsset(style, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(style);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new StylingBuildResult
            {
                success = true,
                name = cleanName,
                assetPath = assetPath
            };
        }

        private static UIAnimationClass FindAnimationClassByName(string name)
        {
            string[] guids = AssetDatabase.FindAssets("t:UIAnimationClass");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var a = AssetDatabase.LoadAssetAtPath<UIAnimationClass>(path);
                if (a != null && (string.Equals(a.name, name, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(a.ClassName, name, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(a.ClassName, "." + name, StringComparison.OrdinalIgnoreCase)))
                {
                    return a;
                }
            }
            return null;
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
