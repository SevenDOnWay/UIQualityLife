using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Central manager for UI themes and design tokens.
    /// Provides live notifications for Editor and Runtime UI style syncing.
    /// </summary>
    public static class UIThemeManager
    {
        private const string PrefsActiveThemeGuidKey = "UIQualityLife_ActiveTheme_GUID";
        private const string ResourcesDefaultThemePath = "UITheme_Default";

        private static UIThemePalette s_ActiveTheme;
        public static event Action OnThemeChanged;

        public static UIThemePalette ActiveTheme
        {
            get
            {
                if (s_ActiveTheme == null)
                {
                    LoadActiveTheme();
                }
                return s_ActiveTheme;
            }
            set
            {
                if (s_ActiveTheme != value)
                {
                    s_ActiveTheme = value;
                    SaveActiveThemePreference();
                    NotifyThemeChanged();
                }
            }
        }

        public static void NotifyThemeChanged()
        {
            OnThemeChanged?.Invoke();
        }

        public static bool TryGetColor(string tokenName, out Color color)
        {
            if (ActiveTheme != null)
            {
                return ActiveTheme.TryGetColor(tokenName, out color);
            }
            color = Color.white;
            return false;
        }

        public static Color GetColor(string tokenName, Color fallback = default)
        {
            if (TryGetColor(tokenName, out var color))
                return color;
            return fallback;
        }

        public static bool TryGetFloat(string tokenName, out float value)
        {
            if (ActiveTheme != null)
            {
                return ActiveTheme.TryGetFloat(tokenName, out value);
            }
            value = 0f;
            return false;
        }

        public static float GetFloat(string tokenName, float fallback = 0f)
        {
            if (TryGetFloat(tokenName, out var val))
                return val;
            return fallback;
        }

        private static void LoadActiveTheme()
        {
#if UNITY_EDITOR
            string savedGuid = EditorPrefs.GetString(PrefsActiveThemeGuidKey, "");
            if (!string.IsNullOrEmpty(savedGuid))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(savedGuid);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    s_ActiveTheme = AssetDatabase.LoadAssetAtPath<UIThemePalette>(assetPath);
                    if (s_ActiveTheme != null) return;
                }
            }

            // Fallback: search project for any UIThemePalette asset
            string[] guids = AssetDatabase.FindAssets("t:UIThemePalette");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                s_ActiveTheme = AssetDatabase.LoadAssetAtPath<UIThemePalette>(path);
                if (s_ActiveTheme != null)
                {
                    EditorPrefs.SetString(PrefsActiveThemeGuidKey, guids[0]);
                    return;
                }
            }
#else
            s_ActiveTheme = Resources.Load<UIThemePalette>(ResourcesDefaultThemePath);
#endif
        }

        private static void SaveActiveThemePreference()
        {
#if UNITY_EDITOR
            if (s_ActiveTheme != null)
            {
                string path = AssetDatabase.GetAssetPath(s_ActiveTheme);
                string guid = AssetDatabase.AssetPathToGUID(path);
                EditorPrefs.SetString(PrefsActiveThemeGuidKey, guid);
            }
            else
            {
                EditorPrefs.DeleteKey(PrefsActiveThemeGuidKey);
            }
#endif
        }
    }
}
