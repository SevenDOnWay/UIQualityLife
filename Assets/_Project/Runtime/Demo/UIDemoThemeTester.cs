using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIQualityLife.Styling;

namespace UIQualityLife.Runtime.Demo
{
    /// <summary>
    /// Interactive controller for the Theme Switcher tab in the demo.
    /// Allows visitors on itch.io to switch themes live and preview token swatches.
    /// </summary>
    public class UIDemoThemeTester : MonoBehaviour
    {
        [Header("Registry")]
        [SerializeField] private UIThemeRegistry m_ThemeRegistry;
        [SerializeField] private List<UIThemePalette> m_FallbackThemes = new List<UIThemePalette>();

        [Header("UI References")]
        [SerializeField] private TMP_Dropdown m_ThemeDropdown;
        [SerializeField] private TMP_Text m_ActiveThemeNameText;
        [SerializeField] private Transform m_SwatchContainer;
        [SerializeField] private GameObject m_SwatchPrefab;

        private List<UIThemePalette> m_AvailableThemes = new List<UIThemePalette>();

        private void Start()
        {
            InitializeThemes();
            PopulateDropdown();
            RefreshSwatches();
            UIThemeManager.OnThemeChanged += OnThemeChanged;
        }

        private void OnDestroy()
        {
            UIThemeManager.OnThemeChanged -= OnThemeChanged;
        }

        private void InitializeThemes()
        {
            m_AvailableThemes.Clear();

            if (m_ThemeRegistry != null && m_ThemeRegistry.Themes.Count > 0)
            {
                foreach (var t in m_ThemeRegistry.Themes)
                {
                    if (t != null && !m_AvailableThemes.Contains(t))
                        m_AvailableThemes.Add(t);
                }
            }

            foreach (var t in m_FallbackThemes)
            {
                if (t != null && !m_AvailableThemes.Contains(t))
                    m_AvailableThemes.Add(t);
            }

            // Load from Resources if empty
            if (m_AvailableThemes.Count == 0)
            {
                var loaded = Resources.LoadAll<UIThemePalette>("Themes");
                if (loaded != null)
                {
                    foreach (var t in loaded)
                    {
                        if (t != null && !m_AvailableThemes.Contains(t))
                            m_AvailableThemes.Add(t);
                    }
                }
            }
        }

        private void PopulateDropdown()
        {
            if (m_ThemeDropdown == null) return;

            m_ThemeDropdown.ClearOptions();
            var options = new List<string>();

            int selectedIndex = 0;
            var currentActive = UIThemeManager.ActiveTheme;

            for (int i = 0; i < m_AvailableThemes.Count; i++)
            {
                var theme = m_AvailableThemes[i];
                string name = string.IsNullOrEmpty(theme.ThemeName) ? theme.name : theme.ThemeName;
                options.Add(name);

                if (currentActive != null && currentActive == theme)
                {
                    selectedIndex = i;
                }
            }

            m_ThemeDropdown.AddOptions(options);
            m_ThemeDropdown.value = selectedIndex;
            m_ThemeDropdown.onValueChanged.AddListener(OnDropdownThemeSelected);

            UpdateActiveThemeLabel();
        }

        public void SelectThemeByIndex(int index)
        {
            if (index >= 0 && index < m_AvailableThemes.Count)
            {
                UIThemeManager.ActiveTheme = m_AvailableThemes[index];
            }
        }

        public void SelectModernDark()
        {
            var dark = m_AvailableThemes.Find(t => t.name.Contains("ModernDark") || t.ThemeName.Contains("Dark"));
            if (dark != null) UIThemeManager.ActiveTheme = dark;
        }

        public void SelectGameModernDark()
        {
            var gameDark = m_AvailableThemes.Find(t => t.name.Contains("GameModernDark") || t.ThemeName.Contains("Game"));
            if (gameDark != null) UIThemeManager.ActiveTheme = gameDark;
        }

        public void SelectCyberNeon()
        {
            var cyber = m_AvailableThemes.Find(t => t.name.Contains("Cyber") || t.ThemeName.Contains("Cyber"));
            if (cyber != null) UIThemeManager.ActiveTheme = cyber;
        }

        private void OnDropdownThemeSelected(int index)
        {
            SelectThemeByIndex(index);
        }

        private void OnThemeChanged()
        {
            UpdateActiveThemeLabel();
            RefreshSwatches();
        }

        private void UpdateActiveThemeLabel()
        {
            var active = UIThemeManager.ActiveTheme;
            string themeName = active != null ? active.ThemeName : "Default Theme";

            if (m_ActiveThemeNameText != null)
            {
                m_ActiveThemeNameText.text = $"Active Theme: <b>{themeName}</b>";
            }
        }

        private void RefreshSwatches()
        {
            if (m_SwatchContainer == null || m_SwatchPrefab == null) return;

            // Clear existing
            for (int i = m_SwatchContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_SwatchContainer.GetChild(i).gameObject);
            }

            var active = UIThemeManager.ActiveTheme;
            if (active == null) return;

            foreach (var token in active.Tokens)
            {
                if (token.Type != UIThemeTokenType.Color) continue;

                var instance = Instantiate(m_SwatchPrefab, m_SwatchContainer);
                var image = instance.GetComponentInChildren<Image>();
                var texts = instance.GetComponentsInChildren<TMP_Text>();

                if (image != null) image.color = token.ColorValue;
                if (texts.Length > 0) texts[0].text = token.Name;
                if (texts.Length > 1) texts[1].text = $"#{ColorUtility.ToHtmlStringRGBA(token.ColorValue)}";
            }
        }
    }
}
