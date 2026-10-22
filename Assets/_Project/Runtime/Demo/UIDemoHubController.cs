using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIQualityLife.Runtime.Demo
{
    /// <summary>
    /// Master hub controller for the UIQualityLife interactive itch.io showcase demo.
    /// Manages navigation tabs, iframe fullscreen scaling, and system diagnostics.
    /// </summary>
    public class UIDemoHubController : MonoBehaviour
    {
        [Header("Tab Panels")]
        [SerializeField] private GameObject m_PresetsTabPanel;
        [SerializeField] private GameObject m_ThemesTabPanel;
        [SerializeField] private GameObject m_ProceduralTabPanel;
        [SerializeField] private GameObject m_AnimationsTabPanel;

        [Header("Navigation Buttons")]
        [SerializeField] private Button m_PresetsNavButton;
        [SerializeField] private Button m_ThemesNavButton;
        [SerializeField] private Button m_ProceduralNavButton;
        [SerializeField] private Button m_AnimationsNavButton;

        [Header("Header & Actions")]
        [SerializeField] private Button m_FullscreenButton;
        [SerializeField] private Button m_GithubButton;

        [Header("Status Bar")]
        [SerializeField] private TMP_Text m_FpsText;
        [SerializeField] private TMP_Text m_ResolutionText;

        private float m_DeltaTime;

        private void Start()
        {
            // Register navigation
            if (m_PresetsNavButton != null) m_PresetsNavButton.onClick.AddListener(ShowPresetsTab);
            if (m_ThemesNavButton != null) m_ThemesNavButton.onClick.AddListener(ShowThemesTab);
            if (m_ProceduralNavButton != null) m_ProceduralNavButton.onClick.AddListener(ShowProceduralTab);
            if (m_AnimationsNavButton != null) m_AnimationsNavButton.onClick.AddListener(ShowAnimationsTab);

            if (m_FullscreenButton != null) m_FullscreenButton.onClick.AddListener(ToggleFullscreen);
            if (m_GithubButton != null) m_GithubButton.onClick.AddListener(OpenGithub);

            // Default view
            ShowPresetsTab();
        }

        private void Update()
        {
            // Calculate smooth FPS for status bar
            m_DeltaTime += (Time.unscaledDeltaTime - m_DeltaTime) * 0.1f;

            if (m_FpsText != null)
            {
                float fps = 1.0f / Mathf.Max(0.0001f, m_DeltaTime);
                m_FpsText.text = $"{Mathf.CeilToInt(fps)} FPS";
            }

            if (m_ResolutionText != null)
            {
                m_ResolutionText.text = $"{Screen.width} × {Screen.height}";
            }
        }

        public void ShowPresetsTab()
        {
            SetTabActive(m_PresetsTabPanel, m_PresetsNavButton);
        }

        public void ShowThemesTab()
        {
            SetTabActive(m_ThemesTabPanel, m_ThemesNavButton);
        }

        public void ShowProceduralTab()
        {
            SetTabActive(m_ProceduralTabPanel, m_ProceduralNavButton);
        }

        public void ShowAnimationsTab()
        {
            SetTabActive(m_AnimationsTabPanel, m_AnimationsNavButton);
        }

        private void SetTabActive(GameObject activePanel, Button activeButton)
        {
            if (m_PresetsTabPanel != null) m_PresetsTabPanel.SetActive(m_PresetsTabPanel == activePanel);
            if (m_ThemesTabPanel != null) m_ThemesTabPanel.SetActive(m_ThemesTabPanel == activePanel);
            if (m_ProceduralTabPanel != null) m_ProceduralTabPanel.SetActive(m_ProceduralTabPanel == activePanel);
            if (m_AnimationsTabPanel != null) m_AnimationsTabPanel.SetActive(m_AnimationsTabPanel == activePanel);
        }

        public void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

        public void OpenGithub()
        {
            Application.OpenURL("https://github.com/SevenDOnWay/UIQualityLife");
        }
    }
}
