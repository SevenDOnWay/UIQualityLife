#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Styling.Editor
{
    [CustomEditor(typeof(UIThemeBinder))]
    [CanEditMultipleObjects]
    public class UIThemeBinderEditor : UnityEditor.Editor
    {
        private UIThemeBinder m_Target;

        private void OnEnable()
        {
            m_Target = (UIThemeBinder)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var theme = m_Target.Theme;
            if (theme == null)
            {
                EditorGUILayout.HelpBox("No Active UIThemePalette found! Create one via Assets > Create > UI Quality of Life > Styling > Theme Palette.", MessageType.Warning);
            }

            SerializedProperty themeOverrideProp = serializedObject.FindProperty("m_ThemeOverride");
            EditorGUILayout.PropertyField(themeOverrideProp, new GUIContent("Theme Asset Override"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎯 Token Bindings", EditorStyles.boldLabel);

            // Fetch available color tokens for popup
            List<string> colorTokens = new List<string> { "<None>" };
            List<string> floatTokens = new List<string> { "<None>" };

            if (theme != null)
            {
                for (int i = 0; i < theme.Colors.Count; i++)
                {
                    colorTokens.Add(theme.Colors[i].id);
                }
                for (int i = 0; i < theme.Floats.Count; i++)
                {
                    floatTokens.Add(theme.Floats[i].id);
                }
            }

            // Graphic Color
            SerializedProperty applyGraphicColor = serializedObject.FindProperty("m_ApplyGraphicColor");
            SerializedProperty colorTokenProp = serializedObject.FindProperty("m_ColorToken");
            SerializedProperty preserveAlphaProp = serializedObject.FindProperty("m_PreserveOriginalAlpha");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(applyGraphicColor, new GUIContent("Main Graphic Color"));
            if (applyGraphicColor.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawTokenDropdown("Color Token", colorTokenProp, colorTokens, isColorToken: true);
                EditorGUILayout.PropertyField(preserveAlphaProp, new GUIContent("Preserve Alpha"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            // Check if ProceduralImage exists
            ProceduralImage procImg = m_Target.GetComponent<ProceduralImage>();
            if (procImg != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Procedural UI Attributes", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Border Color
                SerializedProperty applyBorderColor = serializedObject.FindProperty("m_ApplyBorderColor");
                SerializedProperty borderColorToken = serializedObject.FindProperty("m_BorderColorToken");
                EditorGUILayout.PropertyField(applyBorderColor, new GUIContent("Border Color"));
                if (applyBorderColor.boolValue)
                {
                    EditorGUI.indentLevel++;
                    DrawTokenDropdown("Border Token", borderColorToken, colorTokens, isColorToken: true);
                    EditorGUI.indentLevel--;
                }

                // Gradient Color
                SerializedProperty applyGradient = serializedObject.FindProperty("m_ApplyGradientColor");
                SerializedProperty gradientToken = serializedObject.FindProperty("m_GradientColorToken");
                EditorGUILayout.PropertyField(applyGradient, new GUIContent("Gradient End Color"));
                if (applyGradient.boolValue)
                {
                    EditorGUI.indentLevel++;
                    DrawTokenDropdown("Gradient Token", gradientToken, colorTokens, isColorToken: true);
                    EditorGUI.indentLevel--;
                }

                // Corner Radius
                SerializedProperty applyRadius = serializedObject.FindProperty("m_ApplyRadius");
                SerializedProperty radiusToken = serializedObject.FindProperty("m_RadiusToken");
                EditorGUILayout.PropertyField(applyRadius, new GUIContent("Corner Radius"));
                if (applyRadius.boolValue)
                {
                    EditorGUI.indentLevel++;
                    DrawTokenDropdown("Radius Token", radiusToken, floatTokens, isColorToken: false);
                    EditorGUI.indentLevel--;
                }

                // Border Width
                SerializedProperty applyBorderWidth = serializedObject.FindProperty("m_ApplyBorderWidth");
                SerializedProperty borderWidthToken = serializedObject.FindProperty("m_BorderWidthToken");
                EditorGUILayout.PropertyField(applyBorderWidth, new GUIContent("Border Width"));
                if (applyBorderWidth.boolValue)
                {
                    EditorGUI.indentLevel++;
                    DrawTokenDropdown("Border Width Token", borderWidthToken, floatTokens, isColorToken: false);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                m_Target.ApplyTheme();
                EditorUtility.SetDirty(m_Target);
            }
        }

        private void DrawTokenDropdown(string label, SerializedProperty property, List<string> tokens, bool isColorToken)
        {
            string currentVal = property.stringValue ?? "";

            List<string> options = new List<string>(tokens);
            const string customOption = "✎ Custom (Type manually)...";
            options.Add(customOption);

            int selectedIndex = options.IndexOf(currentVal);
            bool isCustom = selectedIndex < 0 && !string.IsNullOrEmpty(currentVal);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            if (isCustom)
            {
                property.stringValue = EditorGUILayout.TextField(property.stringValue);
                if (GUILayout.Button("Pick List", EditorStyles.miniButton, GUILayout.Width(65)))
                {
                    property.stringValue = options.Count > 1 ? options[1] : "";
                }
            }
            else
            {
                int popupIndex = selectedIndex >= 0 ? selectedIndex : 0;
                int newIndex = EditorGUILayout.Popup(popupIndex, options.ToArray());

                if (newIndex != popupIndex)
                {
                    if (newIndex == 0)
                    {
                        property.stringValue = "";
                    }
                    else if (newIndex == options.Count - 1)
                    {
                        property.stringValue = "custom-token";
                    }
                    else
                    {
                        property.stringValue = options[newIndex];
                    }
                }
            }

            if (isColorToken && m_Target.Theme != null && !string.IsNullOrEmpty(property.stringValue))
            {
                if (m_Target.Theme.TryGetColor(property.stringValue, out Color resolvedColor))
                {
                    var rect = GUILayoutUtility.GetRect(20, 18, GUILayout.Width(20));
                    EditorGUI.DrawRect(rect, resolvedColor);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
