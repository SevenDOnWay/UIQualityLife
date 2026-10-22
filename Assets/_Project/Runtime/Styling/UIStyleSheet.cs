using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Central CSS Style Sheet and Design Token Repository.
    /// Stores both global CSS variables (Colors, Dimensions, Typography, Shadows)
    /// and CSS class/ID rulesets (.btn-danger, .card-modern, #hud-bar).
    /// Bound UI elements reactively update whenever variables or rules change.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUIStyleSheet", menuName = "UI Quality of Life/Styling/Style Sheet")]
    public class UIStyleSheet : ScriptableObject
    {
        [Header("Style Sheet Information")]
        [SerializeField] private string m_StyleSheetName = "Default StyleSheet";
        [TextArea(1, 3)]
        [SerializeField] private string m_Description = "Global design tokens and CSS class rulesets.";

        [Header("CSS Variables (Design Tokens)")]
        [SerializeField] private List<ColorToken> m_Colors = new List<ColorToken>();
        [SerializeField] private List<FloatToken> m_Floats = new List<FloatToken>();
        [SerializeField] private List<TypographyToken> m_Typography = new List<TypographyToken>();
        [SerializeField] private List<ShadowToken> m_Shadows = new List<ShadowToken>();

        [Header("CSS Class & ID Rules")]
        [SerializeField] private List<UIStyleRule> m_Rules = new List<UIStyleRule>();

        // Reactive events
        public static event Action<string> OnVariableChanged;
        public static event Action<string> OnRuleChanged;
        public static event Action OnAnyStyleSheetChanged;

        private static UIStyleSheet s_ActiveStyleSheet;

        public string StyleSheetName => m_StyleSheetName;
        public IReadOnlyList<ColorToken> Colors => m_Colors;
        public IReadOnlyList<FloatToken> Floats => m_Floats;
        public IReadOnlyList<TypographyToken> Typography => m_Typography;
        public IReadOnlyList<ShadowToken> Shadows => m_Shadows;
        public IReadOnlyList<UIStyleRule> Rules => m_Rules;

        public static UIStyleSheet ActiveStyleSheet
        {
            get
            {
                if (s_ActiveStyleSheet == null)
                {
                    LoadActiveStyleSheet();
                }
                return s_ActiveStyleSheet;
            }
            set
            {
                if (s_ActiveStyleSheet != value)
                {
                    s_ActiveStyleSheet = value;
                    OnAnyStyleSheetChanged?.Invoke();
                }
            }
        }

        private static void LoadActiveStyleSheet()
        {
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:UIStyleSheet");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                s_ActiveStyleSheet = AssetDatabase.LoadAssetAtPath<UIStyleSheet>(path);
                return;
            }
#endif
            s_ActiveStyleSheet = Resources.Load<UIStyleSheet>("UIStyleSheet_Default");
        }

        #region Token & Rule Lookups

        public bool TryGetRule(string selector, out UIStyleRule result)
        {
            result = null;
            if (string.IsNullOrEmpty(selector) || m_Rules == null) return false;

            string searchKey = selector.Trim();
            string cleanSearch = searchKey.TrimStart('.', '#');

            for (int i = 0; i < m_Rules.Count; i++)
            {
                var r = m_Rules[i];
                if (r == null || string.IsNullOrEmpty(r.selector)) continue;

                if (string.Equals(r.selector, searchKey, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.CleanSelectorName, cleanSearch, StringComparison.OrdinalIgnoreCase))
                {
                    result = r;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetColor(string tokenOrHex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(tokenOrHex)) return false;

            string key = tokenOrHex.Trim();

            // Resolve alias if needed ({color.danger})
            if (TokenResolverUtils.IsAlias(key))
            {
                key = TokenResolverUtils.ExtractAliasTarget(key);
            }

            // 1. Search in this StyleSheet's color tokens
            if (m_Colors != null)
            {
                for (int i = 0; i < m_Colors.Count; i++)
                {
                    if (string.Equals(m_Colors[i].id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        color = m_Colors[i].color;
                        return true;
                    }
                }
            }

            // 2. Fallback to active UIThemePalette if available
            if (UIThemeManager.ActiveTheme != null && UIThemeManager.ActiveTheme.TryGetColor(key, out color))
            {
                return true;
            }

            // 3. Try parse direct Hex / RGBA
            return TokenResolverUtils.TryParseHexOrRgba(key, out color);
        }

        public bool TryGetFloat(string tokenOrNum, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(tokenOrNum)) return false;

            string key = tokenOrNum.Trim();

            if (TokenResolverUtils.IsAlias(key))
            {
                key = TokenResolverUtils.ExtractAliasTarget(key);
            }

            // 1. Search in this StyleSheet's float tokens
            if (m_Floats != null)
            {
                for (int i = 0; i < m_Floats.Count; i++)
                {
                    if (string.Equals(m_Floats[i].id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = m_Floats[i].value;
                        return true;
                    }
                }
            }

            // 2. Fallback to active UIThemePalette if available
            if (UIThemeManager.ActiveTheme != null && UIThemeManager.ActiveTheme.TryGetFloat(key, out value))
            {
                return true;
            }

            // 3. Try parse direct dimension (e.g. "16px", "1.5rem", "20")
            return TokenResolverUtils.TryParseDimension(key, out value);
        }

        public bool TryGetTypography(string tokenName, out TypographyToken typography)
        {
            typography = null;
            if (string.IsNullOrWhiteSpace(tokenName) || m_Typography == null) return false;

            string key = TokenResolverUtils.ExtractAliasTarget(tokenName.Trim());
            for (int i = 0; i < m_Typography.Count; i++)
            {
                if (string.Equals(m_Typography[i].id, key, StringComparison.OrdinalIgnoreCase))
                {
                    typography = m_Typography[i];
                    return true;
                }
            }
            return false;
        }

        public bool TryGetShadow(string tokenName, out ShadowToken shadow)
        {
            shadow = null;
            if (string.IsNullOrWhiteSpace(tokenName) || m_Shadows == null) return false;

            string key = TokenResolverUtils.ExtractAliasTarget(tokenName.Trim());
            for (int i = 0; i < m_Shadows.Count; i++)
            {
                if (string.Equals(m_Shadows[i].id, key, StringComparison.OrdinalIgnoreCase))
                {
                    shadow = m_Shadows[i];
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Mutation & Notifications

        public void SetColorToken(string id, Color color, string description = "")
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            var existing = m_Colors.Find(c => string.Equals(c.id, id, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.color = color;
                if (!string.IsNullOrEmpty(description)) existing.description = description;
            }
            else
            {
                m_Colors.Add(new ColorToken(id, color, description));
            }
            NotifyVariableChanged(id);
        }

        public void SetFloatToken(string id, float value, string description = "")
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            var existing = m_Floats.Find(f => string.Equals(f.id, id, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.value = value;
                if (!string.IsNullOrEmpty(description)) existing.description = description;
            }
            else
            {
                m_Floats.Add(new FloatToken(id, value, description));
            }
            NotifyVariableChanged(id);
        }

        public void AddOrUpdateRule(UIStyleRule rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.selector)) return;
            int idx = m_Rules.FindIndex(r => string.Equals(r.selector, rule.selector, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                m_Rules[idx] = rule;
            }
            else
            {
                m_Rules.Add(rule);
            }
            NotifyRuleChanged(rule.selector);
        }

        public void NotifyVariableChanged(string tokenId)
        {
            OnVariableChanged?.Invoke(tokenId);
            OnAnyStyleSheetChanged?.Invoke();
        }

        public void NotifyRuleChanged(string selector)
        {
            OnRuleChanged?.Invoke(selector);
            OnAnyStyleSheetChanged?.Invoke();
        }

        private void OnValidate()
        {
            OnAnyStyleSheetChanged?.Invoke();
        }

        #endregion
    }
}
