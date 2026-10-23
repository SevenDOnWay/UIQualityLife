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
        /// <summary>Fired when a specific design variable or token ID is modified.</summary>
        public static event Action<string> OnVariableChanged;
        /// <summary>Fired when a specific CSS selector ruleset is modified or added.</summary>
        public static event Action<string> OnRuleChanged;
        /// <summary>Fired when any stylesheet is modified or the active stylesheet changes.</summary>
        public static event Action OnAnyStyleSheetChanged;

        private static UIStyleSheet s_ActiveStyleSheet;

        /// <summary>Display name of this style sheet.</summary>
        public string StyleSheetName => m_StyleSheetName;
        /// <summary>Description and design tokens overview.</summary>
        public string Description => m_Description;
        /// <summary>Read-only collection of all color design tokens.</summary>
        public IReadOnlyList<ColorToken> Colors => m_Colors;
        /// <summary>Read-only collection of all float / dimension design tokens.</summary>
        public IReadOnlyList<FloatToken> Floats => m_Floats;
        /// <summary>Read-only collection of all typography tokens.</summary>
        public IReadOnlyList<TypographyToken> Typography => m_Typography;
        /// <summary>Read-only collection of all drop shadow tokens.</summary>
        public IReadOnlyList<ShadowToken> Shadows => m_Shadows;
        /// <summary>Read-only collection of all CSS selector rulesets.</summary>
        public IReadOnlyList<UIStyleRule> Rules => m_Rules;

        /// <summary>
        /// Gets or sets the globally active <see cref="UIStyleSheet"/> repository.
        /// When changed, broadcasts <see cref="OnAnyStyleSheetChanged"/> to refresh all bound UI.
        /// </summary>
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

            s_ActiveStyleSheet = CreateDefaultStyleSheetAsset();
            if (s_ActiveStyleSheet != null) return;
#endif
            s_ActiveStyleSheet = Resources.Load<UIStyleSheet>("UIStyleSheet_Default");
        }

#if UNITY_EDITOR
        public static UIStyleSheet CreateDefaultStyleSheetAsset()
        {
            string folder = "Assets/_Project/Styling/Themes";
            if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);

            string path = $"{folder}/UIStyleSheet_Default.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UIStyleSheet>(path);
            if (existing != null) return existing;

            var sheet = ScriptableObject.CreateInstance<UIStyleSheet>();
            sheet.m_StyleSheetName = "Default StyleSheet";
            sheet.m_Description = "Global design tokens and CSS class rulesets.";

            // Default Colors
            sheet.m_Colors.Add(new ColorToken("danger", new Color(0.937f, 0.267f, 0.267f, 1f), "Danger error red"));
            sheet.m_Colors.Add(new ColorToken("primary", new Color(0.231f, 0.510f, 0.965f, 1f), "Brand primary blue"));
            sheet.m_Colors.Add(new ColorToken("success", new Color(0.063f, 0.725f, 0.506f, 1f), "Success green"));
            sheet.m_Colors.Add(new ColorToken("surface", new Color(0.122f, 0.161f, 0.216f, 1f), "Dark surface slate"));
            sheet.m_Colors.Add(new ColorToken("border", new Color(0.3f, 0.35f, 0.45f, 0.6f), "Border gray"));

            // Default Floats / Radii
            sheet.m_Floats.Add(new FloatToken("radius-sm", 4f, "Small radius"));
            sheet.m_Floats.Add(new FloatToken("radius-md", 8f, "Medium radius"));
            sheet.m_Floats.Add(new FloatToken("radius-lg", 16f, "Large radius"));
            sheet.m_Floats.Add(new FloatToken("radius-xl", 24f, "Extra large radius"));
            sheet.m_Floats.Add(new FloatToken("button", 16f, "Button corner radius"));
            sheet.m_Floats.Add(new FloatToken("pill", 999f, "Pill / circular radius"));
            sheet.m_Floats.Add(new FloatToken("border-thin", 1.5f, "Thin border"));

            // Default Rulesets
            var btnDanger = new UIStyleRule(".btn-danger", "Danger button");
            btnDanger.overrideBackground = true;
            btnDanger.backgroundColorToken = "danger";
            btnDanger.overrideCornerRadius = true;
            btnDanger.cornerRadiusToken = "button";
            sheet.m_Rules.Add(btnDanger);

            var btnPrimary = new UIStyleRule(".btn-primary", "Primary button");
            btnPrimary.overrideBackground = true;
            btnPrimary.backgroundColorToken = "primary";
            btnPrimary.overrideCornerRadius = true;
            btnPrimary.cornerRadiusToken = "button";
            sheet.m_Rules.Add(btnPrimary);

            var roundedPill = new UIStyleRule(".rounded-pill", "Pill shape");
            roundedPill.overrideCornerRadius = true;
            roundedPill.cornerRadiusToken = "pill";
            sheet.m_Rules.Add(roundedPill);

            var roundPill = new UIStyleRule(".round-pill", "Round pill shape alias");
            roundPill.overrideCornerRadius = true;
            roundPill.cornerRadiusToken = "pill";
            sheet.m_Rules.Add(roundPill);

            AssetDatabase.CreateAsset(sheet, path);
            AssetDatabase.SaveAssets();
            return sheet;
        }
