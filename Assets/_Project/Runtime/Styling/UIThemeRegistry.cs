using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Runtime registry containing available theme palettes for dynamic switching.
    /// </summary>
    [CreateAssetMenu(fileName = "UIThemeRegistry", menuName = "UI Quality of Life/Theme Registry")]
    public class UIThemeRegistry : ScriptableObject
    {
        [SerializeField] private List<UIThemePalette> m_Themes = new List<UIThemePalette>();

        public IReadOnlyList<UIThemePalette> Themes => m_Themes;

        public UIThemePalette GetThemeByName(string themeName)
        {
            if (string.IsNullOrEmpty(themeName)) return null;
            return m_Themes.Find(t => t != null && string.Equals(t.ThemeName, themeName, StringComparison.OrdinalIgnoreCase));
        }

        public void AddTheme(UIThemePalette theme)
        {
            if (theme != null && !m_Themes.Contains(theme))
            {
                m_Themes.Add(theme);
            }
        }
    }
}
