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

        /// <summary>
        /// Event triggered whenever the active theme asset changes or any color/float token within the active theme is modified.
        /// UI binders subscribe to this event to perform real-time updates.
        /// </summary>
        public static event Action OnThemeChanged;

        /// <summary>
        /// Gets or sets the globally active <see cref="UIThemePalette"/>.
        /// When setting a new theme, persists preference and triggers <see cref="OnThemeChanged"/>.
        /// </summary>
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

        /// <summary>
        /// Manually broadcasts theme modification notifications to all active UI elements and binders across scenes.
        /// </summary>
        public static void NotifyThemeChanged()
        {
            OnThemeChanged?.Invoke();
        }

        /// <summary>
        /// Attempts to retrieve a color design token from the currently active theme.
        /// </summary>
        /// <param name="tokenName">Token identifier (e.g. "primary", "surface", "danger").</param>
        /// <param name="color">Output color if token was found, or Color.white if not found.</param>
        /// <returns>True if the token was found; otherwise false.</returns>
        public static bool TryGetColor(string tokenName, out Color color)
        {
            if (ActiveTheme != null)
            {
                return ActiveTheme.TryGetColor(tokenName, out color);
            }
            color = Color.white;
            return false;
        }

        /// <summary>
        /// Retrieves a color design token from the currently active theme, returning a fallback if not found.
        /// </summary>
        /// <param name="tokenName">Token identifier (e.g. "primary", "text-muted").</param>
        /// <param name="fallback">Fallback color to return if the token is not found or no theme is active.</param>
        /// <returns>The color resolved from the theme or the provided fallback.</returns>
        public static Color GetColor(string tokenName, Color fallback = default)
        {
            if (TryGetColor(tokenName, out var color))
                return color;
            return fallback;
        }

        /// <summary>
        /// Attempts to retrieve a float / dimension design token from the currently active theme.
        /// </summary>
        /// <param name="tokenName">Token identifier (e.g. "radius-md", "border-thin").</param>
        /// <param name="value">Output float value if token was found, or 0f if not found.</param>
        /// <returns>True if the token was found; otherwise false.</returns>
        public static bool TryGetFloat(string tokenName, out float value)
        {
            if (ActiveTheme != null)
            {
                return ActiveTheme.TryGetFloat(tokenName, out value);
            }
            value = 0f;
            return false;
        }

        /// <summary>
        /// Retrieves a float / dimension design token from the currently active theme, returning a fallback if not found.
        /// </summary>
        /// <param name="tokenName">Token identifier (e.g. "radius-sm", "border-medium").</param>
        /// <param name="fallback">Fallback float to return if the token is not found or no theme is active.</param>
        /// <returns>The float value resolved from the theme or the provided fallback.</returns>
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