#endif

        #region Token & Rule Lookups

        /// <summary>
        /// Attempts to find a style rule by its selector (e.g. ".btn-primary" or "btn-primary").
        /// </summary>
        /// <param name="selector">CSS class or ID selector.</param>
        /// <param name="result">Found <see cref="UIStyleRule"/> or null.</param>
        /// <returns>True if a matching rule was found; otherwise false.</returns>
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

        /// <summary>
        /// Resolves a color token name, token alias ("{colors.primary}"), or direct hex/rgba string.
        /// Searches this stylesheet first, falls back to <see cref="UIThemeManager.ActiveTheme"/>, and parses raw hex/rgba values.
        /// </summary>
        /// <param name="tokenOrHex">Token ID, alias, or hex color.</param>
        /// <param name="color">Resolved Color or Color.white.</param>
        /// <returns>True if successfully resolved or parsed; otherwise false.</returns>
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

        /// <summary>
        /// Resolves a float token name, token alias ("{radii.md}"), or direct dimension string ("16px", "1.5rem", "12").
        /// Searches this stylesheet first, falls back to <see cref="UIThemeManager.ActiveTheme"/>, and parses direct numbers.
        /// </summary>
        /// <param name="tokenOrNum">Token ID, alias, or raw numeric dimension.</param>
        /// <param name="value">Resolved float value or 0f.</param>
        /// <returns>True if successfully resolved or parsed; otherwise false.</returns>
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

        /// <summary>
        /// Resolves a typography token by its identifier or alias.
        /// </summary>
        /// <param name="tokenName">Token identifier.</param>
        /// <param name="typography">Resolved TypographyToken or null.</param>
        /// <returns>True if found; otherwise false.</returns>
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

        /// <summary>
        /// Resolves a drop shadow token by its identifier or alias.
        /// </summary>
        /// <param name="tokenName">Token identifier.</param>
        /// <param name="shadow">Resolved ShadowToken or null.</param>
        /// <returns>True if found; otherwise false.</returns>
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

        /// <summary>
        /// Sets or adds a color design token in this stylesheet and triggers reactive update notifications.
        /// </summary>
        /// <param name="id">Token identifier.</param>
        /// <param name="color">New color value.</param>
        /// <param name="description">Optional description or documentation.</param>
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

        /// <summary>
        /// Sets or adds a float / dimension design token in this stylesheet and triggers reactive update notifications.
        /// </summary>
        /// <param name="id">Token identifier.</param>
        /// <param name="value">New float value.</param>
        /// <param name="description">Optional description or documentation.</param>
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

        /// <summary>
        /// Adds a new CSS style rule or updates an existing rule matching the selector, then triggers reactive notifications.
        /// </summary>
        /// <param name="rule">The <see cref="UIStyleRule"/> to add or update.</param>
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

        /// <summary>
        /// Broadcasts notifications that a specific design variable or token ID has changed.
        /// </summary>
        /// <param name="tokenId">The modified token identifier.</param>
        public void NotifyVariableChanged(string tokenId)
        {
            OnVariableChanged?.Invoke(tokenId);
            OnAnyStyleSheetChanged?.Invoke();
        }

        /// <summary>
        /// Broadcasts notifications that a specific selector ruleset has changed.
        /// </summary>
        /// <param name="selector">The modified selector string.</param>
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
