using UnityEngine;
using UIQualityLife.ProceduralUI;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIQualityLife.Styling
{
    /// <summary>
    /// Attaches to any UGUI element to apply a complete CSS Style Class
    /// (colors + border + radius + animation preset).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI Quality of Life/Styling/UI Style Class Binder")]
    public class UIStyleClassBinder : MonoBehaviour
    {
        [Tooltip("The CSS Style Class asset applied to this UI element.")]
        [SerializeField] private UIStyleClass m_StyleClass;

        public UIStyleClass StyleClass
        {
            get => m_StyleClass;
            set
            {
                m_StyleClass = value;
                ApplyStyleClass();
            }
        }

        private void OnEnable()
        {
            UIStyleClass.OnStyleClassChanged += HandleStyleClassChanged;
            UIThemeManager.OnThemeChanged += HandleThemeChanged;
            ApplyStyleClass();
        }

        private void OnDisable()
        {
            UIStyleClass.OnStyleClassChanged -= HandleStyleClassChanged;
            UIThemeManager.OnThemeChanged -= HandleThemeChanged;
        }

        private void OnValidate()
        {
            ApplyStyleClass();
        }

        private void HandleStyleClassChanged()
        {
            ApplyStyleClass();
        }

        private void HandleThemeChanged()
        {
            ApplyStyleClass();
        }

        /// <summary>
        /// Applies the style tokens and animation preset to this GameObject.
        /// </summary>
        public void ApplyStyleClass()
        {
            if (m_StyleClass == null) return;

            var activeTheme = UIThemeManager.ActiveTheme;

            // 1. Configure or sync UIThemeBinder
            var themeBinder = GetComponent<UIThemeBinder>();
            if (themeBinder == null)
            {
                themeBinder = gameObject.AddComponent<UIThemeBinder>();
            }

            themeBinder.ColorToken = m_StyleClass.ColorToken;
            themeBinder.BorderColorToken = m_StyleClass.BorderColorToken;
            themeBinder.RadiusToken = m_StyleClass.RadiusToken;
            themeBinder.ApplyTheme();

            // 2. Configure UIClassAnimator if an animation class is specified
            if (m_StyleClass.AnimationClass != null)
            {
                var animator = GetComponent<UIClassAnimator>();
                if (animator == null)
                {
                    animator = gameObject.AddComponent<UIClassAnimator>();
                }
                animator.AnimationClass = m_StyleClass.AnimationClass;
            }
        }
    }
}
