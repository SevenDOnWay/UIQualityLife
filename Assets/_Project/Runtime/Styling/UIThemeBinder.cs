using System;
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
    /// Attaches to any UGUI element to automatically synchronize its color, borders,
    /// and dimensions with the central UI Theme Palette (like CSS variables).
    /// Updates in real-time in both Editor and Runtime.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI Quality of Life/Styling/UI Theme Binder")]
    public class UIThemeBinder : MonoBehaviour
    {
        [Tooltip("Optional theme asset override. If null, the global active theme is used.")]
        [SerializeField] private UIThemePalette m_ThemeOverride;

        [Header("Main Graphic Color")]
        [Tooltip("CSS color token name (e.g. primary, surface, text-primary, accent)")]
        [SerializeField] private string m_ColorToken = "primary";
        [SerializeField] private bool m_ApplyGraphicColor = true;
        [SerializeField] private bool m_PreserveOriginalAlpha = false;

        [Header("Procedural UI Attributes")]
        [Tooltip("Border color token name for ProceduralImage")]
        [SerializeField] private string m_BorderColorToken = "";
        [SerializeField] private bool m_ApplyBorderColor = false;

        [Tooltip("Gradient end color token for ProceduralImage")]
        [SerializeField] private string m_GradientColorToken = "";
        [SerializeField] private bool m_ApplyGradientColor = false;

        [Tooltip("Corner radius token name for ProceduralImage (e.g. radius-md)")]
        [SerializeField] private string m_RadiusToken = "";
        [SerializeField] private bool m_ApplyRadius = false;

        [Tooltip("Border width token name for ProceduralImage (e.g. border-thin)")]
        [SerializeField] private string m_BorderWidthToken = "";
        [SerializeField] private bool m_ApplyBorderWidth = false;

        // Cached component references
        private Graphic m_CachedGraphic;
        private ProceduralImage m_CachedProceduralImage;
        private CanvasGroup m_CachedCanvasGroup;

        /// <summary>
        /// The active theme resolved for this binder (returns <see cref="ThemeOverride"/> if assigned, otherwise <see cref="UIThemeManager.ActiveTheme"/>).
        /// </summary>
        public UIThemePalette Theme => m_ThemeOverride != null ? m_ThemeOverride : UIThemeManager.ActiveTheme;

        /// <summary>
        /// Explicit theme palette override. If null, the global <see cref="UIThemeManager.ActiveTheme"/> is used.
        /// </summary>
        public UIThemePalette ThemeOverride
        {
            get => m_ThemeOverride;
            set { m_ThemeOverride = value; ApplyTheme(); }
        }

        /// <summary>
        /// Color design token ID applied to the main Graphic component (e.g. "primary", "surface").
        /// Automatically reapplies theme when set.
        /// </summary>
        public string ColorToken
        {
            get => m_ColorToken;
            set { m_ColorToken = value; ApplyTheme(); }
        }

        /// <summary>
        /// Gets or sets whether the graphic color is synchronized from the theme.
        /// </summary>
        public bool ApplyGraphicColor
        {
            get => m_ApplyGraphicColor;
            set { m_ApplyGraphicColor = value; ApplyTheme(); }
        }

        /// <summary>
        /// If true, maintains the Graphic's existing alpha channel when applying new theme colors.
        /// </summary>
        public bool PreserveOriginalAlpha
        {
            get => m_PreserveOriginalAlpha;
            set { m_PreserveOriginalAlpha = value; ApplyTheme(); }
        }

        /// <summary>
        /// Color token ID applied to the <see cref="ProceduralImage.borderColor"/>.
        /// </summary>
        public string BorderColorToken
        {
            get => m_BorderColorToken;
            set { m_BorderColorToken = value; ApplyTheme(); }
        }

        /// <summary>
        /// Gets or sets whether border color synchronization is enabled.
        /// </summary>
        public bool ApplyBorderColor
        {
            get => m_ApplyBorderColor;
            set { m_ApplyBorderColor = value; ApplyTheme(); }
        }

        /// <summary>
        /// Color token ID applied to the <see cref="ProceduralImage.gradientEndColor"/>.
        /// </summary>
        public string GradientColorToken
        {
            get => m_GradientColorToken;
            set { m_GradientColorToken = value; ApplyTheme(); }
        }

        /// <summary>
        /// Gets or sets whether gradient end color synchronization is enabled.
        /// </summary>
        public bool ApplyGradientColor
        {
            get => m_ApplyGradientColor;
            set { m_ApplyGradientColor = value; ApplyTheme(); }
        }

        /// <summary>
        /// Float token ID applied to the <see cref="ProceduralImage.uniformRadius"/> (e.g. "radius-md").
        /// </summary>
        public string RadiusToken
        {
            get => m_RadiusToken;
            set { m_RadiusToken = value; ApplyTheme(); }
        }

        /// <summary>
        /// Gets or sets whether corner radius synchronization is enabled.
        /// </summary>
        public bool ApplyRadius
        {
            get => m_ApplyRadius;
            set { m_ApplyRadius = value; ApplyTheme(); }
        }

        /// <summary>
        /// Float token ID applied to the <see cref="ProceduralImage.borderWidth"/> (e.g. "border-thin").
        /// </summary>
        public string BorderWidthToken
        {
            get => m_BorderWidthToken;
            set { m_BorderWidthToken = value; ApplyTheme(); }
        }

        /// <summary>
        /// Gets or sets whether border width synchronization is enabled.
        /// </summary>
        public bool ApplyBorderWidth
        {
            get => m_ApplyBorderWidth;
            set { m_ApplyBorderWidth = value; ApplyTheme(); }
        }

        private void OnEnable()
        {
            CacheComponents();
            UIThemeManager.OnThemeChanged += OnThemeChangedHandler;
            ApplyTheme();
        }

        private void OnDisable()
        {
            UIThemeManager.OnThemeChanged -= OnThemeChangedHandler;
        }

        private void OnValidate()
        {
            CacheComponents();
            ApplyTheme();
        }

        private void OnThemeChangedHandler()
        {
            ApplyTheme();
        }

        /// <summary>
        /// Locates and caches references to <see cref="ProceduralImage"/>, <see cref="Graphic"/>, and <see cref="CanvasGroup"/> on this GameObject.
        /// </summary>
        public void CacheComponents()
        {
            m_CachedProceduralImage = GetComponent<ProceduralImage>();
            m_CachedGraphic = GetComponent<Graphic>();
            m_CachedCanvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Reads tokens from the active theme and applies them to all matching components.
        /// </summary>
        public void ApplyTheme()
        {
            var activeTheme = Theme;
            if (activeTheme == null) return;

            if (m_CachedGraphic == null && m_CachedProceduralImage == null)
            {
                CacheComponents();
            }

            // 1. Apply Graphic Main Color (Image, ProceduralImage, TextMeshProUGUI, etc.)
            if (m_ApplyGraphicColor && !string.IsNullOrEmpty(m_ColorToken) && m_CachedGraphic != null)
            {
                if (activeTheme.TryGetColor(m_ColorToken, out Color targetColor))
                {
                    if (m_PreserveOriginalAlpha)
                    {
                        targetColor.a = m_CachedGraphic.color.a;
                    }

                    if (m_CachedGraphic.color != targetColor)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            Undo.RecordObject(m_CachedGraphic, "Apply UI Theme Color");
                        }
#endif
                        m_CachedGraphic.color = targetColor;
                    }
                }
            }

            // 2. Procedural Image Specific Properties
            if (m_CachedProceduralImage != null)
            {
                // Border Color
                if (m_ApplyBorderColor && !string.IsNullOrEmpty(m_BorderColorToken))
                {
                    if (activeTheme.TryGetColor(m_BorderColorToken, out Color bColor))
                    {
                        if (m_CachedProceduralImage.borderColor != bColor)
                        {
#if UNITY_EDITOR
                            if (!Application.isPlaying) Undo.RecordObject(m_CachedProceduralImage, "Apply UI Border Color");
#endif
                            m_CachedProceduralImage.borderColor = bColor;
                        }
                    }
                }

                // Gradient End Color
                if (m_ApplyGradientColor && !string.IsNullOrEmpty(m_GradientColorToken))
                {
                    if (activeTheme.TryGetColor(m_GradientColorToken, out Color gColor))
                    {
                        if (m_CachedProceduralImage.gradientEndColor != gColor)
                        {
#if UNITY_EDITOR
                            if (!Application.isPlaying) Undo.RecordObject(m_CachedProceduralImage, "Apply UI Gradient Color");
#endif
                            m_CachedProceduralImage.gradientEndColor = gColor;
                        }
                    }
                }

                // Corner Radius
                if (m_ApplyRadius && !string.IsNullOrEmpty(m_RadiusToken))
                {
                    if (activeTheme.TryGetFloat(m_RadiusToken, out float radius))
                    {
                        if (Mathf.Abs(m_CachedProceduralImage.uniformRadius - radius) > 0.001f)
                        {
#if UNITY_EDITOR
                            if (!Application.isPlaying) Undo.RecordObject(m_CachedProceduralImage, "Apply UI Corner Radius");
#endif
                            m_CachedProceduralImage.uniformRadius = radius;
                        }
                    }
                }

                // Border Width
                if (m_ApplyBorderWidth && !string.IsNullOrEmpty(m_BorderWidthToken))
                {
                    if (activeTheme.TryGetFloat(m_BorderWidthToken, out float borderWidth))
                    {
                        if (Mathf.Abs(m_CachedProceduralImage.borderWidth - borderWidth) > 0.001f)
                        {
#if UNITY_EDITOR
                            if (!Application.isPlaying) Undo.RecordObject(m_CachedProceduralImage, "Apply UI Border Width");
#endif
                            m_CachedProceduralImage.borderWidth = borderWidth;
                        }
                    }
                }
            }
        }
    }
}
