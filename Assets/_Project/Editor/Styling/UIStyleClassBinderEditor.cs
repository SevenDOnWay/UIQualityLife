#if UNITY_EDITOR
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

            // HEADER BANNER
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("CSS Class & Variable Reactive Binder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Elements reactively update when StyleSheet variables or rules change.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);

            // CSS CLASS INPUT
            EditorGUILayout.PropertyField(m_ClassNamesProp, new GUIContent("Class Selectors", "Space-separated CSS class names (e.g. 'btn btn-danger rounded-lg')"));
            EditorGUILayout.PropertyField(m_ElementIdProp, new GUIContent("Element ID", "Optional specific ID selector (e.g. 'submit-btn')"));
            EditorGUILayout.PropertyField(m_StyleSheetOverrideProp, new GUIContent("StyleSheet Override", "Defaults to active project StyleSheet if empty"));

            // CLASS PILLS DISPLAY
            var parsedClasses = binder.GetParsedClassList();
            if (parsedClasses.Count > 0)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Active Classes:", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                foreach (var cls in parsedClasses)
                {
                    bool existsInSheet = sheet != null && (sheet.TryGetRule("." + cls, out _) || sheet.TryGetRule(cls, out _));
                    GUI.backgroundColor = existsInSheet ? new Color(0.2f, 0.6f, 1f, 1f) : new Color(0.85f, 0.4f, 0.2f, 1f);

                    string label = $".{cls}" + (existsInSheet ? "" : " (not defined)");
                    GUILayout.Box(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            // ADD CLASS FROM STYLESHEET HELPER
            if (sheet != null && sheet.Rules != null && sheet.Rules.Count > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Quick Add Rule");
                if (EditorGUILayout.DropdownButton(new GUIContent("+ Add Class..."), FocusType.Keyboard))
                {
                    var menu = new GenericMenu();
                    foreach (var rule in sheet.Rules)
                    {
                        if (rule == null || string.IsNullOrEmpty(rule.selector)) continue;
                        string cleanName = rule.CleanSelectorName;
                        menu.AddItem(new GUIContent(rule.selector), false, () =>
                        {
                            string current = m_ClassNamesProp.stringValue ?? "";
                            if (!current.Contains(cleanName))
                            {
                                m_ClassNamesProp.stringValue = (current + " " + cleanName).Trim();
                                serializedObject.ApplyModifiedProperties();
                                binder.ApplyStyleClass();
                            }
                        });
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();
            }

            // COMPUTED VALUES PREVIEW
            EditorGUILayout.Space(6);
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

            // LEGACY FOLD OUT
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
    }
}
#endif
