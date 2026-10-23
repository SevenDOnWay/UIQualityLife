#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;

namespace UIQualityLife.Editor
{
    [CustomEditor(typeof(UIStyleSheet))]
    public class UIStyleSheetEditor : UnityEditor.Editor
    {
        private SerializedProperty m_StyleSheetNameProp;
        private SerializedProperty m_DescriptionProp;
        private SerializedProperty m_ColorsProp;
        private SerializedProperty m_FloatsProp;
        private SerializedProperty m_TypographyProp;
        private SerializedProperty m_ShadowsProp;
        private SerializedProperty m_RulesProp;

        private int selectedTab = 0;
        private readonly string[] tabs = { "Color Tokens", "Dimension / Radius", "Typography & Shadows", "CSS Rulesets" };

        public static int s_InitialTab = -1;
        public static string s_HighlightRuleSelector = null;

        public static void FocusRule(UIStyleSheet sheet, string selector)
        {
            if (sheet == null) sheet = UIStyleSheet.ActiveStyleSheet;
            if (sheet == null) return;

            s_InitialTab = 3; // "CSS Rulesets" tab
            s_HighlightRuleSelector = selector;
            Selection.activeObject = sheet;
            EditorGUIUtility.PingObject(sheet);
        }

        public static void CreateAndFocusNewRule(UIStyleSheet sheet, string newSelector = ".new-class")
        {
            if (sheet == null) sheet = UIStyleSheet.ActiveStyleSheet;
            if (sheet == null) return;

            if (!sheet.TryGetRule(newSelector, out _))
            {
                var newRule = new UIStyleRule(newSelector, "Custom rule");
                sheet.AddOrUpdateRule(newRule);
                EditorUtility.SetDirty(sheet);
                AssetDatabase.SaveAssets();
            }

            FocusRule(sheet, newSelector);
        }

