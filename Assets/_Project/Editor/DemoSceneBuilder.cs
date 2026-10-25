using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UIQualityLife.ProceduralUI;
using UIQualityLife.Styling;
using UIQualityLife.Runtime.Demo;
using UIQualityLife.Runtime.Demo.Studio;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Editor utility constructing the 3-Panel Runtime UI Studio in DemoScene.unity:
    /// - Left Panel: Runtime Inspector & Property Injector
    /// - Middle Panel: Interactive Canvas Viewport & Selection Gizmo
    /// - Right Panel: Component Library (38 presets), Styling & CSS Studio, Code Export
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string DemoScenePath = "Assets/Scenes/DemoScene.unity";

        [MenuItem("Tools/UI Quality of Life/Setup Demo Scene", priority = 80)]
        public static void BuildDemoScene()
        {
            Debug.Log("[DemoSceneBuilder] Constructing 3-Panel Runtime UI Studio...");

            // Ensure demo assets are synced
            BuildDemoWebGL.SyncAllDemoAssets();

            // Open scene
            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);

            // Clean existing scene root objects except Camera & EventSystem
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
            mainCam.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 1f);

            // Setup EventSystem
            if (eventSys == null)
            {
                var esGo = new GameObject("EventSystem");
                eventSys = esGo.AddComponent<EventSystem>();
            }
            var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null && eventSys.GetComponent(inputModuleType) == null)
            {
                eventSys.gameObject.AddComponent(inputModuleType);
            }
            else if (eventSys.GetComponent<StandaloneInputModule>() == null && inputModuleType == null)
            {
                eventSys.gameObject.AddComponent<StandaloneInputModule>();
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

            // Root Studio Hub
            var hubGo = new GameObject("DemoStudioHub");
            hubGo.transform.SetParent(canvasGo.transform, false);
            var hubRect = hubGo.AddComponent<RectTransform>();
            SetFullStretch(hubRect);
            var hubController = hubGo.AddComponent<UIDemoStudioHub>();

            // Fullscreen Dark Background
            var bgGo = CreatePanel("StudioBackground", hubGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var bgImg = bgGo.AddComponent<ProceduralImage>();
            bgImg.color = new Color(0.06f, 0.08f, 0.11f, 1f);
            bgImg.uniformRadius = 0f;

            // ==========================================
            // 1. TOP HEADER BAR (Height 60px)
            // ==========================================
            var headerGo = CreatePanel("HeaderBar", hubGo.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(0, 60));
            var headerImg = headerGo.AddComponent<ProceduralImage>();
            headerImg.color = new Color(0.09f, 0.11f, 0.16f, 1f);
            headerImg.uniformRadius = 0f;
            headerImg.borderWidth = 1f;
            headerImg.borderColor = new Color(1f, 1f, 1f, 0.1f);

            // Left Title
            var titleGo = CreateText("StudioTitle", headerGo.transform, "<b>UIQualityLife</b> <color=#00E5FF>Studio</color>", 20, TextAlignmentOptions.MidlineLeft);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(0, 1);
            titleRect.pivot = new Vector2(0, 0.5f);
            titleRect.anchoredPosition = new Vector2(20, 0);
            titleRect.sizeDelta = new Vector2(260, 0);

            // Center: Mode Buttons [Interact ⚡] vs [Inspect 🔍]
            var centerRow = new GameObject("ModeControls");
            centerRow.transform.SetParent(headerGo.transform, false);
            var cRowRt = centerRow.AddComponent<RectTransform>();
            cRowRt.anchorMin = new Vector2(0.5f, 0);
            cRowRt.anchorMax = new Vector2(0.5f, 1);
            cRowRt.pivot = new Vector2(0.5f, 0.5f);
            cRowRt.sizeDelta = new Vector2(400, 0);
            var cLayout = centerRow.AddComponent<HorizontalLayoutGroup>();
            cLayout.childControlWidth = false;
            cLayout.childControlHeight = false;
            cLayout.childAlignment = TextAnchor.MiddleCenter;
            cLayout.spacing = 12f;

            var btnInteract = CreateButton("Btn_Interact", centerRow.transform, "Interact", 100, 36, new Color(0.12f, 0.40f, 0.30f, 1f));
            var btnInspect = CreateButton("Btn_Inspect", centerRow.transform, "Inspect", 100, 36, new Color(0.15f, 0.18f, 0.24f, 1f));
            var modeLabel = CreateText("ModeLabel", centerRow.transform, "Mode: <b><color=#00FF88>Interact</color></b>", 13, TextAlignmentOptions.MidlineLeft).GetComponent<TMP_Text>();
            modeLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 36);

            // Right Header Actions
            var actRow = new GameObject("HeaderActions");
            actRow.transform.SetParent(headerGo.transform, false);
            var actRt = actRow.AddComponent<RectTransform>();
            actRt.anchorMin = new Vector2(1, 0);
            actRt.anchorMax = new Vector2(1, 1);
            actRt.pivot = new Vector2(1, 0.5f);
            actRt.anchoredPosition = new Vector2(-15, 0);
            actRt.sizeDelta = new Vector2(360, 0);
            var actLayout = actRow.AddComponent<HorizontalLayoutGroup>();
            actLayout.childControlWidth = false;
            actLayout.childControlHeight = false;
            actLayout.childAlignment = TextAnchor.MiddleRight;
            actLayout.spacing = 8f;

            var btnClear = CreateButton("Btn_Clear", actRow.transform, "Clear", 70, 34, new Color(0.20f, 0.24f, 0.32f, 1f));
            var btnReset = CreateButton("Btn_Reset", actRow.transform, "Reset Demo", 95, 34, new Color(0.18f, 0.35f, 0.65f, 1f));
            var btnFull = CreateButton("Btn_Full", actRow.transform, "Fullscreen", 85, 34, new Color(0.20f, 0.24f, 0.32f, 1f));
            var btnGit = CreateButton("Btn_Git", actRow.transform, "GitHub", 75, 34, new Color(0.15f, 0.18f, 0.24f, 1f));

            // ==========================================
            // 2. MAIN 3-PANEL BODY (Stretch between Header and Status Bar)
            // ==========================================
            var bodyGo = CreatePanel("StudioBody", hubGo.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(0, -88));

            // --- LEFT PANEL (Inspector, Width 340px) ---
            var leftGo = CreatePanel("Left_InspectorPanel", bodyGo.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(340, 0));
            var inspector = SetupLeftInspector(leftGo);

            // --- RIGHT PANEL (Library & CSS Studio, Width 420px) ---
            var rightGo = CreatePanel("Right_ShelfPanel", bodyGo.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-210, 0), new Vector2(420, 0));

            // --- MIDDLE PANEL (Canvas Viewport, Flexible Stretch) ---
            var midGo = CreatePanel("Middle_CanvasViewport", bodyGo.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var midRt = midGo.GetComponent<RectTransform>();
            midRt.offsetMin = new Vector2(345, 0);
            midRt.offsetMax = new Vector2(-425, 0);
            var viewport = SetupMiddleCanvas(midGo);

            // Setup Right Shelf (wired to viewport)
            var shelf = SetupRightShelf(rightGo, viewport);

            // ==========================================
            // 3. BOTTOM STATUS BAR (Height 28px)
            // ==========================================
            var statusGo = CreatePanel("StatusBar", hubGo.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(0, 28));
            var statusImg = statusGo.AddComponent<ProceduralImage>();
            statusImg.color = new Color(0.08f, 0.10f, 0.14f, 1f);
            statusImg.borderWidth = 1f;
            statusImg.borderColor = new Color(1f, 1f, 1f, 0.08f);

            var sLayout = statusGo.AddComponent<HorizontalLayoutGroup>();
            sLayout.padding = new RectOffset(20, 20, 2, 2);
            sLayout.childControlWidth = false;
            sLayout.childControlHeight = true;
            sLayout.spacing = 25f;

            var fpsTxt = CreateText("FpsText", statusGo.transform, "60 FPS", 12, TextAlignmentOptions.MidlineLeft).GetComponent<TMP_Text>();
            fpsTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 24);

            var resTxt = CreateText("ResText", statusGo.transform, "1920 × 1080", 12, TextAlignmentOptions.MidlineLeft).GetComponent<TMP_Text>();
            resTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 24);

            var infoTxt = CreateText("InfoText", statusGo.transform, "UIQualityLife 3-Panel Studio • Unity 6 WebGL Ready", 12, TextAlignmentOptions.MidlineRight).GetComponent<TMP_Text>();
            infoTxt.color = new Color(1f, 1f, 1f, 0.5f);
            infoTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 24);

            // Wire Master Studio Hub
            SetField(hubController, "m_Inspector", inspector);
            SetField(hubController, "m_CanvasViewport", viewport);
            SetField(hubController, "m_ShelfPanel", shelf);
            SetField(hubController, "m_InteractModeBtn", btnInteract.GetComponent<Button>());
            SetField(hubController, "m_InspectModeBtn", btnInspect.GetComponent<Button>());
            SetField(hubController, "m_ModeLabel", modeLabel);
            SetField(hubController, "m_ClearCanvasBtn", btnClear.GetComponent<Button>());
            SetField(hubController, "m_ResetLayoutBtn", btnReset.GetComponent<Button>());
            SetField(hubController, "m_FullscreenBtn", btnFull.GetComponent<Button>());
            SetField(hubController, "m_GithubBtn", btnGit.GetComponent<Button>());
            SetField(hubController, "m_FpsText", fpsTxt);
            SetField(hubController, "m_ResolutionText", resTxt);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DemoSceneBuilder] 3-Panel Runtime UI Studio constructed and saved successfully!");
        }

        private static UIRuntimeInspector SetupLeftInspector(GameObject parent)
        {
            var pImg = parent.AddComponent<ProceduralImage>();
            pImg.color = new Color(0.09f, 0.11f, 0.16f, 1f);
            pImg.uniformRadius = 0f;
            pImg.borderWidth = 1f;
            pImg.borderColor = new Color(1f, 1f, 1f, 0.10f);

            var inspector = parent.AddComponent<UIRuntimeInspector>();

            // Panel Header
            var pHeader = CreatePanel("InspectorHeader", parent.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(0, 60));
            var title = CreateText("SelectedTitle", pHeader.transform, "Canvas Viewport", 16, TextAlignmentOptions.TopLeft).GetComponent<TMP_Text>();
            var tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0.5f);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.offsetMin = new Vector2(16, 0);
            tRt.offsetMax = new Vector2(-16, -6);

            var sub = CreateText("SelectedSubtitle", pHeader.transform, "Click any component on canvas to inspect", 11, TextAlignmentOptions.TopLeft).GetComponent<TMP_Text>();
            var sRt = sub.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0);
            sRt.anchorMax = new Vector2(1, 0.5f);
            sRt.offsetMin = new Vector2(16, 4);
            sRt.offsetMax = new Vector2(-16, 0);
            sub.color = new Color(0.5f, 0.8f, 1f, 0.8f);

            // Add Property Dropdown Container
            var addPropGo = CreatePanel("AddPropertyContainer", parent.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -75), new Vector2(-24, 34));
            var addImg = addPropGo.AddComponent<ProceduralImage>();
            addImg.color = new Color(0.14f, 0.18f, 0.26f, 1f);
            addImg.uniformRadius = 6f;
            addImg.borderWidth = 1f;
            addImg.borderColor = new Color(0f, 0.85f, 1f, 0.3f);

            var dropdown = addPropGo.AddComponent<TMP_Dropdown>();
            var ddLabel = CreateText("Label", addPropGo.transform, "+ Add Property / Modifier...", 12, TextAlignmentOptions.MidlineLeft).GetComponent<TMP_Text>();
            SetFullStretch(ddLabel.GetComponent<RectTransform>());
            ddLabel.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0);
            ddLabel.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);
            dropdown.captionText = ddLabel;

            // Scroll View for Dynamic Fields
            var scrollGo = CreatePanel("FieldsScrollView", parent.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var scRt = scrollGo.GetComponent<RectTransform>();
            scRt.offsetMin = new Vector2(12, 10);
            scRt.offsetMax = new Vector2(-12, -100);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGo.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            SetFullStretch(vpRt);
            vp.AddComponent<Image>().color = Color.white;
            vp.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            var contentGo = new GameObject("FieldsContainer");
            contentGo.transform.SetParent(vp.transform, false);
            var cRt = contentGo.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0, 800);

            var vLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(4, 4, 8, 8);
            vLayout.spacing = 10f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRt;

            SetField(inspector, "m_FieldsContainer", contentGo.transform);
            SetField(inspector, "m_SelectedTitleText", title);
            SetField(inspector, "m_SelectedSubtitleText", sub);
            SetField(inspector, "m_AddPropertyDropdown", dropdown);

            return inspector;
        }

        private static UIDemoCanvasViewport SetupMiddleCanvas(GameObject parent)
        {
            var pImg = parent.AddComponent<ProceduralImage>();
            pImg.color = new Color(0.05f, 0.06f, 0.09f, 1f);
            pImg.uniformRadius = 0f;

            var viewport = parent.AddComponent<UIDemoCanvasViewport>();

            // Viewport Top Toolbar Label
            var barGo = CreatePanel("ViewportBar", parent.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(0, 36));
            var barText = CreateText("Label", barGo.transform, "<b>CANVAS VIEWPORT</b> <size=11><color=#88A0B8>(Hold Alt or Tab to toggle Inspect Mode)</color></size>", 12, TextAlignmentOptions.Center);
            SetFullStretch(barText.GetComponent<RectTransform>());

            // Scroll View for Centered Canvas Artboard
            var scrollGo = CreatePanel("ArtboardScrollView", parent.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var scRt = scrollGo.GetComponent<RectTransform>();
            scRt.offsetMin = new Vector2(15, 15);
            scRt.offsetMax = new Vector2(-15, -45);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGo.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            SetFullStretch(vpRt);
            vp.AddComponent<Image>().color = Color.white;
            vp.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            // Centered Artboard Container (Cards spawn here)
            var artboardGo = new GameObject("ArtboardContainer");
            artboardGo.transform.SetParent(vp.transform, false);
            var abRt = artboardGo.AddComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0, 1);
            abRt.anchorMax = new Vector2(1, 1);
            abRt.pivot = new Vector2(0.5f, 1f);
            abRt.sizeDelta = new Vector2(0, 1200);

            var vLayout = artboardGo.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(40, 40, 30, 40);
            vLayout.spacing = 24f;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;
            vLayout.childAlignment = TextAnchor.UpperCenter;

            var csf = artboardGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = abRt;

            // Selection Gizmo (Overlay box)
            var gizmoGo = new GameObject("SelectionGizmo");
            gizmoGo.transform.SetParent(artboardGo.transform, false);
            var gRt = gizmoGo.AddComponent<RectTransform>();
            gRt.sizeDelta = new Vector2(100, 100);

            var gImg = gizmoGo.AddComponent<ProceduralImage>();
            gImg.color = new Color(0f, 0.9f, 1f, 0.05f); // Subtle fill
            gImg.uniformRadius = 4f;
            gImg.borderWidth = 2f;
            gImg.borderColor = new Color(0f, 0.95f, 1f, 0.9f); // Bright cyan outline
            gImg.raycastTarget = false;
            gizmoGo.SetActive(false);

            SetField(viewport, "m_ArtboardContainer", abRt);
            SetField(viewport, "m_SelectionGizmo", gRt);

            // Raycast Overlay (Intercepts clicks in Inspect Mode)
            var overlayGo = new GameObject("InspectRaycastOverlay");
            overlayGo.transform.SetParent(scrollGo.transform, false);
            var ovRt = overlayGo.AddComponent<RectTransform>();
            SetFullStretch(ovRt);
            var ovImg = overlayGo.AddComponent<Image>();
            ovImg.color = new Color(0, 0, 0, 0.001f); // Transparent raycast target
            ovImg.raycastTarget = true;
            var interceptor = overlayGo.AddComponent<UIRaycastInterceptor>();
            interceptor.SetViewport(viewport);
            overlayGo.SetActive(false); // Inactive by default (Interact Mode)

            SetField(viewport, "m_RaycastOverlay", ovRt);

            return viewport;
        }

        private static UIDemoShelfPanel SetupRightShelf(GameObject parent, UIDemoCanvasViewport viewport)
        {
            var pImg = parent.AddComponent<ProceduralImage>();
            pImg.color = new Color(0.09f, 0.11f, 0.16f, 1f);
            pImg.uniformRadius = 0f;
            pImg.borderWidth = 1f;
            pImg.borderColor = new Color(1f, 1f, 1f, 0.10f);

            var shelf = parent.AddComponent<UIDemoShelfPanel>();

            // Tab Navigation Buttons Row (Library, Styling, Export)
            var navRow = CreatePanel("ShelfNavRow", parent.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(-20, 36));
            var navLayout = navRow.AddComponent<HorizontalLayoutGroup>();
            navLayout.childControlWidth = true;
            navLayout.childControlHeight = true;
            navLayout.spacing = 8f;

            var btnLib = CreateButton("Btn_TabLib", navRow.transform, "Library (38)", 120, 36, new Color(0.12f, 0.45f, 0.75f, 1f));
            var btnStyle = CreateButton("Btn_TabStyle", navRow.transform, "Styling Studio", 120, 36, new Color(0.13f, 0.16f, 0.22f, 1f));
            var btnExport = CreateButton("Btn_TabExport", navRow.transform, "Code Export", 120, 36, new Color(0.13f, 0.16f, 0.22f, 1f));

            // Content Area for Tabs
            var tabArea = CreatePanel("ShelfContentArea", parent.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var taRt = tabArea.GetComponent<RectTransform>();
            taRt.offsetMin = new Vector2(12, 10);
            taRt.offsetMax = new Vector2(-12, -50);

            // 1. Tab Library
            var tabLib = CreatePanel("Tab_Library", tabArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var filterPills = CreatePanel("FilterPills", tabLib.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(0, 32));
            var fpLayout = filterPills.AddComponent<HorizontalLayoutGroup>();
            fpLayout.childControlWidth = false;
            fpLayout.childControlHeight = true;
            fpLayout.spacing = 6f;

            var searchRow = CreatePanel("SearchRow", tabLib.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -55), new Vector2(0, 32));
            var searchImg = searchRow.AddComponent<ProceduralImage>();
            searchImg.color = new Color(0.12f, 0.15f, 0.20f, 1f);
            searchImg.uniformRadius = 4f;
            searchImg.borderWidth = 1f;
            searchImg.borderColor = new Color(1f, 1f, 1f, 0.12f);
            var searchIn = searchRow.AddComponent<TMP_InputField>();
            var searchTxt = CreateText("Text", searchRow.transform, "", 12, TextAlignmentOptions.MidlineLeft);
            SetFullStretch(searchTxt.GetComponent<RectTransform>());
            searchTxt.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0);
            searchIn.textComponent = searchTxt.GetComponent<TextMeshProUGUI>();

            // Preset Cards Scroll Grid
            var libScroll = CreatePanel("LibScrollView", tabLib.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var lsRt = libScroll.GetComponent<RectTransform>();
            lsRt.offsetMin = Vector2.zero;
            lsRt.offsetMax = new Vector2(0, -80);
            var lScroll = libScroll.AddComponent<ScrollRect>();
            lScroll.horizontal = false;
            lScroll.vertical = true;
            var lVp = new GameObject("Viewport");
            lVp.transform.SetParent(libScroll.transform, false);
            var lVpRt = lVp.AddComponent<RectTransform>();
            SetFullStretch(lVpRt);
            lVp.AddComponent<Image>().color = Color.white;
            lVp.AddComponent<Mask>().showMaskGraphic = false;
            lScroll.viewport = lVpRt;

            var gridGo = new GameObject("PresetGrid");
            gridGo.transform.SetParent(lVp.transform, false);
            var gRt = gridGo.AddComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0, 1);
            gRt.anchorMax = new Vector2(1, 1);
            gRt.pivot = new Vector2(0.5f, 1f);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(185, 105);
            grid.spacing = new Vector2(10, 10);
            var gCsf = gridGo.AddComponent<ContentSizeFitter>();
            gCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            lScroll.content = gRt;

            // 2. Tab Styling
            var tabStyle = CreatePanel("Tab_Styling", tabArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tabStyle.SetActive(false);

            var activeThemeTxt = CreateText("ActiveThemeTxt", tabStyle.transform, "Active: <b><color=#00E5FF>Modern Dark</color></b>", 15, TextAlignmentOptions.TopLeft).GetComponent<TMP_Text>();
            var atRt = activeThemeTxt.GetComponent<RectTransform>();
            atRt.anchorMin = new Vector2(0, 1);
            atRt.anchorMax = new Vector2(1, 1);
            atRt.anchoredPosition = new Vector2(0, -12);
            atRt.sizeDelta = new Vector2(0, 24);

            var themeBtnRow = CreatePanel("ThemeBtns", tabStyle.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -45), new Vector2(0, 36));
            var tbLayout = themeBtnRow.AddComponent<HorizontalLayoutGroup>();
            tbLayout.childControlWidth = true;
            tbLayout.childControlHeight = true;
            tbLayout.spacing = 8f;

            var btnDark = CreateButton("Btn_Dark", themeBtnRow.transform, "Modern Dark", 110, 34, new Color(0.15f, 0.18f, 0.25f, 1f));
            var btnGame = CreateButton("Btn_Game", themeBtnRow.transform, "Game Dark", 110, 34, new Color(0.15f, 0.18f, 0.25f, 1f));
            var btnCyber = CreateButton("Btn_Cyber", themeBtnRow.transform, "Cyber Neon", 110, 34, new Color(0.15f, 0.18f, 0.25f, 1f));

            // Swatches ScrollView
            var swScroll = CreatePanel("SwatchScrollView", tabStyle.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var swRt = swScroll.GetComponent<RectTransform>();
            swRt.offsetMin = Vector2.zero;
            swRt.offsetMax = new Vector2(0, -90);
            var sScroll = swScroll.AddComponent<ScrollRect>();
            sScroll.horizontal = false;
            sScroll.vertical = true;
            var sVp = new GameObject("Viewport");
            sVp.transform.SetParent(swScroll.transform, false);
            var sVpRt = sVp.AddComponent<RectTransform>();
            SetFullStretch(sVpRt);
            sVp.AddComponent<Image>().color = Color.white;
            sVp.AddComponent<Mask>().showMaskGraphic = false;
            sScroll.viewport = sVpRt;

            var swGridGo = new GameObject("SwatchGrid");
            swGridGo.transform.SetParent(sVp.transform, false);
            var swgRt = swGridGo.AddComponent<RectTransform>();
            swgRt.anchorMin = new Vector2(0, 1);
            swgRt.anchorMax = new Vector2(1, 1);
            swgRt.pivot = new Vector2(0.5f, 1f);
            var swGrid = swGridGo.AddComponent<GridLayoutGroup>();
            swGrid.cellSize = new Vector2(185, 42);
            swGrid.spacing = new Vector2(10, 8);
            var swCsf = swGridGo.AddComponent<ContentSizeFitter>();
            swCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sScroll.content = swgRt;

            // 3. Tab Export
            var tabExport = CreatePanel("Tab_Export", tabArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tabExport.SetActive(false);

            var expTitle = CreateText("ExportTitle", tabExport.transform, "<b>Generated C# & CSS Class</b>", 15, TextAlignmentOptions.TopLeft);
            var etRt = expTitle.GetComponent<RectTransform>();
            etRt.anchorMin = new Vector2(0, 1);
            etRt.anchorMax = new Vector2(1, 1);
            etRt.anchoredPosition = new Vector2(0, -12);
            etRt.sizeDelta = new Vector2(0, 24);

            var copyBtn = CreateButton("Btn_Copy", tabExport.transform, "Copy Code to Clipboard", 220, 36, new Color(0.12f, 0.45f, 0.75f, 1f));
            var cbRt = copyBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0, 1);
            cbRt.anchorMax = new Vector2(0, 1);
            cbRt.pivot = new Vector2(0, 1);
            cbRt.anchoredPosition = new Vector2(0, -45);
            cbRt.sizeDelta = new Vector2(220, 36);

            var notifyTxt = CreateText("NotifyTxt", tabExport.transform, "", 12, TextAlignmentOptions.MidlineLeft).GetComponent<TMP_Text>();
            notifyTxt.color = new Color(0.2f, 1f, 0.5f, 1f);
            var nRt = notifyTxt.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0, 1);
            nRt.anchorMax = new Vector2(1, 1);
            nRt.anchoredPosition = new Vector2(235, -45);
            nRt.sizeDelta = new Vector2(150, 36);

            var codeBox = CreatePanel("CodeBox", tabExport.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var coRt = codeBox.GetComponent<RectTransform>();
            coRt.offsetMin = Vector2.zero;
            coRt.offsetMax = new Vector2(0, -90);
            var cbImg = codeBox.AddComponent<ProceduralImage>();
            cbImg.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            cbImg.uniformRadius = 6f;
            cbImg.borderWidth = 1f;
            cbImg.borderColor = new Color(1f, 1f, 1f, 0.12f);

            var codeTxt = CreateText("CodeText", codeBox.transform, "// Select component to view code", 12, TextAlignmentOptions.TopLeft).GetComponent<TMP_Text>();
            SetFullStretch(codeTxt.GetComponent<RectTransform>());
            codeTxt.GetComponent<RectTransform>().offsetMin = new Vector2(12, 12);
            codeTxt.GetComponent<RectTransform>().offsetMax = new Vector2(-12, -12);
            codeTxt.color = new Color(0.4f, 0.9f, 0.7f, 1f);

            // Wire Shelf Panel
            SetField(shelf, "m_CanvasViewport", viewport);
            SetField(shelf, "m_TabLibraryBtn", btnLib.GetComponent<Button>());
            SetField(shelf, "m_TabStylingBtn", btnStyle.GetComponent<Button>());
            SetField(shelf, "m_TabExportBtn", btnExport.GetComponent<Button>());
            SetField(shelf, "m_TabLibraryPanel", tabLib);
            SetField(shelf, "m_TabStylingPanel", tabStyle);
            SetField(shelf, "m_TabExportPanel", tabExport);
            SetField(shelf, "m_FilterPillContainer", filterPills.transform);
            SetField(shelf, "m_SearchInput", searchIn);
            SetField(shelf, "m_PresetGridContainer", gridGo.transform);
            SetField(shelf, "m_ActiveThemeText", activeThemeTxt);
            SetField(shelf, "m_ThemeModernDarkBtn", btnDark.GetComponent<Button>());
            SetField(shelf, "m_ThemeGameDarkBtn", btnGame.GetComponent<Button>());
            SetField(shelf, "m_ThemeCyberNeonBtn", btnCyber.GetComponent<Button>());
            SetField(shelf, "m_SwatchContainer", swGridGo.transform);
            SetField(shelf, "m_GeneratedCodeText", codeTxt);
            SetField(shelf, "m_CopyCodeBtn", copyBtn.GetComponent<Button>());
            SetField(shelf, "m_CopyNotificationText", notifyTxt);

            return shelf;
        }

        #region Factory Helpers

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

        private static GameObject CreateButton(string name, Transform parent, string label, float width, float height, Color bgColor)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var img = go.AddComponent<ProceduralImage>();
            img.color = bgColor;
            img.uniformRadius = 6f;
            img.borderWidth = 1f;
            img.borderColor = new Color(1f, 1f, 1f, 0.15f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = bgColor * 1.25f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            var txt = CreateText("Label", go.transform, label, 13, TextAlignmentOptions.Center);
            SetFullStretch(txt.GetComponent<RectTransform>());

            return go;
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
