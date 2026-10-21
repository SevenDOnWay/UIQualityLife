using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIQualityLife.Styling
{
    [Serializable]
    public class ColorToken
    {
        public string id;
        public Color color = Color.white;
        public string description;

        public ColorToken() { }
        public ColorToken(string id, Color color, string description = "")
        {
            this.id = id;
            this.color = color;
            this.description = description;
        }
    }

    [Serializable]
    public class FloatToken
    {
        public string id;
        public float value;
        public string description;

        public FloatToken() { }
        public FloatToken(string id, float value, string description = "")
        {
            this.id = id;
            this.value = value;
            this.description = description;
        }
    }

    /// <summary>
    /// Centralized CSS-like UI Palette & Design Token Asset.
    /// Modifying any token here updates all bound UGUI elements across scenes and prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUIThemePalette", menuName = "UI Quality of Life/Styling/Theme Palette")]
    public class UIThemePalette : ScriptableObject
    {
        [Header("Theme Information")]
        [SerializeField] private string m_ThemeName = "Modern Dark";
        [TextArea(2, 4)]
        [SerializeField] private string m_Description = "Global design tokens and color palette.";

        [Header("Color Tokens (CSS Variables)")]
        [SerializeField] private List<ColorToken> m_Colors = new List<ColorToken>();

        [Header("Dimension / Radius Tokens")]
        [SerializeField] private List<FloatToken> m_Floats = new List<FloatToken>();

        public string ThemeName => m_ThemeName;
        public IReadOnlyList<ColorToken> Colors => m_Colors;
        public IReadOnlyList<FloatToken> Floats => m_Floats;

        public bool TryGetColor(string tokenName, out Color result)
        {
            if (!string.IsNullOrEmpty(tokenName) && m_Colors != null)
            {
                for (int i = 0; i < m_Colors.Count; i++)
                {
                    if (string.Equals(m_Colors[i].id, tokenName, StringComparison.OrdinalIgnoreCase))
                    {
                        result = m_Colors[i].color;
                        return true;
                    }
                }
            }

            result = Color.white;
            return false;
        }

        public Color GetColor(string tokenName, Color fallback = default)
        {
            if (TryGetColor(tokenName, out var color))
                return color;
            return fallback;
        }

        public bool TryGetFloat(string tokenName, out float result)
        {
            if (!string.IsNullOrEmpty(tokenName) && m_Floats != null)
            {
                for (int i = 0; i < m_Floats.Count; i++)
                {
                    if (string.Equals(m_Floats[i].id, tokenName, StringComparison.OrdinalIgnoreCase))
                    {
                        result = m_Floats[i].value;
                        return true;
                    }
                }
            }

            result = 0f;
            return false;
        }

        public float GetFloat(string tokenName, float fallback = 0f)
        {
            if (TryGetFloat(tokenName, out var val))
                return val;
            return fallback;
        }

        public void SetColor(string tokenName, Color color)
        {
            for (int i = 0; i < m_Colors.Count; i++)
            {
                if (string.Equals(m_Colors[i].id, tokenName, StringComparison.OrdinalIgnoreCase))
                {
                    m_Colors[i].color = color;
                    NotifyChanged();
                    return;
                }
            }

            m_Colors.Add(new ColorToken(tokenName, color));
            NotifyChanged();
        }

        public void SetFloat(string tokenName, float value)
        {
            for (int i = 0; i < m_Floats.Count; i++)
            {
                if (string.Equals(m_Floats[i].id, tokenName, StringComparison.OrdinalIgnoreCase))
                {
                    m_Floats[i].value = value;
                    NotifyChanged();
                    return;
                }
            }

            m_Floats.Add(new FloatToken(tokenName, value));
            NotifyChanged();
        }

        public void PopulateDefaultsIfEmpty()
        {
            if (m_Colors.Count == 0)
            {
                // Tailwind / Modern Dark inspired defaults
                m_Colors.Add(new ColorToken("primary", new Color(0.231f, 0.510f, 0.965f, 1f), "Primary action / accent color (#3B82F6)"));
                m_Colors.Add(new ColorToken("primary-hover", new Color(0.149f, 0.384f, 0.886f, 1f), "Primary button hover color (#2563EB)"));
                m_Colors.Add(new ColorToken("secondary", new Color(0.392f, 0.455f, 0.545f, 1f), "Secondary action / muted button (#64748B)"));
                m_Colors.Add(new ColorToken("accent", new Color(0.549f, 0.337f, 0.965f, 1f), "Purple accent (#8B5CF6)"));
                m_Colors.Add(new ColorToken("background", new Color(0.071f, 0.082f, 0.102f, 1f), "App canvas background (#12151A)"));
                m_Colors.Add(new ColorToken("surface", new Color(0.118f, 0.125f, 0.145f, 1f), "Card / Panel background (#1E2025)"));
                m_Colors.Add(new ColorToken("surface-elevated", new Color(0.165f, 0.176f, 0.208f, 1f), "Modal / Popup background (#2A2D35)"));
                m_Colors.Add(new ColorToken("border", new Color(0.247f, 0.267f, 0.314f, 1f), "Subtle container border (#3F4450)"));
                m_Colors.Add(new ColorToken("text-primary", new Color(0.976f, 0.980f, 0.984f, 1f), "Main title and body text (#F8FAFC)"));
                m_Colors.Add(new ColorToken("text-muted", new Color(0.580f, 0.639f, 0.722f, 1f), "Secondary / subtitle text (#94A3B8)"));
                m_Colors.Add(new ColorToken("success", new Color(0.133f, 0.773f, 0.369f, 1f), "Success green (#22C55E)"));
                m_Colors.Add(new ColorToken("warning", new Color(0.961f, 0.620f, 0.043f, 1f), "Warning amber (#F59E0B)"));
                m_Colors.Add(new ColorToken("danger", new Color(0.937f, 0.267f, 0.267f, 1f), "Danger / error red (#EF4444)"));
            }

            if (m_Floats.Count == 0)
            {
                m_Floats.Add(new FloatToken("radius-sm", 6f, "Small corner radius (badges, pills)"));
                m_Floats.Add(new FloatToken("radius-md", 12f, "Standard corner radius (buttons, inputs)"));
                m_Floats.Add(new FloatToken("radius-lg", 20f, "Large corner radius (cards, dialogs)"));
                m_Floats.Add(new FloatToken("border-thin", 1f, "Subtle 1px border"));
                m_Floats.Add(new FloatToken("border-medium", 2f, "Prominent 2px border"));
            }
        }

        private void Reset()
        {
            PopulateDefaultsIfEmpty();
        }

        private void OnValidate()
        {
            NotifyChanged();
        }

        public void NotifyChanged()
        {
            UIThemeManager.NotifyThemeChanged();
        }
    }
}
