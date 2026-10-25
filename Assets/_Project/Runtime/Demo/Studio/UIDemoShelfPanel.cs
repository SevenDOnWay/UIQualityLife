using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIQualityLife.ProceduralUI;
using UIQualityLife.Styling;

namespace UIQualityLife.Runtime.Demo.Studio
{
    /// <summary>
    /// Right Panel: Component Library, Styling & CSS Studio, and Code Export.
    /// Lets visitors spawn any of the 38 presets, switch themes, inspect animation profiles, and copy code.
    /// </summary>
    public class UIDemoShelfPanel : MonoBehaviour
    {
        [Header("Target Viewport")]
        [SerializeField] private UIDemoCanvasViewport m_CanvasViewport;

        [Header("Tab Navigation")]
        [SerializeField] private Button m_TabLibraryBtn;
        [SerializeField] private Button m_TabStylingBtn;
        [SerializeField] private Button m_TabExportBtn;

        [Header("Tab Panels")]
        [SerializeField] private GameObject m_TabLibraryPanel;
        [SerializeField] private GameObject m_TabStylingPanel;
        [SerializeField] private GameObject m_TabExportPanel;

        [Header("Library Controls")]
        [SerializeField] private Transform m_FilterPillContainer;
        [SerializeField] private TMP_InputField m_SearchInput;
        [SerializeField] private Transform m_PresetGridContainer;
        [SerializeField] private GameObject m_PresetCardTemplate;

        [Header("Styling Controls")]
        [SerializeField] private TMP_Text m_ActiveThemeText;
        [SerializeField] private Button m_ThemeModernDarkBtn;
        [SerializeField] private Button m_ThemeGameDarkBtn;
        [SerializeField] private Button m_ThemeCyberNeonBtn;
        [SerializeField] private Transform m_SwatchContainer;
        [SerializeField] private GameObject m_SwatchTemplate;

        [Header("Code Export Controls")]
        [SerializeField] private TMP_Text m_GeneratedCodeText;
        [SerializeField] private Button m_CopyCodeBtn;
        [SerializeField] private TMP_Text m_CopyNotificationText;

        private UIPresetCatalog m_PresetCatalog;
        private string m_ActiveCategory = "All";
        private string m_SearchFilter = "";

        private void Awake()
        {
            m_PresetCatalog = Resources.Load<UIPresetCatalog>("Demo/UIPresetCatalog");
        }

        private void Start()
        {
            // Register Tab buttons
            if (m_TabLibraryBtn != null) m_TabLibraryBtn.onClick.AddListener(() => SwitchTab(0));
            if (m_TabStylingBtn != null) m_TabStylingBtn.onClick.AddListener(() => SwitchTab(1));
            if (m_TabExportBtn != null) m_TabExportBtn.onClick.AddListener(() => SwitchTab(2));

            // Library search
            if (m_SearchInput != null)
                m_SearchInput.onValueChanged.AddListener(s => { m_SearchFilter = s.Trim().ToLowerInvariant(); RenderPresets(); });

            // Styling Theme buttons
            if (m_ThemeModernDarkBtn != null) m_ThemeModernDarkBtn.onClick.AddListener(() => SwitchThemeByName("ModernDark"));
            if (m_ThemeGameDarkBtn != null) m_ThemeGameDarkBtn.onClick.AddListener(() => SwitchThemeByName("GameModernDark"));
            if (m_ThemeCyberNeonBtn != null) m_ThemeCyberNeonBtn.onClick.AddListener(() => SwitchThemeByName("CyberNeon"));

            // Export copy button
            if (m_CopyCodeBtn != null) m_CopyCodeBtn.onClick.AddListener(CopyCodeToClipboard);

            // Selection listener to update code export
            UIStudioSelection.OnSelectionChanged += _ => UpdateExportCode();
            UIThemeManager.OnThemeChanged += RefreshThemeSwatches;

            SetupFilterPills();
            RenderPresets();
            RefreshThemeSwatches();
            SwitchTab(0);
        }