        private void OnEnable()
        {
            if (s_InitialTab >= 0)
            {
                selectedTab = s_InitialTab;
                s_InitialTab = -1;
            }

            m_StyleSheetNameProp = serializedObject.FindProperty("m_StyleSheetName");
            m_DescriptionProp = serializedObject.FindProperty("m_Description");
            m_ColorsProp = serializedObject.FindProperty("m_Colors");
            m_FloatsProp = serializedObject.FindProperty("m_Floats");
            m_TypographyProp = serializedObject.FindProperty("m_Typography");
            m_ShadowsProp = serializedObject.FindProperty("m_Shadows");
            m_RulesProp = serializedObject.FindProperty("m_Rules");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var sheet = (UIStyleSheet)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("CSS Style Sheet & Design Token System", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_StyleSheetNameProp, new GUIContent("Name"));
            EditorGUILayout.PropertyField(m_DescriptionProp, new GUIContent("Description"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import Figma JSON...", GUILayout.Height(24)))
            {
                FigmaTokenImportWindow.Open();
            }
            if (GUILayout.Button("Set As Active Project Sheet", GUILayout.Height(24)))
            {
                UIStyleSheet.ActiveStyleSheet = sheet;
                EditorUtility.DisplayDialog("Active Sheet Updated", $"'{sheet.StyleSheetName}' is now the active project StyleSheet.", "OK");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            EditorGUILayout.Space(6);

            switch (selectedTab)
            {
                case 0:
                    DrawColorsTab(sheet);
                    break;
                case 1:
                    DrawDimensionsTab(sheet);
                    break;
                case 2:
                    DrawTypographyShadowsTab();
                    break;
                case 3:
                    DrawRulesTab(sheet);
                    break;
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                sheet.NotifyVariableChanged("*");
                SceneView.RepaintAll();
            }
        }

        private void DrawColorsTab(UIStyleSheet sheet)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Color Tokens ({m_ColorsProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Color", GUILayout.Width(100)))
            {
                m_ColorsProp.InsertArrayElementAtIndex(m_ColorsProp.arraySize);
                var elem = m_ColorsProp.GetArrayElementAtIndex(m_ColorsProp.arraySize - 1);
                elem.FindPropertyRelative("id").stringValue = "new-color";
                elem.FindPropertyRelative("color").colorValue = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < m_ColorsProp.arraySize; i++)
            {
                var elem = m_ColorsProp.GetArrayElementAtIndex(i);
                var idProp = elem.FindPropertyRelative("id");
                var colorProp = elem.FindPropertyRelative("color");

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(idProp, GUIContent.none, GUILayout.Width(140));
                EditorGUILayout.PropertyField(colorProp, GUIContent.none);
                GUI.backgroundColor = UIElementStyleUtils.UnityDeleteChip;
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    GUI.backgroundColor = Color.white;
                    m_ColorsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawDimensionsTab(UIStyleSheet sheet)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Dimension / Float Tokens ({m_FloatsProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Float", GUILayout.Width(100)))
            {
                m_FloatsProp.InsertArrayElementAtIndex(m_FloatsProp.arraySize);
                var elem = m_FloatsProp.GetArrayElementAtIndex(m_FloatsProp.arraySize - 1);
                elem.FindPropertyRelative("id").stringValue = "radius-sm";
                elem.FindPropertyRelative("value").floatValue = 8f;
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < m_FloatsProp.arraySize; i++)
            {
                var elem = m_FloatsProp.GetArrayElementAtIndex(i);
                var idProp = elem.FindPropertyRelative("id");
                var valProp = elem.FindPropertyRelative("value");

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(idProp, GUIContent.none, GUILayout.Width(140));
                EditorGUILayout.PropertyField(valProp, GUIContent.none);
                GUI.backgroundColor = UIElementStyleUtils.UnityDeleteChip;
                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    GUI.backgroundColor = Color.white;
                    m_FloatsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTypographyShadowsTab()
        {
            EditorGUILayout.LabelField($"Typography Tokens ({m_TypographyProp.arraySize})", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_TypographyProp, true);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Shadow Tokens ({m_ShadowsProp.arraySize})", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ShadowsProp, true);
        }

        private void DrawRulesTab(UIStyleSheet sheet)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"CSS Rulesets ({m_RulesProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Rule", GUILayout.Width(100)))
            {
                m_RulesProp.InsertArrayElementAtIndex(m_RulesProp.arraySize);
                var elem = m_RulesProp.GetArrayElementAtIndex(m_RulesProp.arraySize - 1);
                elem.FindPropertyRelative("selector").stringValue = ".new-class";
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < m_RulesProp.arraySize; i++)
            {
                var ruleProp = m_RulesProp.GetArrayElementAtIndex(i);
                var selectorProp = ruleProp.FindPropertyRelative("selector");

                bool isHighlighted = !string.IsNullOrEmpty(s_HighlightRuleSelector) &&
                    string.Equals(selectorProp.stringValue, s_HighlightRuleSelector, System.StringComparison.OrdinalIgnoreCase);

                if (isHighlighted)
                {
                    GUI.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (isHighlighted) GUI.backgroundColor = Color.white;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(selectorProp, GUIContent.none, GUILayout.Width(180));
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = UIElementStyleUtils.UnityDeleteChip;
                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(65)))
                {
                    GUI.backgroundColor = Color.white;
                    m_RulesProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("overrideBackground"), new GUIContent("Override Background"));
                if (ruleProp.FindPropertyRelative("overrideBackground").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("backgroundColorToken"), new GUIContent("Color Token"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("overrideCornerRadius"), new GUIContent("Override Corner Radius"));
                if (ruleProp.FindPropertyRelative("overrideCornerRadius").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("cornerRadiusToken"), new GUIContent("Radius Token"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("overrideBorder"), new GUIContent("Override Border"));
                if (ruleProp.FindPropertyRelative("overrideBorder").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("borderColorToken"), new GUIContent("Border Color Token"));
                    EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("borderWidthToken"), new GUIContent("Border Width Token"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("overrideAnimation"), new GUIContent("Override Animation"));
                if (ruleProp.FindPropertyRelative("overrideAnimation").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(ruleProp.FindPropertyRelative("animationClass"), new GUIContent("Animation Class"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }
        }
    }
}
#endif
