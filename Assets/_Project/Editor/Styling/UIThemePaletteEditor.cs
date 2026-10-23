#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UIQualityLife.Editor;

namespace UIQualityLife.Styling.Editor
{
    [CustomEditor(typeof(UIThemePalette))]
    public class UIThemePaletteEditor : UnityEditor.Editor
    {
        private UIThemePalette m_Target;

        private void OnEnable()
        {
            m_Target = (UIThemePalette)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎨 UI Theme Palette (CSS Design Tokens)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tokens defined here serve as the single source of truth for colors and dimensions across all UGUI elements.", MessageType.Info);
            EditorGUILayout.Space(4);

            SerializedProperty themeNameProp = serializedObject.FindProperty("m_ThemeName");
            SerializedProperty descProp = serializedObject.FindProperty("m_Description");
            EditorGUILayout.PropertyField(themeNameProp);
            EditorGUILayout.PropertyField(descProp);

            EditorGUILayout.Space(8);

            // Active Theme Status
            bool isCurrentActive = UIThemeManager.ActiveTheme == m_Target;
            EditorGUILayout.BeginHorizontal();
            if (isCurrentActive)
            {
                GUI.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
                GUILayout.Box("✓ ACTIVE PROJECT THEME", GUILayout.Height(24), GUILayout.ExpandWidth(true));
                GUI.backgroundColor = Color.white;
            }
            else
            {
                if (GUILayout.Button("Set As Active Project Theme", GUILayout.Height(24)))
                {
                    UIThemeManager.ActiveTheme = m_Target;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Render Color Tokens visually
            SerializedProperty colorsProp = serializedObject.FindProperty("m_Colors");
            EditorGUILayout.LabelField($"Color Tokens ({colorsProp.arraySize})", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            for (int i = 0; i < colorsProp.arraySize; i++)
            {
                SerializedProperty token = colorsProp.GetArrayElementAtIndex(i);
                SerializedProperty idProp = token.FindPropertyRelative("id");
                SerializedProperty colorProp = token.FindPropertyRelative("color");
                SerializedProperty noteProp = token.FindPropertyRelative("description");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(idProp, GUIContent.none, GUILayout.Width(130));
                EditorGUILayout.PropertyField(colorProp, GUIContent.none);

                // Hex code display
                Color col = colorProp.colorValue;
                string hex = $"#{ColorUtility.ToHtmlStringRGBA(col)}";
                GUILayout.Label(hex, EditorStyles.miniLabel, GUILayout.Width(80));

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    colorsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(noteProp.stringValue))
                {
                    EditorGUILayout.LabelField(noteProp.stringValue, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Color Token"))
            {
                colorsProp.arraySize++;
                var elem = colorsProp.GetArrayElementAtIndex(colorsProp.arraySize - 1);
                elem.FindPropertyRelative("id").stringValue = $"color-{colorsProp.arraySize}";
                elem.FindPropertyRelative("color").colorValue = Color.white;
                elem.FindPropertyRelative("description").stringValue = "";
            }
            if (GUILayout.Button("Load Default Palette", GUILayout.Width(140)))
            {
                if (EditorUtility.DisplayDialog("Load Defaults", "Load default color tokens? Existing tokens will be kept.", "Yes", "Cancel"))
                {
                    m_Target.PopulateDefaultsIfEmpty();
                    EditorUtility.SetDirty(m_Target);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);

            // Render Float Tokens
            SerializedProperty floatsProp = serializedObject.FindProperty("m_Floats");
            EditorGUILayout.LabelField($"Dimension / Float Tokens ({floatsProp.arraySize})", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            for (int i = 0; i < floatsProp.arraySize; i++)
            {
                SerializedProperty token = floatsProp.GetArrayElementAtIndex(i);
                SerializedProperty idProp = token.FindPropertyRelative("id");
                SerializedProperty valProp = token.FindPropertyRelative("value");

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(idProp, GUIContent.none, GUILayout.Width(130));
                EditorGUILayout.PropertyField(valProp, GUIContent.none);

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    floatsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;

            if (GUILayout.Button("+ Add Float Token"))
            {
                floatsProp.arraySize++;
                var elem = floatsProp.GetArrayElementAtIndex(floatsProp.arraySize - 1);
                elem.FindPropertyRelative("id").stringValue = $"val-{floatsProp.arraySize}";
                elem.FindPropertyRelative("value").floatValue = 12f;
            }

            EditorGUILayout.Space(12);
            if (GUILayout.Button("⚡ Force Live Sync All Open UI", GUILayout.Height(28)))
            {
                UIThemeManager.NotifyThemeChanged();
                SceneView.RepaintAll();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                UIThemeManager.NotifyThemeChanged();
            }
        }
    }
}
#endif
