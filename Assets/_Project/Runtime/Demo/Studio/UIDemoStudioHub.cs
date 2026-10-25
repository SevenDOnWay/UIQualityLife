using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIQualityLife.Runtime.Demo.Studio {
    /// <summary>
    /// Master coordinator for the 3-panel Runtime UI Studio.
    /// Manages viewport modes, top-level actions, and diagnostic readouts.
    /// </summary>
    public class UIDemoStudioHub : MonoBehaviour {
        private static bool s_IsInspectMode = false;
        public static bool IsInspectMode => s_IsInspectMode;

        [Header("Panels")]
        [SerializeField] private UIRuntimeInspector m_Inspector;
        [SerializeField] private UIDemoCanvasViewport m_CanvasViewport;
        [SerializeField] private UIDemoShelfPanel m_ShelfPanel;

        [Header("Mode Controls")]
        [SerializeField] private Button m_InteractModeBtn;
        [SerializeField] private Button m_InspectModeBtn;
        [SerializeField] private TMP_Text m_ModeLabel;

        [Header("Header Actions")]
        [SerializeField] private Button m_ClearCanvasBtn;
        [SerializeField] private Button m_ResetLayoutBtn;
        [SerializeField] private Button m_FullscreenBtn;
        [SerializeField] private Button m_GithubBtn;

        [Header("Diagnostics")]
        [SerializeField] private TMP_Text m_FpsText;
        [SerializeField] private TMP_Text m_ResolutionText;

        private float m_DeltaTime;

        private void Start() {
            if ( m_InteractModeBtn != null ) m_InteractModeBtn.onClick.AddListener(() => SetInspectMode(false));
            if ( m_InspectModeBtn != null ) m_InspectModeBtn.onClick.AddListener(() => SetInspectMode(true));

            if ( m_ClearCanvasBtn != null && m_CanvasViewport != null )
                m_ClearCanvasBtn.onClick.AddListener(m_CanvasViewport.ClearCanvas);

            if ( m_ResetLayoutBtn != null && m_CanvasViewport != null )
                m_ResetLayoutBtn.onClick.AddListener(m_CanvasViewport.ResetDefaultLayout);

            if ( m_FullscreenBtn != null )
                m_FullscreenBtn.onClick.AddListener(ToggleFullscreen);

            if ( m_GithubBtn != null )
                m_GithubBtn.onClick.AddListener(OpenGithub);

            SetInspectMode(false);
        }

        private void Update() {
            // FPS calculation
            m_DeltaTime += (Time.unscaledDeltaTime - m_DeltaTime) * 0.1f;
            if ( m_FpsText != null ) {
                float fps = 1.0f / Mathf.Max(0.0001f, m_DeltaTime);
                m_FpsText.text = $"{Mathf.CeilToInt(fps)} FPS";
            }

            if ( m_ResolutionText != null ) {
                m_ResolutionText.text = $"{Screen.width} × {Screen.height}";
            }
        }

        public void SetInspectMode( bool inspect ) {
            s_IsInspectMode = inspect;
            if ( m_CanvasViewport != null ) {
                m_CanvasViewport.SetInspectOverlayActive(inspect);
            }
            UpdateModeUI();
        }

        private void UpdateModeUI() {
            if ( m_ModeLabel != null ) {
                m_ModeLabel.text = s_IsInspectMode ? "Mode: <b><color=#00E5FF>Inspect</color></b>" : "Mode: <b><color=#00FF88>Interact</color></b>";
            }

            if ( m_InteractModeBtn != null ) {
                var img = m_InteractModeBtn.GetComponent<Image>();
                if ( img != null ) img.color = !s_IsInspectMode ? new Color(0.12f, 0.40f, 0.30f, 1f) : new Color(0.15f, 0.18f, 0.24f, 1f);
            }

            if ( m_InspectModeBtn != null ) {
                var img = m_InspectModeBtn.GetComponent<Image>();
                if ( img != null ) img.color = s_IsInspectMode ? new Color(0.10f, 0.35f, 0.55f, 1f) : new Color(0.15f, 0.18f, 0.24f, 1f);
            }
        }

        public void ToggleFullscreen() {
            Screen.fullScreen = !Screen.fullScreen;
        }

        public void OpenGithub() {
            Application.OpenURL("https://github.com/SevenDOnWay/UIQualityLife");
        }
    }
}
