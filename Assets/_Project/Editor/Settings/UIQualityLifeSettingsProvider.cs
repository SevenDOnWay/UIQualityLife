#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Registers UI Quality of Life configuration into Unity's Project Settings window.
    /// (Edit > Project Settings > UI Quality of Life).
    /// </summary>
    public static class UIQualityLifeSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/UI Quality of Life", SettingsScope.Project)
            {
                label = "UI Quality of Life",
                guiHandler = (searchContext) =>
                {
                    var settings = UIQualityLifeSettings.instance;
                    var serializedObj = new SerializedObject(settings);

                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Asset Storage Locations", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Configure target folders for UI presets, theme palettes, animation classes, and styles. " +
                                            "Folders outside Assets/_Project are fully supported for custom project structures.", MessageType.Info);

                    DrawFolderField(serializedObj.FindProperty("m_PresetsFolder"), "Presets Root Folder");
                    DrawFolderField(serializedObj.FindProperty("m_ThemesFolder"), "Themes Root Folder");
                    DrawFolderField(serializedObj.FindProperty("m_AnimationsFolder"), "Animations Root Folder");
                    DrawFolderField(serializedObj.FindProperty("m_StylesFolder"), "Styles Root Folder");

                    EditorGUILayout.Space(15);
                    EditorGUILayout.LabelField("Automation & CLI Bridge", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObj.FindProperty("m_BridgePort"), new GUIContent("HTTP Bridge Port"));
                    EditorGUILayout.PropertyField(serializedObj.FindProperty("m_AutoStartBridge"), new GUIContent("Auto-Start Bridge On Load"));

                    DrawFolderField(serializedObj.FindProperty("m_QueueFolder"), "File Queue Folder");
                    DrawFolderField(serializedObj.FindProperty("m_FinishedFolder"), "Finished Output Folder");

                    EditorGUILayout.Space(15);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reset to Defaults", GUILayout.Width(130)))
                    {
                        if (EditorUtility.DisplayDialog("Reset Settings", "Reset all UI Quality of Life paths to default?", "Yes", "Cancel"))
                        {
                            settings.ResetToDefaults();
                            serializedObj.Update();
                        }
                    }

                    if (GUILayout.Button("Create Missing Folders", GUILayout.Width(160)))
                    {
                        EnsureFolder(UIQualityLifeSettings.PresetsFolder);
                        EnsureFolder(UIQualityLifeSettings.ThemesFolder);
                        EnsureFolder(UIQualityLifeSettings.AnimationsFolder);
                        EnsureFolder(UIQualityLifeSettings.StylesFolder);
                        EnsureFolder(UIQualityLifeSettings.QueueFolder);
                        EnsureFolder(UIQualityLifeSettings.FinishedFolder);
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("Folders Created", "Configured storage directories have been verified and created.", "OK");
                    }
                    EditorGUILayout.EndHorizontal();

                    if (serializedObj.ApplyModifiedProperties())
                    {
                        settings.SaveSettings();
                    }
                },
                keywords = new HashSet<string>(new[] { "UI", "Presets", "Themes", "Animations", "Bridge", "Styling", "Quality of Life" })
            };

            return provider;
        }

        private static void DrawFolderField(SerializedProperty prop, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop, new GUIContent(label));
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string currentPath = prop.stringValue;
                string absPath = Application.dataPath;
                if (!string.IsNullOrEmpty(currentPath) && currentPath.StartsWith("Assets"))
                {
                    absPath = Path.Combine(Directory.GetCurrentDirectory(), currentPath);
                }

                string selected = EditorUtility.OpenFolderPanel($"Select {label}", absPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
                    selected = selected.Replace('\\', '/');

                    if (selected.StartsWith(projectRoot))
                    {
                        string relative = selected.Substring(projectRoot.Length).TrimStart('/');
                        prop.stringValue = relative;
                    }
                    else
                    {
                        prop.stringValue = selected;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void EnsureFolder(string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
#endif