        private void OnDestroy()
        {
            UIThemeManager.OnThemeChanged -= RefreshThemeSwatches;
        }

        public void SwitchTab(int tabIndex)
        {
            if (m_TabLibraryPanel != null) m_TabLibraryPanel.SetActive(tabIndex == 0);
            if (m_TabStylingPanel != null) m_TabStylingPanel.SetActive(tabIndex == 1);
            if (m_TabExportPanel != null) m_TabExportPanel.SetActive(tabIndex == 2);

            UpdateTabButtonStyles(tabIndex);

            if (tabIndex == 2) UpdateExportCode();
        }

        private void UpdateTabButtonStyles(int activeIndex)
        {
            UpdateButtonColor(m_TabLibraryBtn, activeIndex == 0);
            UpdateButtonColor(m_TabStylingBtn, activeIndex == 1);
            UpdateButtonColor(m_TabExportBtn, activeIndex == 2);
        }

        private void UpdateButtonColor(Button btn, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<ProceduralImage>();
            if (img != null)
            {
                img.color = isActive ? new Color(0.12f, 0.45f, 0.75f, 1f) : new Color(0.13f, 0.16f, 0.22f, 1f);
            }
        }

        #region Library Tab

        private void SetupFilterPills()
        {
            if (m_FilterPillContainer == null || m_PresetCatalog == null) return;

            var categories = m_PresetCatalog.GetCategories();
            categories.Insert(0, "All");

            foreach (var cat in categories)
            {
                var pillGo = new GameObject("Pill_" + cat);
                pillGo.transform.SetParent(m_FilterPillContainer, false);
                var rt = pillGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(80, 28);

                var img = pillGo.AddComponent<ProceduralImage>();
                img.color = new Color(0.16f, 0.20f, 0.28f, 1f);
                img.uniformRadius = 6f;

                var btn = pillGo.AddComponent<Button>();
                string capturedCat = cat;
                btn.onClick.AddListener(() =>
                {
                    m_ActiveCategory = capturedCat;
                    RenderPresets();
                });

                var textGo = new GameObject("Label");
                textGo.transform.SetParent(pillGo.transform, false);
                var tRt = textGo.AddComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;

                var txt = textGo.AddComponent<TextMeshProUGUI>();
                txt.text = cat;
                txt.fontSize = 12;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
            }
        }

        private void RenderPresets()
        {
            if (m_PresetGridContainer == null || m_PresetCatalog == null) return;

            for (int i = m_PresetGridContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_PresetGridContainer.GetChild(i).gameObject);
            }

            var items = m_PresetCatalog.GetItemsByCategory(m_ActiveCategory);

            foreach (var item in items)
            {
                if (item.Prefab == null) continue;

                // Search query filter
                if (!string.IsNullOrEmpty(m_SearchFilter))
                {
                    bool matchName = item.Name != null && item.Name.ToLowerInvariant().Contains(m_SearchFilter);
                    bool matchCat = item.Category != null && item.Category.ToLowerInvariant().Contains(m_SearchFilter);
                    if (!matchName && !matchCat) continue;
                }

                CreatePresetCard(item);
            }
        }

        private void CreatePresetCard(PresetCatalogItem item)
        {
            var cardGo = new GameObject("Card_" + item.Name);
            cardGo.transform.SetParent(m_PresetGridContainer, false);
            var rt = cardGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(170, 110);

            var img = cardGo.AddComponent<ProceduralImage>();
            img.color = new Color(0.12f, 0.15f, 0.21f, 1f);
            img.uniformRadius = 8f;
            img.borderWidth = 1f;
            img.borderColor = new Color(1f, 1f, 1f, 0.12f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(cardGo.transform, false);
            var tRt = titleGo.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0.55f);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.offsetMin = new Vector2(8, 0);
            tRt.offsetMax = new Vector2(-8, -6);

            var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
            titleTxt.text = $"<b>{item.Name}</b>";
            titleTxt.fontSize = 12;
            titleTxt.alignment = TextAlignmentOptions.TopLeft;
            titleTxt.color = Color.white;

            var catGo = new GameObject("Category");
            catGo.transform.SetParent(cardGo.transform, false);
            var cRt = catGo.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 0.35f);
            cRt.anchorMax = new Vector2(1, 0.55f);
            cRt.offsetMin = new Vector2(8, 0);
            cRt.offsetMax = new Vector2(-8, 0);

