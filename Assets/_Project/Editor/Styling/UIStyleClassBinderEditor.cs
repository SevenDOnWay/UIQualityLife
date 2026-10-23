#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Editor
{
    [CustomEditor(typeof(UIStyleClassBinder))]
    public class UIStyleClassBinderEditor : UnityEditor.Editor
    {
        private SerializedProperty m_ClassNamesProp;
        private SerializedProperty m_ElementIdProp;
        private SerializedProperty m_StyleSheetOverrideProp;
        private SerializedProperty m_LegacyStyleClassProp;

        private bool m_ShowComputedStyles = true;
        private bool m_ShowRawString = false;
        private string m_NewCustomClassInput = "";
        private bool m_ShowCustomClassInput = false;

        private void OnEnable()
        {
            m_ClassNamesProp = serializedObject.FindProperty("m_ClassNames");
            m_ElementIdProp = serializedObject.FindProperty("m_ElementId");
            m_StyleSheetOverrideProp = serializedObject.FindProperty("m_StyleSheetOverride");
            m_LegacyStyleClassProp = serializedObject.FindProperty("m_LegacyStyleClass");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var binder = (UIStyleClassBinder)target;
            var sheet = binder.StyleSheet;

            // 1. HEADER BANNER
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("CSS Class & Variable Reactive Binder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Select classes to style this element. Elements reactively update when StyleSheet variables change.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);

            // 2. PROCEDURAL IMAGE UPGRADE HELPER
            var currentProcedural = binder.GetComponent<ProceduralImage>();
            var currentStandard = binder.GetComponent<UnityEngine.UI.Image>();
            if (currentProcedural == null && currentStandard != null)
            {
                EditorGUILayout.HelpBox("This GameObject has standard Unity Image. Smooth corners, borders, chamfers, and shadows require ProceduralImage.", MessageType.Info);
                if (GUILayout.Button("⚡ Upgrade to ProceduralImage (Preserves Button Target)", GUILayout.Height(28)))
                {
                    binder.ExecuteUpgradeToProceduralImage();
                    binder.ApplyStyleClass();
                }
                EditorGUILayout.Space(4);
            }

            // 3. STYLESHEET STATUS
            if (sheet == null)
            {
                EditorGUILayout.HelpBox("No UIStyleSheet active in project. Click below to initialize the default design tokens and CSS class rules.", MessageType.Warning);
                if (GUILayout.Button("+ Initialize Default UIStyleSheet", GUILayout.Height(26)))
                {
                    UIStyleSheet.CreateDefaultStyleSheetAsset();
                    binder.ApplyStyleClass();
                }
                EditorGUILayout.Space(4);
            }

            // 4. MAIN CSS CLASS SELECTOR & ACTIVE PILLS
            DrawClassSelectorSection(binder, sheet);

            EditorGUILayout.Space(8);

            // 5. ELEMENT ID (OPTIONAL - TOP PRIORITY OVERRIDE)
            DrawElementIdSection(binder, sheet);

            EditorGUILayout.Space(6);

            // 6. STYLESHEET OVERRIDE (OPTIONAL)
            EditorGUILayout.PropertyField(m_StyleSheetOverrideProp, new GUIContent("StyleSheet Override", "Optional custom StyleSheet asset for this specific screen/element"));

            // 7. COMPUTED VALUES PREVIEW
            EditorGUILayout.Space(6);
            DrawComputedStylesPreview(binder);

            // 8. LEGACY COMPATIBILITY
            if (m_LegacyStyleClassProp.objectReferenceValue != null)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.PropertyField(m_LegacyStyleClassProp);
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Reapply Styles Now"))
            {
                binder.ApplyStyleClass();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                binder.ApplyStyleClass();
            }
        }

        private void DrawClassSelectorSection(UIStyleClassBinder binder, UIStyleSheet sheet)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header row with "+ Add Class" button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Applied CSS Classes", EditorStyles.boldLabel);

            GUI.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
            if (EditorGUILayout.DropdownButton(new GUIContent("+ Add Class ▾"), FocusType.Keyboard, EditorStyles.miniButton, GUILayout.Width(110), GUILayout.Height(20)))
            {
                ShowAddClassMenu(binder, sheet);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // Render active class pills
            var parsedClasses = binder.GetParsedClassList();
            if (parsedClasses.Count > 0)
            {
                EditorGUILayout.LabelField("Click a class to edit in StyleSheet • Click '✕' to remove:", EditorStyles.miniLabel);
                EditorGUILayout.Space(2);

                for (int i = 0; i < parsedClasses.Count; i++)
                {
                    string cls = parsedClasses[i];
                    bool existsInSheet = sheet != null && (sheet.TryGetRule("." + cls, out _) || sheet.TryGetRule(cls, out _));

                    EditorGUILayout.BeginHorizontal();

                    // Class badge button (Click to jump to StyleSheet) - Matches Unity Selection / Chip style
                    GUI.backgroundColor = existsInSheet ? UIElementStyleUtils.UnityAccentBlue : UIElementStyleUtils.UnitySubtleChip;
                    string icon = existsInSheet ? "✓ " : "• ";
                    string label = $"{icon}.{cls}" + (existsInSheet ? "  (StyleSheet ↗)" : "  (Utility ↗)");

                    if (GUILayout.Button(new GUIContent(label, "Click to navigate to this class in the StyleSheet"), EditorStyles.miniButtonLeft, GUILayout.Height(20)))
                    {
                        if (sheet != null)
                        {
                            if (existsInSheet)
                            {
                                UIStyleSheetEditor.FocusRule(sheet, cls);
                            }
                            else
                            {
                                if (EditorUtility.DisplayDialog("Rule Not in StyleSheet",
                                    $"'.{cls}' is currently treated as an external or utility class.\n\nDo you want to create '.{cls}' in the StyleSheet?", "Create Rule", "Cancel"))
                                {
                                    UIStyleSheetEditor.CreateAndFocusNewRule(sheet, "." + cls);
                                }
                            }
                        }
                    }

                    // Remove "✕" button
                    GUI.backgroundColor = UIElementStyleUtils.UnityDeleteChip;
                    if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(24), GUILayout.Height(20)))
                    {
                        RemoveClass(cls);
                        GUIUtility.ExitGUI();
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No CSS classes applied yet. Click [+ Add Class ▾] above to select or create a class.", MessageType.None);
            }

            // Custom Class Input Bar
            if (m_ShowCustomClassInput)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                m_NewCustomClassInput = EditorGUILayout.TextField("Custom Class:", m_NewCustomClassInput);
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    if (!string.IsNullOrWhiteSpace(m_NewCustomClassInput))
                    {
                        AddClass(m_NewCustomClassInput.Trim().TrimStart('.'));
                        m_NewCustomClassInput = "";
                        m_ShowCustomClassInput = false;
                    }
                }
                if (GUILayout.Button("Cancel", GUILayout.Width(55)))
                {
                    m_ShowCustomClassInput = false;
                }
                EditorGUILayout.EndHorizontal();
            }

            // Advanced Raw String foldout
            EditorGUILayout.Space(2);
            m_ShowRawString = EditorGUILayout.Foldout(m_ShowRawString, "Advanced (Raw Class String)", false);
            if (m_ShowRawString)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ClassNamesProp, new GUIContent("Raw String"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void ShowAddClassMenu(UIStyleClassBinder binder, UIStyleSheet sheet)
        {
            var menu = new GenericMenu();
            var parsed = binder.GetParsedClassList();

            // 1. Classes from active StyleSheet
            if (sheet != null && sheet.Rules != null && sheet.Rules.Count > 0)
            {
                foreach (var rule in sheet.Rules)
                {
                    if (rule == null || string.IsNullOrEmpty(rule.selector)) continue;
                    if (rule.IsIdSelector) continue; // Skip ID selectors from class list

                    string clean = rule.CleanSelectorName;
                    bool isApplied = parsed.Contains(clean);

                    menu.AddItem(new GUIContent($"StyleSheet Classes/{rule.selector}"), isApplied, () =>
                    {
                        if (isApplied) RemoveClass(clean);
                        else AddClass(clean);
                    });
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("StyleSheet Classes/(No rules in active StyleSheet)"));
            }

            menu.AddSeparator("");

            // 2. Common Utility Classes
            AddUtilityMenuItem(menu, "Utilities/Capsule Pill (round-pill)", "round-pill", parsed);
            AddUtilityMenuItem(menu, "Utilities/Rounded Small (rounded-sm - 4px)", "rounded-sm", parsed);
            AddUtilityMenuItem(menu, "Utilities/Rounded Medium (rounded-md - 8px)", "rounded-md", parsed);
            AddUtilityMenuItem(menu, "Utilities/Rounded Large (rounded-lg - 16px)", "rounded-lg", parsed);
            AddUtilityMenuItem(menu, "Utilities/Rounded Extra Large (rounded-xl - 24px)", "rounded-xl", parsed);
            AddUtilityMenuItem(menu, "Utilities/No Radius (rounded-none - 0px)", "rounded-none", parsed);

            menu.AddSeparator("");

            // 3. "+ Create New Class in StyleSheet..."
            menu.AddItem(new GUIContent("➕ Create New Class in StyleSheet..."), false, () =>
            {
                if (sheet != null)
                {
                    string suggestedName = ".custom-class";
                    UIStyleSheetEditor.CreateAndFocusNewRule(sheet, suggestedName);
                    AddClass("custom-class");
                }
                else
                {
                    EditorUtility.DisplayDialog("No StyleSheet", "Please initialize a StyleSheet first.", "OK");
                }
            });

            // 4. "+ Custom Utility Class (Type name)..."
            menu.AddItem(new GUIContent("✏️ Type Custom Utility Class Name..."), false, () =>
            {
                m_ShowCustomClassInput = true;
            });

            menu.ShowAsContext();
        }

        private void AddUtilityMenuItem(GenericMenu menu, string menuPath, string className, List<string> parsed)
        {
            bool isApplied = parsed.Contains(className);
            menu.AddItem(new GUIContent(menuPath), isApplied, () =>
            {
                if (isApplied) RemoveClass(className);
                else AddClass(className);
            });
        }

        private void AddClass(string className)
        {
            string clean = className.Trim().TrimStart('.');
            if (string.IsNullOrEmpty(clean)) return;

            string current = m_ClassNamesProp.stringValue ?? "";
            var list = new List<string>(current.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries));
            if (!list.Contains(clean))
            {
                list.Add(clean);
                m_ClassNamesProp.stringValue = string.Join(" ", list);
                serializedObject.ApplyModifiedProperties();
                ((UIStyleClassBinder)target).ApplyStyleClass();
            }
        }

        private void RemoveClass(string className)
        {
            string clean = className.Trim().TrimStart('.');
            string current = m_ClassNamesProp.stringValue ?? "";
            var list = new List<string>(current.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries));
            if (list.Remove(clean))
            {
                m_ClassNamesProp.stringValue = string.Join(" ", list);
                serializedObject.ApplyModifiedProperties();
                ((UIStyleClassBinder)target).ApplyStyleClass();
            }
        }

        private void DrawElementIdSection(UIStyleClassBinder binder, UIStyleSheet sheet)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Element ID (Optional - Top Priority Override)", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Optional unique identifier for THIS specific element (e.g. 'submit-btn').\n" +
                "Any style rules defined with '#id' in the StyleSheet take TOP PRIORITY and will override all class styles above.",
                MessageType.Info);

            EditorGUILayout.PropertyField(m_ElementIdProp, new GUIContent("Element ID", "Example: 'submit-btn' or '#submit-btn'"));

            string idValue = m_ElementIdProp.stringValue?.Trim().TrimStart('#');
            if (!string.IsNullOrEmpty(idValue))
            {
                bool exists = sheet != null && (sheet.TryGetRule("#" + idValue, out _) || sheet.TryGetRule(idValue, out _));

                EditorGUILayout.BeginHorizontal();
                if (exists)
                {
                    EditorGUILayout.LabelField($"✓ Override rule '#{idValue}' active in StyleSheet", EditorStyles.miniBoldLabel);
                    GUI.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
                    if (GUILayout.Button("↗ Edit Rule", EditorStyles.miniButton, GUILayout.Width(90), GUILayout.Height(20)))
                    {
                        UIStyleSheetEditor.FocusRule(sheet, "#" + idValue);
                    }
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.LabelField($"Rule '#{idValue}' is not yet defined in StyleSheet", EditorStyles.miniLabel);
                    GUI.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
                    if (GUILayout.Button("+ Create '#" + idValue + "'", EditorStyles.miniButton, GUILayout.Width(130), GUILayout.Height(20)))
                    {
                        if (sheet != null)
                        {
                            UIStyleSheetEditor.CreateAndFocusNewRule(sheet, "#" + idValue);
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawComputedStylesPreview(UIStyleClassBinder binder)
        {
            m_ShowComputedStyles = EditorGUILayout.Foldout(m_ShowComputedStyles, "Computed Style Preview", true);
            if (m_ShowComputedStyles)
            {
                EditorGUI.indentLevel++;
                var procImage = binder.GetComponent<ProceduralImage>();
                if (procImage != null)
                {
                    EditorGUILayout.LabelField("Resolved Fill Color", $"RGBA({procImage.color.r:F2}, {procImage.color.g:F2}, {procImage.color.b:F2}, {procImage.color.a:F2})");
                    EditorGUILayout.LabelField("Resolved Corner Radius", $"{procImage.uniformRadius:F1}px ({procImage.cornerStyle})");
                    if (procImage.borderWidth > 0f)
                    {
                        EditorGUILayout.LabelField("Resolved Border", $"{procImage.borderWidth:F1}px");
                    }
                }
                var txt = binder.GetComponentInChildren<TMPro.TMP_Text>();
                if (txt != null)
                {
                    EditorGUILayout.LabelField("Text Color", $"RGBA({txt.color.r:F2}, {txt.color.g:F2}, {txt.color.b:F2})");
                    EditorGUILayout.LabelField("Text Font Size", $"{txt.fontSize:F1}pt");
                }
                var anim = binder.GetComponent<UIClassAnimator>();
                if (anim != null && anim.AnimationClass != null)
                {
                    EditorGUILayout.LabelField("Animation Profile", anim.AnimationClass.name);
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif
