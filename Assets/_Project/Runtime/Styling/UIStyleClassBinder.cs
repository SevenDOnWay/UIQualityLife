using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIQualityLife.ProceduralUI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Attaches to any UGUI element to apply CSS class selectors (e.g. "btn btn-danger rounded-lg")
    /// and ID selectors (#submit-button).
    /// Reactively recalculates and updates geometry, colors, borders, and animations whenever
    /// design variables or class rules change.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI Quality of Life/Styling/UI Style Class Binder")]
    public class UIStyleClassBinder : MonoBehaviour
    {
        [Header("CSS Selectors")]
        [Tooltip("Space-separated CSS class names (e.g. 'btn btn-danger rounded-lg card-surface')")]
        [SerializeField] private string m_ClassNames = "btn btn-danger";

        [Tooltip("Optional element ID selector (e.g. 'submit-btn' or '#submit-btn')")]
        [SerializeField] private string m_ElementId = "";

        [Header("StyleSheet Source")]
        [Tooltip("Optional stylesheet override. If null, UIStyleSheet.ActiveStyleSheet is used.")]
        [SerializeField] private UIStyleSheet m_StyleSheetOverride;

        [Tooltip("Legacy single StyleClass asset for backwards compatibility.")]
        [SerializeField] private UIStyleClass m_LegacyStyleClass;

        // Cached component references
        private ProceduralImage m_CachedProceduralImage;
        private Graphic m_CachedGraphic;
        private TMP_Text m_CachedText;
        private UIClassAnimator m_CachedAnimator;

        public string ClassNames
        {
            get => m_ClassNames;
            set
            {
                if (m_ClassNames != value)
                {
                    m_ClassNames = value;
                    ApplyStyleClass();
                }
            }
        }

        public string ElementId
        {
            get => m_ElementId;
            set
            {
                if (m_ElementId != value)
                {
                    m_ElementId = value;
                    ApplyStyleClass();
                }
            }
        }

        public UIStyleSheet StyleSheet => m_StyleSheetOverride != null ? m_StyleSheetOverride : UIStyleSheet.ActiveStyleSheet;

        public UIStyleClass LegacyStyleClass
        {
            get => m_LegacyStyleClass;
            set
            {
                m_LegacyStyleClass = value;
                ApplyStyleClass();
            }
        }

        private void OnEnable()
        {
            CacheComponents();
            SubscribeEvents();
            ApplyStyleClass();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnValidate()
        {
            CacheComponents();
            ApplyStyleClass();
        }

        private void SubscribeEvents()
        {
            UIStyleSheet.OnAnyStyleSheetChanged += HandleStyleSheetChanged;
            UIStyleSheet.OnVariableChanged += HandleVariableChanged;
            UIStyleSheet.OnRuleChanged += HandleRuleChanged;
            UIStyleClass.OnStyleClassChanged += HandleLegacyStyleChanged;
            UIThemeManager.OnThemeChanged += HandleThemeChanged;
        }

        private void UnsubscribeEvents()
        {
            UIStyleSheet.OnAnyStyleSheetChanged -= HandleStyleSheetChanged;
            UIStyleSheet.OnVariableChanged -= HandleVariableChanged;
            UIStyleSheet.OnRuleChanged -= HandleRuleChanged;
            UIStyleClass.OnStyleClassChanged -= HandleLegacyStyleChanged;
            UIThemeManager.OnThemeChanged -= HandleThemeChanged;
        }

        private void HandleStyleSheetChanged() => ApplyStyleClass();
        private void HandleLegacyStyleChanged() => ApplyStyleClass();
        private void HandleThemeChanged() => ApplyStyleClass();

        private void HandleVariableChanged(string varName)
        {
            // Granular check: update when any variable changes
            ApplyStyleClass();
        }

        private void HandleRuleChanged(string selector)
        {
            if (IsSubscribedToSelector(selector))
            {
                ApplyStyleClass();
            }
        }

        public bool IsSubscribedToSelector(string selector)
        {
            if (string.IsNullOrEmpty(selector)) return false;
            string clean = selector.Trim().TrimStart('.', '#');

            var classes = GetParsedClassList();
            for (int i = 0; i < classes.Count; i++)
            {
                if (string.Equals(classes[i], clean, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (!string.IsNullOrEmpty(m_ElementId))
            {
                string cleanId = m_ElementId.Trim().TrimStart('#');
                if (string.Equals(cleanId, clean, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public List<string> GetParsedClassList()
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(m_ClassNames)) return list;

            string[] tokens = m_ClassNames.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                string clean = tokens[i].Trim().TrimStart('.');
                if (!string.IsNullOrEmpty(clean) && !list.Contains(clean))
                {
                    list.Add(clean);
                }
            }
            return list;
        }

        public void CacheComponents()
        {
            m_CachedProceduralImage = GetComponent<ProceduralImage>();
            m_CachedGraphic = GetComponent<Graphic>();
            m_CachedText = GetComponentInChildren<TMP_Text>();
            m_CachedAnimator = GetComponent<UIClassAnimator>();
        }

        /// <summary>
        /// Computes and applies compound CSS styles with full cascading inheritance.
        /// Left-to-right class specificity, followed by Element ID override.
        /// </summary>
        public void ApplyStyleClass()
        {
            CacheComponents();

            var sheet = StyleSheet;

            // 1. Check Legacy StyleClass fallback
            if (m_LegacyStyleClass != null)
            {
                ApplyLegacyStyleClass(m_LegacyStyleClass);
                return;
            }

            if (sheet == null) return;

            // Cascading resolved properties
            bool hasBg = false;
            string bgToken = null;

            bool hasBorderColor = false;
            string borderColorToken = null;

            bool hasBorderWidth = false;
            string borderWidthToken = null;

            bool hasRadius = false;
            string radiusToken = null;

            bool hasCornerStyle = false;
            CornerStyle resolvedCornerStyle = CornerStyle.Round;

            bool hasGradient = false;
            string gradientEndToken = null;
            float gradientAngle = 0f;

            bool hasShadow = false;
            string shadowToken = null;

            bool hasTypography = false;
            string textColorToken = null;
            string fontSizeToken = null;
            TMP_FontAsset fontAsset = null;

            bool hasAnimation = false;
            UIAnimationClass animationClass = null;

            // 2. Cascade classes (Left-to-Right)
            var classList = GetParsedClassList();
            for (int i = 0; i < classList.Count; i++)
            {
                if (sheet.TryGetRule("." + classList[i], out var rule) || sheet.TryGetRule(classList[i], out rule))
                {
                    ApplyRuleProperties(rule, ref hasBg, ref bgToken,
                        ref hasBorderColor, ref borderColorToken,
                        ref hasBorderWidth, ref borderWidthToken,
                        ref hasRadius, ref radiusToken,
                        ref hasCornerStyle, ref resolvedCornerStyle,
                        ref hasGradient, ref gradientEndToken, ref gradientAngle,
                        ref hasShadow, ref shadowToken,
                        ref hasTypography, ref textColorToken, ref fontSizeToken, ref fontAsset,
                        ref hasAnimation, ref animationClass);
                }
            }

            // 3. Apply Element ID override (Highest specificity)
            if (!string.IsNullOrWhiteSpace(m_ElementId))
            {
                string cleanId = m_ElementId.Trim().TrimStart('#');
                if (sheet.TryGetRule("#" + cleanId, out var idRule) || sheet.TryGetRule(cleanId, out idRule))
                {
                    ApplyRuleProperties(idRule, ref hasBg, ref bgToken,
                        ref hasBorderColor, ref borderColorToken,
                        ref hasBorderWidth, ref borderWidthToken,
                        ref hasRadius, ref radiusToken,
                        ref hasCornerStyle, ref resolvedCornerStyle,
                        ref hasGradient, ref gradientEndToken, ref gradientAngle,
                        ref hasShadow, ref shadowToken,
                        ref hasTypography, ref textColorToken, ref fontSizeToken, ref fontAsset,
                        ref hasAnimation, ref animationClass);
                }
            }

            // 4. Atomic application to ProceduralImage or Standard Graphic
            if (m_CachedProceduralImage != null)
            {
                if (hasBg && sheet.TryGetColor(bgToken, out var bgColor))
                {
                    m_CachedProceduralImage.color = bgColor;
                }

                if (hasRadius && sheet.TryGetFloat(radiusToken, out var radVal))
                {
                    m_CachedProceduralImage.uniformRadius = radVal;
                }

                if (hasCornerStyle)
                {
                    m_CachedProceduralImage.cornerStyle = resolvedCornerStyle;
                }

                if (hasBorderWidth && sheet.TryGetFloat(borderWidthToken, out var bWidth))
                {
                    m_CachedProceduralImage.borderWidth = bWidth;
                }

                if (hasBorderColor && sheet.TryGetColor(borderColorToken, out var bColor))
                {
                    m_CachedProceduralImage.borderColor = bColor;
                }

                if (hasGradient && !string.IsNullOrEmpty(gradientEndToken) && sheet.TryGetColor(gradientEndToken, out var gradColor))
                {
                    m_CachedProceduralImage.fillMode = FillMode.LinearGradient;
                    m_CachedProceduralImage.gradientEndColor = gradColor;
                    m_CachedProceduralImage.gradientAngle = gradientAngle;
                }

                if (hasShadow && !string.IsNullOrEmpty(shadowToken) && sheet.TryGetShadow(shadowToken, out var shadow))
                {
                    m_CachedProceduralImage.dropShadowSoftness = shadow.softness;
                    m_CachedProceduralImage.dropShadowOffset = shadow.offset;
                    m_CachedProceduralImage.dropShadowColor = shadow.color;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying) EditorUtility.SetDirty(m_CachedProceduralImage);
#endif
            }
            else if (m_CachedGraphic != null)
            {
                if (hasBg && sheet.TryGetColor(bgToken, out var bgColor))
                {
                    m_CachedGraphic.color = bgColor;
#if UNITY_EDITOR
                    if (!Application.isPlaying) EditorUtility.SetDirty(m_CachedGraphic);
#endif
                }
            }

            // 5. Apply Typography
            if (m_CachedText != null && hasTypography)
            {
                if (!string.IsNullOrEmpty(textColorToken) && sheet.TryGetColor(textColorToken, out var txtColor))
                {
                    m_CachedText.color = txtColor;
                }
                if (!string.IsNullOrEmpty(fontSizeToken) && sheet.TryGetFloat(fontSizeToken, out var fSize))
                {
                    m_CachedText.fontSize = fSize;
                }
                if (fontAsset != null)
                {
                    m_CachedText.font = fontAsset;
                }
#if UNITY_EDITOR
                if (!Application.isPlaying) EditorUtility.SetDirty(m_CachedText);
#endif
            }

            // 6. Apply Animation Class
            if (hasAnimation && animationClass != null)
            {
                if (m_CachedAnimator == null)
                {
                    m_CachedAnimator = gameObject.AddComponent<UIClassAnimator>();
                }
                m_CachedAnimator.AnimationClass = animationClass;
            }
        }

        private static void ApplyRuleProperties(UIStyleRule rule,
            ref bool hasBg, ref string bgToken,
            ref bool hasBorderColor, ref string borderColorToken,
            ref bool hasBorderWidth, ref string borderWidthToken,
            ref bool hasRadius, ref string radiusToken,
            ref bool hasCornerStyle, ref CornerStyle cornerStyle,
            ref bool hasGradient, ref string gradientEndToken, ref float gradientAngle,
            ref bool hasShadow, ref string shadowToken,
            ref bool hasTypography, ref string textColorToken, ref string fontSizeToken, ref TMP_FontAsset fontAsset,
            ref bool hasAnimation, ref UIAnimationClass animationClass)
        {
            if (rule == null) return;

            if (rule.overrideBackground) { hasBg = true; bgToken = rule.backgroundColorToken; }
            if (rule.overrideBorder)
            {
                hasBorderColor = true; borderColorToken = rule.borderColorToken;
                hasBorderWidth = true; borderWidthToken = rule.borderWidthToken;
            }
            if (rule.overrideCornerRadius) { hasRadius = true; radiusToken = rule.cornerRadiusToken; }
            if (rule.overrideCornerStyle) { hasCornerStyle = true; cornerStyle = rule.cornerStyle; }
            if (rule.overrideGradient)
            {
                hasGradient = true;
                gradientEndToken = rule.gradientEndColorToken;
                gradientAngle = rule.gradientAngle;
            }
            if (rule.overrideShadow) { hasShadow = true; shadowToken = rule.shadowToken; }
            if (rule.overrideTypography)
            {
                hasTypography = true;
                textColorToken = rule.textColorToken;
                fontSizeToken = rule.fontSizeToken;
                if (rule.fontAsset != null) fontAsset = rule.fontAsset;
            }
            if (rule.overrideAnimation && rule.animationClass != null)
            {
                hasAnimation = true;
                animationClass = rule.animationClass;
            }
        }

        private void ApplyLegacyStyleClass(UIStyleClass legacy)
        {
            var themeBinder = GetComponent<UIThemeBinder>();
            if (themeBinder == null)
            {
                themeBinder = gameObject.AddComponent<UIThemeBinder>();
            }

            themeBinder.ColorToken = legacy.ColorToken;
            themeBinder.BorderColorToken = legacy.BorderColorToken;
            themeBinder.RadiusToken = legacy.RadiusToken;
            themeBinder.ApplyTheme();

            if (legacy.AnimationClass != null)
            {
                if (m_CachedAnimator == null)
                {
                    m_CachedAnimator = gameObject.AddComponent<UIClassAnimator>();
                }
                m_CachedAnimator.AnimationClass = legacy.AnimationClass;
            }
        }
    }
}