            var catTxt = catGo.AddComponent<TextMeshProUGUI>();
            catTxt.text = item.Category;
            catTxt.fontSize = 10;
            catTxt.color = new Color(0f, 0.85f, 1f, 0.8f);

            // Spawn Button
            var spawnBtnGo = new GameObject("Btn_Spawn");
            spawnBtnGo.transform.SetParent(cardGo.transform, false);
            var sRt = spawnBtnGo.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0);
            sRt.anchorMax = new Vector2(1, 0.32f);
            sRt.offsetMin = new Vector2(8, 6);
            sRt.offsetMax = new Vector2(-8, 0);

            var sImg = spawnBtnGo.AddComponent<ProceduralImage>();
            sImg.color = new Color(0.12f, 0.40f, 0.70f, 1f);
            sImg.uniformRadius = 4f;

            var sBtn = spawnBtnGo.AddComponent<Button>();
            var prefabToSpawn = item.Prefab;
            sBtn.onClick.AddListener(() =>
            {
                if (m_CanvasViewport != null)
                {
                    m_CanvasViewport.SpawnPreset(prefabToSpawn);
                }
            });

            var sTxtGo = new GameObject("Label");
            sTxtGo.transform.SetParent(spawnBtnGo.transform, false);
            var stRt = sTxtGo.AddComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = Vector2.zero;
            stRt.offsetMax = Vector2.zero;

            var sTxt = sTxtGo.AddComponent<TextMeshProUGUI>();
            sTxt.text = "+ Spawn";
            sTxt.fontSize = 11;
            sTxt.alignment = TextAlignmentOptions.Center;
            sTxt.color = Color.white;
        }

        #endregion

        #region Styling Tab

        private void SwitchThemeByName(string themeKey)
        {
            var loaded = Resources.LoadAll<UIThemePalette>("Themes");
            if (loaded != null)
            {
                foreach (var t in loaded)
                {
                    if (t != null && (t.name.Contains(themeKey) || t.ThemeName.Contains(themeKey)))
                    {
                        UIThemeManager.ActiveTheme = t;
                        break;
                    }
                }
            }
        }

        private void RefreshThemeSwatches()
        {
            var active = UIThemeManager.ActiveTheme;
            if (m_ActiveThemeText != null)
            {
                m_ActiveThemeText.text = active != null ? $"Active: <b><color=#00E5FF>{active.ThemeName}</color></b>" : "Active: Default";
            }

            if (m_SwatchContainer == null || active == null) return;

            for (int i = m_SwatchContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(m_SwatchContainer.GetChild(i).gameObject);
            }

            foreach (var token in active.Colors)
            {
                if (token == null) continue;

                var rowGo = new GameObject("Swatch_" + token.id);
                rowGo.transform.SetParent(m_SwatchContainer, false);
                var rt = rowGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(175, 40);

                var img = rowGo.AddComponent<ProceduralImage>();
                img.color = new Color(0.12f, 0.15f, 0.20f, 1f);
                img.uniformRadius = 6f;
                img.borderWidth = 1f;
                img.borderColor = new Color(1f, 1f, 1f, 0.1f);

                // Color swatch square
                var block = new GameObject("ColorBlock");
                block.transform.SetParent(rowGo.transform, false);
                var bRt = block.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0, 0);
                bRt.anchorMax = new Vector2(0, 1);
                bRt.anchoredPosition = new Vector2(18, 0);
                bRt.sizeDelta = new Vector2(24, -12);

                var bImg = block.AddComponent<Image>();
                bImg.color = token.color;

                // Name & Hex text
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(rowGo.transform, false);
                var lRt = labelGo.AddComponent<RectTransform>();
                lRt.anchorMin = new Vector2(0, 0);
                lRt.anchorMax = new Vector2(1, 1);
                lRt.offsetMin = new Vector2(36, 0);
                lRt.offsetMax = new Vector2(-6, 0);

                var txt = labelGo.AddComponent<TextMeshProUGUI>();
                txt.text = $"<b>{token.id}</b>\n<size=10><color=#88A0B8>#{ColorUtility.ToHtmlStringRGBA(token.color)}</color></size>";
                txt.fontSize = 11;
                txt.alignment = TextAlignmentOptions.MidlineLeft;
                txt.color = Color.white;
            }
        }

        #endregion

        #region Code Export Tab

        private void UpdateExportCode()
        {
            if (m_GeneratedCodeText == null) return;

            var target = UIStudioSelection.Current;
            if (target == null)
            {
                m_GeneratedCodeText.text =
                    "// Select an element on the canvas to inspect its generated C# & CSS code.\n\n" +
                    "// Example Tailwind Class Setup:\n" +
                    "<div class=\"bg-surface rounded-xl border border-white/10 p-4 shadow-lg\">\n" +
                    "  <button class=\"bg-primary text-white font-medium px-4 py-2 rounded-lg\">\n" +
                    "    Action Button\n" +
                    "  </button>\n" +
                    "</div>";
                return;
            }

            var pImg = target.GetComponent<ProceduralImage>();
            var rt = target.GetComponent<RectTransform>();
            var tmp = target.GetComponentInChildren<TextMeshProUGUI>();

            string code = $"// Generated C# Component Setup: {target.name}\n" +
                          $"var go = new GameObject(\"{target.name}\");\n" +
                          $"var rt = go.AddComponent<RectTransform>();\n" +
                          $"rt.sizeDelta = new Vector2({rt?.sizeDelta.x ?? 120}f, {rt?.sizeDelta.y ?? 40}f);\n\n";

            if (pImg != null)
            {
                code += $"var img = go.AddComponent<ProceduralImage>();\n" +
                        $"img.cornerStyle = CornerStyle.{pImg.cornerStyle};\n" +
                        $"img.uniformRadius = {pImg.uniformRadius:F0}f;\n" +
                        $"img.borderWidth = {pImg.borderWidth:F1}f;\n" +
                        $"img.dropShadowSoftness = {pImg.dropShadowSoftness:F0}f;\n\n";
            }

            if (tmp != null)
            {
                code += $"var label = go.AddComponent<TextMeshProUGUI>();\n" +
                        $"label.text = \"{tmp.text}\";\n" +
                        $"label.fontSize = {tmp.fontSize:F0};\n";
            }

            code += $"\n/* Equivalent CSS Class */\n" +
                    $".{target.name.ToLowerInvariant()} {{\n" +
                    $"  width: {rt?.sizeDelta.x ?? 120:F0}px;\n" +
                    $"  height: {rt?.sizeDelta.y ?? 40:F0}px;\n" +
                    (pImg != null ? $"  border-radius: {pImg.uniformRadius:F0}px;\n  border: {pImg.borderWidth:F1}px solid rgba(255,255,255,0.8);\n" : "") +
                    $"}}";

            m_GeneratedCodeText.text = code;
        }

        private void CopyCodeToClipboard()
        {
            if (m_GeneratedCodeText == null) return;
            GUIUtility.systemCopyBuffer = m_GeneratedCodeText.text;

            if (m_CopyNotificationText != null)
            {
                m_CopyNotificationText.text = "Code Copied to Clipboard!";
                CancelInvoke(nameof(ClearCopyNotification));
                Invoke(nameof(ClearCopyNotification), 2.5f);
            }
        }

        private void ClearCopyNotification()
        {
            if (m_CopyNotificationText != null) m_CopyNotificationText.text = "";
        }

        #endregion
    }
}
