using System;
using UnityEngine;

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Represents a compound CSS-like Style Class (e.g. .btn-primary, .card-surface, .badge-warning).
    /// Combines palette tokens (colors, border, radius) with an interactive animation class.
    /// Devs can edit this single asset and all UI elements with this class update in 1 click!
    /// </summary>
    [CreateAssetMenu(fileName = "NewUIStyleClass", menuName = "UI Quality of Life/Styling/Style Class")]
    public class UIStyleClass : ScriptableObject
    {
        [Header("CSS Class Identifier")]
        [Tooltip("The class selector name (e.g. .btn-primary, .card-modern)")]
        [SerializeField] private string m_ClassName = ".btn-primary";

        [TextArea(1, 3)]
        [SerializeField] private string m_Description = "Primary button with theme colors and interactive micro-animations.";

        [Header("Design Tokens")]
        [Tooltip("Color token name in UIThemePalette (e.g. primary, surface, text-primary)")]
        [SerializeField] private string m_ColorToken = "primary";

        [Tooltip("Border color token name for ProceduralImage")]
        [SerializeField] private string m_BorderColorToken = "border";

        [Tooltip("Gradient end color token for ProceduralImage (optional)")]
        [SerializeField] private string m_GradientEndColorToken = "";

        [Tooltip("Corner radius token name (e.g. radius-md)")]
        [SerializeField] private string m_RadiusToken = "radius-md";

        [Tooltip("Border width token name (e.g. border-thin)")]
        [SerializeField] private string m_BorderWidthToken = "border-thin";

        [Header("Animation Profile")]
        [Tooltip("CSS-like interactive animation class")]
        [SerializeField] private UIAnimationClass m_AnimationClass;

        public static event Action OnStyleClassChanged;

        public string ClassName => m_ClassName;
        public string Description => m_Description;
        public string ColorToken => m_ColorToken;
        public string BorderColorToken => m_BorderColorToken;
        public string GradientEndColorToken => m_GradientEndColorToken;
        public string RadiusToken => m_RadiusToken;
        public string BorderWidthToken => m_BorderWidthToken;
        public UIAnimationClass AnimationClass => m_AnimationClass;

        private void OnValidate()
        {
            OnStyleClassChanged?.Invoke();
        }
    }
}
