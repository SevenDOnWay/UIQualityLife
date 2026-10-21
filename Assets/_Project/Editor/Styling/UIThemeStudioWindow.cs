#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UIQualityLife.Styling.Editor
{
    /// <summary>
    /// All-in-one CSS Studio Editor Window.
    /// Edit color palettes, design tokens, and animation classes in 1 place
    /// with instant live preview across all open scenes.
    /// </summary>
    public class UIThemeStudioWindow : EditorWindow
    {
        private enum Tab
        {
            Palette = 0,
            AnimationClasses = 1,
            StyleClasses = 2
        }

        private Tab m_CurrentTab = Tab.Palette;
        private Vector2 m_ScrollPos;

        private List<UIThemePalette> m_AllPalettes = new List<UIThemePalette>();
        private List<UIAnimationClass> m_AllAnimClasses = new List<UIAnimationClass>();
        private List<UIStyleClass> m_AllStyleClasses = new List<UIStyleClass>();

        private UnityEditor.Editor m_PaletteCachedEditor;
        private UnityEditor.Editor m_AnimCachedEditor;

        [MenuItem("Window/UI Quality of Life/CSS Theme & Animation Studio", false, 20)]
        public static void Open()
        {
            var window = GetWindow<UIThemeStudioWindow>("CSS Studio");
            window.minSize = new Vector2(400, 520);
            window.Show();
        }

        public static void GenerateStarterAssets()
        {
            EnsureAssetDirectory("Assets/_Project/Styling/Themes/UITheme_ModernDark");
            EnsureAssetDirectory("Assets/_Project/Styling/Animations/AnimClass_ButtonBounce");
            EnsureAssetDirectory("Assets/_Project/Styling/Animations/AnimClass_CardHoverLift");
            EnsureAssetDirectory("Assets/_Project/Styling/Animations/AnimClass_BadgePulse");
            EnsureAssetDirectory("Assets/_Project/Styling/Styles/StyleClass_BtnPrimary");
            EnsureAssetDirectory("Assets/_Project/Styling/Styles/StyleClass_CardSurface");

            // 1. Theme Palette
            string themePath = "Assets/_Project/Styling/Themes/UITheme_ModernDark/UITheme_ModernDark.asset";
            UIThemePalette theme = AssetDatabase.LoadAssetAtPath<UIThemePalette>(themePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UIThemePalette>();
                theme.PopulateDefaultsIfEmpty();
                AssetDatabase.CreateAsset(theme, themePath);
            }
            UIThemeManager.ActiveTheme = theme;

            // 2. Animation Classes
            string btnAnimPath = "Assets/_Project/Styling/Animations/AnimClass_ButtonBounce/AnimClass_ButtonBounce.asset";
            var btnAnim = AssetDatabase.LoadAssetAtPath<UIAnimationClass>(btnAnimPath);
            if (btnAnim == null)
            {
                btnAnim = ScriptableObject.CreateInstance<UIAnimationClass>();
                AssetDatabase.CreateAsset(btnAnim, btnAnimPath);
            }
            btnAnim.PopulateDefaultStatesIfEmpty();
            EditorUtility.SetDirty(btnAnim);

            string cardAnimPath = "Assets/_Project/Styling/Animations/AnimClass_CardHoverLift/AnimClass_CardHoverLift.asset";
            var cardAnim = AssetDatabase.LoadAssetAtPath<UIAnimationClass>(cardAnimPath);
            if (cardAnim == null)
            {
                cardAnim = ScriptableObject.CreateInstance<UIAnimationClass>();
                AssetDatabase.CreateAsset(cardAnim, cardAnimPath);
            }
            cardAnim.PopulateDefaultStatesIfEmpty();
            EditorUtility.SetDirty(cardAnim);

            string pulseAnimPath = "Assets/_Project/Styling/Animations/AnimClass_BadgePulse/AnimClass_BadgePulse.asset";
            var pulseAnim = AssetDatabase.LoadAssetAtPath<UIAnimationClass>(pulseAnimPath);
            if (pulseAnim == null)
            {
                pulseAnim = ScriptableObject.CreateInstance<UIAnimationClass>();
                AssetDatabase.CreateAsset(pulseAnim, pulseAnimPath);
            }
            pulseAnim.PopulateDefaultStatesIfEmpty();
            SerializedObject pulseAnimSO = new SerializedObject(pulseAnim);
            pulseAnimSO.FindProperty("m_ClassName").stringValue = ".badge-pulse";
            pulseAnimSO.FindProperty("m_Description").stringValue = "Continuous smooth breathing scale loop for notification badges and highlights.";
            pulseAnimSO.FindProperty("m_LoopType").enumValueIndex = (int)UILoopAnimationType.Pulse;
            pulseAnimSO.FindProperty("m_LoopSpeed").floatValue = 1.8f;
            pulseAnimSO.FindProperty("m_LoopMagnitude").floatValue = 0.08f;
            pulseAnimSO.ApplyModifiedPropertiesWithoutUndo();

            // 3. Style Classes
            string btnStylePath = "Assets/_Project/Styling/Styles/StyleClass_BtnPrimary/StyleClass_BtnPrimary.asset";
            var btnStyle = AssetDatabase.LoadAssetAtPath<UIStyleClass>(btnStylePath);
            if (btnStyle == null)
            {
                btnStyle = ScriptableObject.CreateInstance<UIStyleClass>();
                AssetDatabase.CreateAsset(btnStyle, btnStylePath);
            }
            SerializedObject btnStyleSO = new SerializedObject(btnStyle);
            btnStyleSO.FindProperty("m_ClassName").stringValue = ".btn-primary";
            btnStyleSO.FindProperty("m_Description").stringValue = "Primary call-to-action button with rounded corners and interactive bounce.";
            btnStyleSO.FindProperty("m_ColorToken").stringValue = "primary";
            btnStyleSO.FindProperty("m_BorderColorToken").stringValue = "border";
            btnStyleSO.FindProperty("m_RadiusToken").stringValue = "radius-md";
            btnStyleSO.FindProperty("m_BorderWidthToken").stringValue = "border-thin";
            btnStyleSO.FindProperty("m_AnimationClass").objectReferenceValue = btnAnim;
            btnStyleSO.ApplyModifiedPropertiesWithoutUndo();

            string cardStylePath = "Assets/_Project/Styling/Styles/StyleClass_CardSurface/StyleClass_CardSurface.asset";
            var cardStyle = AssetDatabase.LoadAssetAtPath<UIStyleClass>(cardStylePath);
            if (cardStyle == null)
            {
                cardStyle = ScriptableObject.CreateInstance<UIStyleClass>();
                AssetDatabase.CreateAsset(cardStyle, cardStylePath);
            }
            SerializedObject cardStyleSO = new SerializedObject(cardStyle);
            cardStyleSO.FindProperty("m_ClassName").stringValue = ".card-surface";
            cardStyleSO.FindProperty("m_Description").stringValue = "Surface card container with border and hover lift.";
            cardStyleSO.FindProperty("m_ColorToken").stringValue = "surface";
            cardStyleSO.FindProperty("m_BorderColorToken").stringValue = "border";
            cardStyleSO.FindProperty("m_RadiusToken").stringValue = "radius-lg";
            cardStyleSO.FindProperty("m_BorderWidthToken").stringValue = "border-thin";
            cardStyleSO.FindProperty("m_AnimationClass").objectReferenceValue = cardAnim;
            cardStyleSO.ApplyModifiedPropertiesWithoutUndo();

            // 4. Bake Theme and Animation Fallback Thumbnails
            UIThemeThumbnailBaker.BakePaletteThumbnail(theme);
            UIAnimationThumbnailBaker.GetOrCreateDefaultAnimationThumbnail();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CSS Studio] Starter styling assets generated successfully in Assets/_Project/Styling!");
        }

        private void OnEnable()
        {
            RefreshAssets();
        }

        private void OnFocus()
        {
            RefreshAssets();
        }

        private void RefreshAssets()
        {
            m_AllPalettes.Clear();
            string[] paletteGuids = AssetDatabase.FindAssets("t:UIThemePalette");
            foreach (var g in paletteGuids)
            {
                var p = AssetDatabase.LoadAssetAtPath<UIThemePalette>(AssetDatabase.GUIDToAssetPath(g));
                if (p != null) m_AllPalettes.Add(p);
            }

            m_AllAnimClasses.Clear();
            string[] animGuids = AssetDatabase.FindAssets("t:UIAnimationClass");
            foreach (var g in animGuids)
            {
                var a = AssetDatabase.LoadAssetAtPath<UIAnimationClass>(AssetDatabase.GUIDToAssetPath(g));
                if (a != null) m_AllAnimClasses.Add(a);
            }

            m_AllStyleClasses.Clear();
            string[] styleGuids = AssetDatabase.FindAssets("t:UIStyleClass");
            foreach (var g in styleGuids)
            {
                var s = AssetDatabase.LoadAssetAtPath<UIStyleClass>(AssetDatabase.GUIDToAssetPath(g));
                if (s != null) m_AllStyleClasses.Add(s);
            }
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.Space(4);
            m_CurrentTab = (Tab)GUILayout.Toolbar((int)m_CurrentTab, new string[]
            {
                "🎨 Color & Token Palette",
                "✨ Animation Classes",
                "🏷️ Style Classes"
            }, GUILayout.Height(28));

            EditorGUILayout.Space(6);

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            switch (m_CurrentTab)
            {
                case Tab.Palette:
                    DrawPaletteTab();
                    break;
                case Tab.AnimationClasses:
                    DrawAnimationClassesTab();
                    break;
                case Tab.StyleClasses:
                    DrawStyleClassesTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.toolbar);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("UI CSS Studio — Edit in 1 Place, Sync Everywhere", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🔄 Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshAssets();
                UIThemeManager.NotifyThemeChanged();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawPaletteTab()
        {
            var activeTheme = UIThemeManager.ActiveTheme;

            // Palette Selector Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Active Theme:", EditorStyles.boldLabel, GUILayout.Width(90));

            string[] names = new string[m_AllPalettes.Count + 1];
            names[0] = "<None>";
            int selectedIndex = 0;
            for (int i = 0; i < m_AllPalettes.Count; i++)
            {
                names[i + 1] = m_AllPalettes[i].ThemeName;
                if (m_AllPalettes[i] == activeTheme)
                    selectedIndex = i + 1;
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, names);
            if (newIndex != selectedIndex)
            {
                UIThemeManager.ActiveTheme = (newIndex == 0) ? null : m_AllPalettes[newIndex - 1];
            }

            if (GUILayout.Button("+ New Theme", GUILayout.Width(95)))
            {
                CreateNewThemeAsset();
            }
            EditorGUILayout.EndHorizontal();

            if (activeTheme == null)
            {
                EditorGUILayout.HelpBox("No active Theme Palette selected. Create or select a palette to begin editing colors.", MessageType.Warning);
                return;
            }

            // Render theme palette inspector embedded
            if (m_PaletteCachedEditor == null || m_PaletteCachedEditor.target != activeTheme)
            {
                UnityEditor.Editor.CreateCachedEditor(activeTheme, null, ref m_PaletteCachedEditor);
            }

            if (m_PaletteCachedEditor != null)
            {
                m_PaletteCachedEditor.OnInspectorGUI();
            }
        }

        private void DrawAnimationClassesTab()
        {
            EditorGUILayout.HelpBox("CSS Animation Classes define hover transitions, press reactions, and keyframe loops. Assigning a class to multiple UI elements lets you tweak timing and curves for all of them in 1 click.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Animation Classes ({m_AllAnimClasses.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Create Animation Class"))
            {
                CreateNewAnimationClassAsset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Quick live tester if selection has an animator
            var activeGO = Selection.activeGameObject;
            var activeAnimator = activeGO != null ? activeGO.GetComponent<UIClassAnimator>() : null;
            if (activeAnimator != null && activeAnimator.AnimationClass != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"🎮 Test Live on '{activeGO.name}':", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("🔄 Reset", GUILayout.Width(60)))
                {
                    activeAnimator.EditorReset();
                }
                EditorGUILayout.EndHorizontal();

                // Dynamic state buttons
                EditorGUILayout.BeginHorizontal();
                var states = activeAnimator.AnimationClass.States;
                for (int s = 0; s < states.Count && s < 6; s++)
                {
                    var st = states[s];
                    bool isActive = string.Equals(activeAnimator.CurrentStateName, st.stateName, System.StringComparison.OrdinalIgnoreCase);
                    if (isActive) GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);

                    if (GUILayout.Button(st.stateName, GUILayout.Height(24)))
                    {
                        activeAnimator.EditorPreviewState(st.stateName);
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("💥 Bounce", GUILayout.Height(24)))
                {
                    activeAnimator.EditorTriggerClickBounce();
                }

                bool loopActive = activeAnimator.IsEditorLoopActive;
                GUI.backgroundColor = loopActive ? new Color(0.4f, 0.9f, 0.4f) : Color.white;
                string loopName = activeAnimator.AnimationClass.LoopType != UILoopAnimationType.None
                    ? $"✨ {activeAnimator.AnimationClass.LoopType}"
                    : "✨ Loop";
                if (GUILayout.Button(loopActive ? "⏹ Stop" : loopName, GUILayout.Height(24)))
                {
                    activeAnimator.EditorToggleLoop();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            for (int i = 0; i < m_AllAnimClasses.Count; i++)
            {
                var animClass = m_AllAnimClasses[i];
                if (animClass == null) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(animClass.ClassName, EditorStyles.boldLabel, GUILayout.Width(140));
                GUILayout.Label($"Hover: {animClass.HoverScale:F2}x | Press: {animClass.PressScale:F2}x | Loop: {animClass.LoopType}", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Apply To Selected UI", GUILayout.Width(140)))
                {
                    ApplyAnimationClassToSelection(animClass);
                }

                if (GUILayout.Button("Select Asset", GUILayout.Width(85)))
                {
                    Selection.activeObject = animClass;
                    EditorGUIUtility.PingObject(animClass);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawStyleClassesTab()
        {
            EditorGUILayout.HelpBox("Style Classes bundle color tokens, border radius, and animation presets into a single reusable CSS class (e.g. .btn-primary, .card-elevated).", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Style Classes ({m_AllStyleClasses.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Create Style Class"))
            {
                CreateNewStyleClassAsset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            for (int i = 0; i < m_AllStyleClasses.Count; i++)
            {
                var styleClass = m_AllStyleClasses[i];
                if (styleClass == null) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(styleClass.ClassName, EditorStyles.boldLabel, GUILayout.Width(130));
                GUILayout.Label($"Color: [{styleClass.ColorToken}] | Radius: [{styleClass.RadiusToken}]", EditorStyles.miniLabel);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Apply To Selected UI", GUILayout.Width(140)))
                {
                    ApplyStyleClassToSelection(styleClass);
                }

                if (GUILayout.Button("Select Asset", GUILayout.Width(85)))
                {
                    Selection.activeObject = styleClass;
                    EditorGUIUtility.PingObject(styleClass);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void ApplyAnimationClassToSelection(UIAnimationClass animClass)
        {
            var selectedGOs = Selection.gameObjects;
            if (selectedGOs.Length == 0)
            {
                EditorUtility.DisplayDialog("Notice", "Please select one or more GameObjects in the scene hierarchy first.", "OK");
                return;
            }

            int count = 0;
            foreach (var go in selectedGOs)
            {
                var animator = go.GetComponent<UIClassAnimator>();
                if (animator == null)
                {
                    animator = Undo.AddComponent<UIClassAnimator>(go);
                }
                else
                {
                    Undo.RecordObject(animator, "Apply UI Animation Class");
                }
                animator.AnimationClass = animClass;
                count++;
            }

            Debug.Log($"[CSS Studio] Applied animation class '{animClass.ClassName}' to {count} GameObjects.");
        }

        private void ApplyStyleClassToSelection(UIStyleClass styleClass)
        {
            var selectedGOs = Selection.gameObjects;
            if (selectedGOs.Length == 0)
            {
                EditorUtility.DisplayDialog("Notice", "Please select one or more GameObjects in the scene hierarchy first.", "OK");
                return;
            }

            int count = 0;
            foreach (var go in selectedGOs)
            {
                var binder = go.GetComponent<UIStyleClassBinder>();
                if (binder == null)
                {
                    binder = Undo.AddComponent<UIStyleClassBinder>(go);
                }
                else
                {
                    Undo.RecordObject(binder, "Apply UI Style Class");
                }
                binder.StyleClass = styleClass;
                binder.ApplyStyleClass();
                count++;
            }

            Debug.Log($"[CSS Studio] Applied style class '{styleClass.ClassName}' to {count} GameObjects.");
        }

        private void CreateNewThemeAsset()
        {
            EnsureAssetDirectory("Assets/_Project/Styling/Themes");
            var asset = ScriptableObject.CreateInstance<UIThemePalette>();
            asset.PopulateDefaultsIfEmpty();
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Styling/Themes/NewTheme");
            EnsureAssetDirectory(folder);
            string name = Path.GetFileName(folder);
            string path = $"{folder}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            UIThemeThumbnailBaker.BakePaletteThumbnail(asset);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            UIThemeManager.ActiveTheme = asset;
            Selection.activeObject = asset;
        }

        private void CreateNewAnimationClassAsset()
        {
            EnsureAssetDirectory("Assets/_Project/Styling/Animations");
            var asset = ScriptableObject.CreateInstance<UIAnimationClass>();
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Styling/Animations/NewAnimation");
            EnsureAssetDirectory(folder);
            string name = Path.GetFileName(folder);
            string path = $"{folder}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            Selection.activeObject = asset;
        }

        private void CreateNewStyleClassAsset()
        {
            EnsureAssetDirectory("Assets/_Project/Styling/Styles");
            var asset = ScriptableObject.CreateInstance<UIStyleClass>();
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Styling/Styles/NewStyle");
            EnsureAssetDirectory(folder);
            string name = Path.GetFileName(folder);
            string path = $"{folder}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            RefreshAssets();
            Selection.activeObject = asset;
        }

        private static void EnsureAssetDirectory(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] parts = folderPath.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = $"{current}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }
    }
}
#endif
