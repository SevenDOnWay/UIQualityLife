using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UIQualityLife.Styling;
using UIQualityLife.ProceduralUI;
using UIQualityLife.Runtime.Demo;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Editor utility to construct and wire the complete interactive DemoScene.unity for testing in Unity and itch.io.
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string DemoScenePath = "Assets/Scenes/DemoScene.unity";

        [MenuItem("Tools/UI Quality of Life/Setup Demo Scene", priority = 80)]
        public static void BuildDemoScene()
        {
            Debug.Log("[DemoSceneBuilder] Building interactive DemoScene...");

            // First ensure demo assets are synced
            BuildDemoWebGL.SyncAllDemoAssets();

            // Open or create scene
            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);

            // Clean scene root objects except Camera & EventSystem
            var roots = scene.GetRootGameObjects();
            Camera mainCam = null;
            EventSystem eventSys = null;

            foreach (var go in roots)
            {
                if (go.GetComponent<Camera>() != null) mainCam = go.GetComponent<Camera>();
                else if (go.GetComponent<EventSystem>() != null) eventSys = go.GetComponent<EventSystem>();
                else UnityEngine.Object.DestroyImmediate(go);
            }

            // Setup Camera
            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera");
                mainCam = camGo.AddComponent<Camera>();
            }
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f); // #0F141C

            // Setup EventSystem
            if (eventSys == null)
            {
                var esGo = new GameObject("EventSystem");
                eventSys = esGo.AddComponent<EventSystem>();
                var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    esGo.AddComponent(inputModuleType);
                }
                else
                {
                    esGo.AddComponent<StandaloneInputModule>();
                }
            }

            // Create Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Root Hub
            var hubGo = new GameObject("DemoHub");
            hubGo.transform.SetParent(canvasGo.transform, false);
            var hubRect = hubGo.AddComponent<RectTransform>();
            SetFullStretch(hubRect);

            var hubController = hubGo.AddComponent<UIDemoHubController>();

            // --- Background ---
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(hubGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            SetFullStretch(bgRect);
            var bgImg = bgGo.AddComponent<ProceduralImage>();
            bgImg.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            bgImg.uniformRadius = 0f;

            // --- Header Bar ---
            var headerGo = CreatePanel("HeaderBar", hubGo.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -35), new Vector2(0, 70));
            var headerImg = headerGo.AddComponent<ProceduralImage>();
            headerImg.color = new Color(0.10f, 0.13f, 0.18f, 1f);
            headerImg.uniformRadius = 0f;
            headerImg.borderWidth = 1f;
            headerImg.borderColor = new Color(1f, 1f, 1f, 0.1f);

            var titleGo = CreateText("Title", headerGo.transform, "<b>UIQualityLife</b> <color=#00E5FF>Showcase Lab</color>", 24, TextAlignmentOptions.MidlineLeft);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0, 1);
            titleRect.pivot = new Vector2(0, 0.5f);
            titleRect.anchoredPosition = new Vector2(25, 0);
            titleRect.sizeDelta = new Vector2(350, 60);

            // Nav Tabs Container
            var navGo = new GameObject("NavContainer");
            navGo.transform.SetParent(headerGo.transform, false);
            var navRect = navGo.AddComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0.35f, 0f);
            navRect.anchorMax = new Vector2(0.75f, 1f);
            navRect.offsetMin = Vector2.zero;
            navRect.offsetMax = Vector2.zero;
            var navLayout = navGo.AddComponent<HorizontalLayoutGroup>();
            navLayout.childControlWidth = true;
            navLayout.childControlHeight = true;
            navLayout.childForceExpandWidth = true;
            navLayout.childForceExpandHeight = false;
            navLayout.spacing = 10f;
            navLayout.padding = new RectOffset(10, 10, 12, 12);

            var btnPresets = CreateNavButton("Btn_NavPresets", navGo.transform, "Presets (38)");
            var btnThemes = CreateNavButton("Btn_NavThemes", navGo.transform, "Themes");
            var btnProcedural = CreateNavButton("Btn_NavProcedural", navGo.transform, "Shape Studio");
            var btnAnimations = CreateNavButton("Btn_NavAnimations", navGo.transform, "Animation Lab");

            // Header Right Actions
            var actionsGo = new GameObject("HeaderActions");
            actionsGo.transform.SetParent(headerGo.transform, false);
            var actRect = actionsGo.AddComponent<RectTransform>();
            actRect.anchorMin = new Vector2(1, 0);
            actRect.anchorMax = new Vector2(1, 1);
            actRect.pivot = new Vector2(1, 0.5f);
            actRect.anchoredPosition = new Vector2(-20, 0);
            actRect.sizeDelta = new Vector2(240, 50);
            var actLayout = actionsGo.AddComponent<HorizontalLayoutGroup>();
            actLayout.childControlWidth = false;
            actLayout.childControlHeight = false;
            actLayout.spacing = 10f;
            actLayout.childAlignment = TextAnchor.MiddleRight;

            var btnFullscreen = CreateActionButton("Btn_Fullscreen", actionsGo.transform, "Fullscreen", 110);
            var btnGithub = CreateActionButton("Btn_Github", actionsGo.transform, "GitHub", 100);

            // --- Main Content Area ---
            var contentGo = CreatePanel("MainContent", hubGo.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(0, -120));

            // Tab 1: Presets Showcase
            var tabPresets = CreatePanel("Tab_Presets", contentGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetupPresetsTab(tabPresets);

            // Tab 2: Theme Switcher
            var tabThemes = CreatePanel("Tab_Themes", contentGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetupThemesTab(tabThemes);

            // Tab 3: Procedural Studio
            var tabProcedural = CreatePanel("Tab_Procedural", contentGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetupProceduralTab(tabProcedural);

            // Tab 4: Animation Lab
            var tabAnimations = CreatePanel("Tab_Animations", contentGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            SetupAnimationsTab(tabAnimations);

            // --- Bottom Status Bar ---
            var statusGo = CreatePanel("StatusBar", hubGo.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 18), new Vector2(0, 36));
            var statusImg = statusGo.AddComponent<ProceduralImage>();
            statusImg.color = new Color(0.08f, 0.10f, 0.14f, 1f);
            statusImg.borderWidth = 1f;
            statusImg.borderColor = new Color(1f, 1f, 1f, 0.08f);

            var statusLayout = statusGo.AddComponent<HorizontalLayoutGroup>();
            statusLayout.padding = new RectOffset(25, 25, 4, 4);
            statusLayout.childControlWidth = false;
            statusLayout.childControlHeight = true;
            statusLayout.spacing = 30f;

            var fpsText = CreateText("FpsText", statusGo.transform, "60 FPS", 14, TextAlignmentOptions.MidlineLeft).GetComponent<TMP_Text>();
            fpsText.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 28);

            var resText = CreateText("ResText", statusGo.transform, "1920 × 1080", 14, TextAlignmentOptions.MidlineLeft).GetComponent<TMP_Text>();
            resText.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 28);

            var verText = CreateText("VerText", statusGo.transform, "UIQualityLife Unity 6 WebGL Ready", 14, TextAlignmentOptions.MidlineRight).GetComponent<TMP_Text>();
            verText.color = new Color(1f, 1f, 1f, 0.5f);
            verText.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 28);

            // Wire Hub Controller via reflection
            SetField(hubController, "m_PresetsTabPanel", tabPresets);
            SetField(hubController, "m_ThemesTabPanel", tabThemes);
            SetField(hubController, "m_ProceduralTabPanel", tabProcedural);
            SetField(hubController, "m_AnimationsTabPanel", tabAnimations);
            SetField(hubController, "m_PresetsNavButton", btnPresets.GetComponent<Button>());
            SetField(hubController, "m_ThemesNavButton", btnThemes.GetComponent<Button>());
            SetField(hubController, "m_ProceduralNavButton", btnProcedural.GetComponent<Button>());
            SetField(hubController, "m_AnimationsNavButton", btnAnimations.GetComponent<Button>());
            SetField(hubController, "m_FullscreenButton", btnFullscreen.GetComponent<Button>());
            SetField(hubController, "m_GithubButton", btnGithub.GetComponent<Button>());
            SetField(hubController, "m_FpsText", fpsText);
            SetField(hubController, "m_ResolutionText", resText);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DemoSceneBuilder] Interactive DemoScene successfully built and saved!");
        }

        private static void SetupPresetsTab(GameObject tab)
        {
            var gallery = tab.AddComponent<UIDemoPresetGallery>();

            // Header text
            var title = CreateText("PresetTitle", tab.transform, "<b>UI Component Presets</b> (Buttons, Inputs, Cards, HUD, Modals, Badges)", 20, TextAlignmentOptions.TopLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.anchoredPosition = new Vector2(25, -20);
            titleRect.sizeDelta = new Vector2(-50, 30);

            // Filter Buttons container
            var filtersGo = new GameObject("FilterContainer");
            filtersGo.transform.SetParent(tab.transform, false);
            var filtersRect = filtersGo.AddComponent<RectTransform>();
            filtersRect.anchorMin = new Vector2(0, 1);
            filtersRect.anchorMax = new Vector2(1, 1);
            filtersRect.anchoredPosition = new Vector2(25, -60);
            filtersRect.sizeDelta = new Vector2(-50, 40);
            var fLayout = filtersGo.AddComponent<HorizontalLayoutGroup>();
            fLayout.childControlWidth = false;
            fLayout.childControlHeight = true;
            fLayout.spacing = 8f;

            // Template filter button
            var filterBtnTemplate = CreateButton("FilterBtnTemplate", null, "Category", 110, 36);
            filterBtnTemplate.SetActive(false);
            filterBtnTemplate.transform.SetParent(tab.transform, false);

            // Scroll View for Preset Cards
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(tab.transform, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(25, 20);
            scrollRect.offsetMax = new Vector2(-25, -115);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            SetFullStretch(vpRect);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.white;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRect;

            var gridGo = new GameObject("GridContent");
            gridGo.transform.SetParent(viewport.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 1);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.sizeDelta = new Vector2(0, 1200);

            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(350, 220);
            grid.spacing = new Vector2(16, 16);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.childAlignment = TextAnchor.UpperLeft;

            var csf = gridGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = gridRect;

            // Template Preset Card
            var cardTemplate = CreatePresetCardTemplate(tab.transform);
            cardTemplate.SetActive(false);

            SetField(gallery, "m_CardContainer", gridGo.transform);
            SetField(gallery, "m_FilterContainer", filtersGo.transform);
            SetField(gallery, "m_PresetCardPrefab", cardTemplate);
            SetField(gallery, "m_FilterButtonPrefab", filterBtnTemplate);
        }

        private static void SetupThemesTab(GameObject tab)
        {
            var themeTester = tab.AddComponent<UIDemoThemeTester>();

            var title = CreateText("ThemeTitle", tab.transform, "<b>Theme Palettes & Token Swatches</b>", 20, TextAlignmentOptions.TopLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.anchoredPosition = new Vector2(25, -20);
            titleRect.sizeDelta = new Vector2(-50, 30);

            // Active theme label
            var activeNameText = CreateText("ActiveThemeText", tab.transform, "Active Theme: <b>Modern Dark</b>", 18, TextAlignmentOptions.TopLeft).GetComponent<TMP_Text>();
            var nameRect = activeNameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.anchoredPosition = new Vector2(25, -55);
            nameRect.sizeDelta = new Vector2(-50, 30);

            // Quick theme switch buttons
            var buttonsRow = new GameObject("ThemeButtons");
            buttonsRow.transform.SetParent(tab.transform, false);
            var rowRect = buttonsRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.anchoredPosition = new Vector2(25, -95);
            rowRect.sizeDelta = new Vector2(-50, 42);
            var rowLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.spacing = 12f;

            var btnModernDark = CreateButton("Btn_Dark", buttonsRow.transform, "Modern Dark", 150, 40);
            btnModernDark.GetComponent<Button>().onClick.AddListener(themeTester.SelectModernDark);

            var btnCyber = CreateButton("Btn_Cyber", buttonsRow.transform, "Cyber Neon", 150, 40);
            btnCyber.GetComponent<Button>().onClick.AddListener(themeTester.SelectCyberNeon);

            var btnGameDark = CreateButton("Btn_GameDark", buttonsRow.transform, "Game Modern Dark", 170, 40);
            btnGameDark.GetComponent<Button>().onClick.AddListener(themeTester.SelectGameModernDark);

            // Swatch Grid
            var swatchScroll = new GameObject("SwatchScrollView");
            swatchScroll.transform.SetParent(tab.transform, false);
            var swRect = swatchScroll.AddComponent<RectTransform>();
            swRect.anchorMin = new Vector2(0, 0);
            swRect.anchorMax = new Vector2(1, 1);
            swRect.offsetMin = new Vector2(25, 20);
            swRect.offsetMax = new Vector2(-25, -150);

            var scroll = swatchScroll.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(swatchScroll.transform, false);
            var vpRect = vp.AddComponent<RectTransform>();
            SetFullStretch(vpRect);
            vp.AddComponent<Image>().color = Color.white;
            vp.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRect;

            var gridGo = new GameObject("SwatchGrid");
            gridGo.transform.SetParent(vp.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 1);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.pivot = new Vector2(0.5f, 1f);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180, 80);
            grid.spacing = new Vector2(12, 12);
            var csf = gridGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = gridRect;

            // Template Swatch Item
            var swatchTemplate = CreateSwatchTemplate(tab.transform);
            swatchTemplate.SetActive(false);

            SetField(themeTester, "m_ActiveThemeNameText", activeNameText);
            SetField(themeTester, "m_SwatchContainer", gridGo.transform);
            SetField(themeTester, "m_SwatchPrefab", swatchTemplate);
        }

        private static void SetupProceduralTab(GameObject tab)
        {
            var procTester = tab.AddComponent<UIDemoProceduralTester>();

            var title = CreateText("ProcTitle", tab.transform, "<b>Procedural Shape Studio</b> (SDF Borders, 4-Corner Radii, Drop Shadows)", 20, TextAlignmentOptions.TopLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.anchoredPosition = new Vector2(25, -20);
            titleRect.sizeDelta = new Vector2(-50, 30);

            // Preview Box Container (Left side)
            var previewBox = CreatePanel("PreviewCard", tab.transform, new Vector2(0, 0), new Vector2(0.45f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var pbRect = previewBox.GetComponent<RectTransform>();
            pbRect.offsetMin = new Vector2(25, 20);
            pbRect.offsetMax = new Vector2(-15, -60);
            var pbImg = previewBox.AddComponent<ProceduralImage>();
            pbImg.color = new Color(0.12f, 0.15f, 0.20f, 1f);
            pbImg.uniformRadius = 12f;
            pbImg.borderWidth = 1f;
            pbImg.borderColor = new Color(1f, 1f, 1f, 0.15f);

            // Target Image in center of preview card
            var targetGo = new GameObject("TargetProceduralImage");
            targetGo.transform.SetParent(previewBox.transform, false);
            var tRect = targetGo.AddComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(260, 260);
            var targetImg = targetGo.AddComponent<ProceduralImage>();
            targetImg.color = new Color(0.15f, 0.55f, 0.95f, 1f);
            targetImg.uniformRadius = 24f;
            targetImg.borderWidth = 3f;
            targetImg.borderColor = new Color(1f, 1f, 1f, 0.8f);
            targetImg.dropShadowSoftness = 12f;
            targetImg.dropShadowOffset = new Vector2(0, -6);

            // Controls Container (Right side)
            var controlsBox = CreatePanel("ControlsPanel", tab.transform, new Vector2(0.45f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var cbRect = controlsBox.GetComponent<RectTransform>();
            cbRect.offsetMin = new Vector2(15, 20);
            cbRect.offsetMax = new Vector2(-25, -60);

            var cLayout = controlsBox.AddComponent<VerticalLayoutGroup>();
            cLayout.padding = new RectOffset(20, 20, 20, 20);
            cLayout.spacing = 16f;
            cLayout.childControlWidth = true;
            cLayout.childControlHeight = false;

            // Sliders
            var radiusSlider = CreateSliderControl("RadiusSlider", controlsBox.transform, "Corner Radius", 0, 100, 24, out var radiusLabel);
            var borderSlider = CreateSliderControl("BorderSlider", controlsBox.transform, "Border Width", 0, 20, 3, out var borderLabel);
            var shadowSlider = CreateSliderControl("ShadowSlider", controlsBox.transform, "Drop Shadow", 0, 30, 12, out var shadowLabel);
            var angleSlider = CreateSliderControl("AngleSlider", controlsBox.transform, "Gradient Angle", 0, 360, 45, out var angleLabel);

            // Code snippet label
            var codeLabel = CreateText("CodeSnippet", controlsBox.transform, "// Live Code Preview", 14, TextAlignmentOptions.TopLeft).GetComponent<TMP_Text>();
            codeLabel.color = new Color(0.4f, 0.9f, 0.7f, 1f);
            codeLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 100);

            SetField(procTester, "m_TargetImage", targetImg);
            SetField(procTester, "m_RadiusSlider", radiusSlider);
            SetField(procTester, "m_BorderWidthSlider", borderSlider);
            SetField(procTester, "m_ShadowSoftnessSlider", shadowSlider);
            SetField(procTester, "m_GradientAngleSlider", angleSlider);
            SetField(procTester, "m_RadiusLabel", radiusLabel);
            SetField(procTester, "m_BorderWidthLabel", borderLabel);
            SetField(procTester, "m_ShadowLabel", shadowLabel);
            SetField(procTester, "m_AngleLabel", angleLabel);
            SetField(procTester, "m_CodeSnippetLabel", codeLabel);
        }

        private static void SetupAnimationsTab(GameObject tab)
        {
            var animTester = tab.AddComponent<UIDemoAnimationTester>();

            var title = CreateText("AnimTitle", tab.transform, "<b>Atomic Motion Lab</b> (Multi-Channel Easing & Physics)", 20, TextAlignmentOptions.TopLeft);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.anchoredPosition = new Vector2(25, -20);
            titleRect.sizeDelta = new Vector2(-50, 30);

            // Preview Box
            var previewBox = CreatePanel("PreviewCard", tab.transform, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var pbRect = previewBox.GetComponent<RectTransform>();
            pbRect.offsetMin = new Vector2(25, 20);
            pbRect.offsetMax = new Vector2(-15, -60);
            var pbImg = previewBox.AddComponent<ProceduralImage>();
            pbImg.color = new Color(0.12f, 0.15f, 0.20f, 1f);
            pbImg.uniformRadius = 12f;

            // Animated interactive target card
            var animTargetGo = new GameObject("AnimatedTarget");
            animTargetGo.transform.SetParent(previewBox.transform, false);
            var atRect = animTargetGo.AddComponent<RectTransform>();
            atRect.sizeDelta = new Vector2(280, 180);
            var atImg = animTargetGo.AddComponent<ProceduralImage>();
            atImg.color = new Color(0.20f, 0.45f, 0.85f, 1f);
            atImg.uniformRadius = 16f;
            atImg.borderWidth = 2f;
            atImg.borderColor = new Color(1f, 1f, 1f, 0.3f);
            var animator = animTargetGo.AddComponent<UIClassAnimator>();

            var cardText = CreateText("CardLabel", animTargetGo.transform, "<b>Hover / Click Me</b>\n<size=14>Atomic Motion Target</size>", 18, TextAlignmentOptions.Center);
            var ctRect = cardText.GetComponent<RectTransform>();
            SetFullStretch(ctRect);

            // Controls on the right
            var controlsBox = CreatePanel("ControlsPanel", tab.transform, new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var cbRect = controlsBox.GetComponent<RectTransform>();
            cbRect.offsetMin = new Vector2(15, 20);
            cbRect.offsetMax = new Vector2(-25, -60);

            var cLayout = controlsBox.AddComponent<VerticalLayoutGroup>();
            cLayout.padding = new RectOffset(20, 20, 20, 20);
            cLayout.spacing = 16f;
            cLayout.childControlWidth = true;
            cLayout.childControlHeight = false;

            var profileTitle = CreateText("ProfileTitle", controlsBox.transform, "Profile: <b>ButtonBounce</b>", 18, TextAlignmentOptions.TopLeft).GetComponent<TMP_Text>();
            profileTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            var profileInfo = CreateText("ProfileInfo", controlsBox.transform, "Select an animation profile to test.", 14, TextAlignmentOptions.TopLeft).GetComponent<TMP_Text>();
            profileInfo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);

            // Action buttons
            var btnPunch = CreateButton("Btn_Punch", controlsBox.transform, "Trigger Click Bounce", 200, 42);
            var btnReset = CreateButton("Btn_Reset", controlsBox.transform, "Reset To Rest", 200, 42);

            SetField(animTester, "m_TargetAnimator", animator);
            SetField(animTester, "m_PunchButton", btnPunch.GetComponent<Button>());
            SetField(animTester, "m_ResetButton", btnReset.GetComponent<Button>());
            SetField(animTester, "m_AnimationTitleText", profileTitle);
            SetField(animTester, "m_AnimationInfoText", profileInfo);
        }

        #region Helper Factories

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private static GameObject CreateText(string name, Transform parent, string content, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, float width, float height)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var img = go.AddComponent<ProceduralImage>();
            img.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            img.uniformRadius = 8f;
            img.borderWidth = 1f;
            img.borderColor = new Color(1f, 1f, 1f, 0.15f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.24f, 0.29f, 0.38f, 1f);
            colors.pressedColor = new Color(0.14f, 0.17f, 0.22f, 1f);
            btn.colors = colors;

            var txt = CreateText("Label", go.transform, label, 14, TextAlignmentOptions.Center);
            SetFullStretch(txt.GetComponent<RectTransform>());

            return go;
        }

        private static GameObject CreateNavButton(string name, Transform parent, string label)
        {
            var btn = CreateButton(name, parent, label, 140, 42);
            var img = btn.GetComponent<ProceduralImage>();
            img.color = new Color(0.14f, 0.17f, 0.24f, 1f);
            img.uniformRadius = 8f;
            return btn;
        }

        private static GameObject CreateActionButton(string name, Transform parent, string label, float width)
        {
            var btn = CreateButton(name, parent, label, width, 36);
            var img = btn.GetComponent<ProceduralImage>();
            img.color = new Color(0.12f, 0.35f, 0.75f, 1f);
            img.uniformRadius = 6f;
            return btn;
        }

        private static Slider CreateSliderControl(string name, Transform parent, string labelText, float min, float max, float defVal, out TMP_Text valLabel)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);

            var lbl = CreateText("Label", row.transform, labelText, 14, TextAlignmentOptions.MidlineLeft).GetComponent<TMP_Text>();
            var lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0.5f);
            lblRt.anchorMax = new Vector2(0.4f, 0.5f);
            lblRt.sizeDelta = new Vector2(0, 30);

            valLabel = CreateText("Val", row.transform, $"{defVal:F0}", 14, TextAlignmentOptions.MidlineRight).GetComponent<TMP_Text>();
            var valRt = valLabel.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.85f, 0.5f);
            valRt.anchorMax = new Vector2(1, 0.5f);
            valRt.sizeDelta = new Vector2(0, 30);

            // Slider
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);
            var sRt = sliderGo.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.42f, 0.5f);
            sRt.anchorMax = new Vector2(0.83f, 0.5f);
            sRt.sizeDelta = new Vector2(0, 20);

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defVal;

            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderGo.transform, false);
            SetFullStretch(bg.AddComponent<RectTransform>());
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.25f, 0.35f, 1f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            SetFullStretch(faRt);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.AddComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.00f, 0.85f, 1.00f, 1f);
            slider.fillRect = fRt;

            return slider;
        }

        private static GameObject CreatePresetCardTemplate(Transform parent)
        {
            var card = new GameObject("CardTemplate");
            if (parent != null) card.transform.SetParent(parent, false);
            var rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(350, 220);

            var img = card.AddComponent<ProceduralImage>();
            img.color = new Color(0.11f, 0.14f, 0.19f, 1f);
            img.uniformRadius = 10f;
            img.borderWidth = 1f;
            img.borderColor = new Color(1f, 1f, 1f, 0.12f);

            var title = CreateText("Title", card.transform, "Component Name", 16, TextAlignmentOptions.TopLeft);
            var tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(0.7f, 1);
            tRt.anchoredPosition = new Vector2(16, -14);
            tRt.sizeDelta = new Vector2(0, 24);

            var cat = CreateText("Category", card.transform, "Buttons", 12, TextAlignmentOptions.TopRight);
            var cRt = cat.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.7f, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.anchoredPosition = new Vector2(-16, -14);
            cRt.sizeDelta = new Vector2(0, 24);
            cat.GetComponent<TMP_Text>().color = new Color(0f, 0.9f, 1f, 0.8f);

            var preview = new GameObject("PreviewArea");
            preview.transform.SetParent(card.transform, false);
            var pRt = preview.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 0);
            pRt.anchorMax = new Vector2(1, 1);
            pRt.offsetMin = new Vector2(16, 16);
            pRt.offsetMax = new Vector2(-16, -45);

            return card;
        }

        private static GameObject CreateSwatchTemplate(Transform parent)
        {
            var swatch = new GameObject("SwatchTemplate");
            if (parent != null) swatch.transform.SetParent(parent, false);
            var rt = swatch.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 80);

            var pImg = swatch.AddComponent<ProceduralImage>();
            pImg.color = new Color(0.12f, 0.15f, 0.20f, 1f);
            pImg.uniformRadius = 8f;
            pImg.borderWidth = 1f;
            pImg.borderColor = new Color(1f, 1f, 1f, 0.1f);

            var colorBlock = new GameObject("ColorBlock");
            colorBlock.transform.SetParent(swatch.transform, false);
            var cbRt = colorBlock.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0, 0);
            cbRt.anchorMax = new Vector2(0, 1);
            cbRt.anchoredPosition = new Vector2(25, 0);
            cbRt.sizeDelta = new Vector2(36, -20);
            var cbImg = colorBlock.AddComponent<Image>();
            cbImg.color = Color.white;

            var nameText = CreateText("TokenName", swatch.transform, "--primary", 13, TextAlignmentOptions.MidlineLeft);
            var ntRt = nameText.GetComponent<RectTransform>();
            ntRt.anchorMin = new Vector2(0, 0.5f);
            ntRt.anchorMax = new Vector2(1, 1);
            ntRt.anchoredPosition = new Vector2(55, 0);
            ntRt.sizeDelta = new Vector2(-60, 0);

            var hexText = CreateText("Hex", swatch.transform, "#00E5FF", 11, TextAlignmentOptions.MidlineLeft);
            var htRt = hexText.GetComponent<RectTransform>();
            htRt.anchorMin = new Vector2(0, 0);
            htRt.anchorMax = new Vector2(1, 0.5f);
            htRt.anchoredPosition = new Vector2(55, 0);
            htRt.sizeDelta = new Vector2(-60, 0);
            hexText.GetComponent<TMP_Text>().color = new Color(1f, 1f, 1f, 0.6f);

            return swatch;
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        #endregion
    }
}
