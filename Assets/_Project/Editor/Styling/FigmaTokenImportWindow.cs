#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;

namespace UIQualityLife.Editor
{
    public class FigmaTokenImportWindow : EditorWindow
    {
        private UIStyleSheet targetStyleSheet;
        private string jsonContent = "";
        private string importedFilePath = "";
        private Vector2 scrollPos;
        private TokenImportReport lastReport;

        [MenuItem("Tools/UI Quality of Life/Import Figma Tokens (JSON)", false, 20)]
        [MenuItem("Window/UI Quality of Life/Import Figma Tokens (JSON)", false, 120)]
        public static void Open()
        {
            var win = GetWindow<FigmaTokenImportWindow>("Figma Tokens");
            win.minSize = new Vector2(450, 520);
        }

        private void OnEnable()
        {
            if (targetStyleSheet == null)
            {
                targetStyleSheet = UIStyleSheet.ActiveStyleSheet;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Figma & Design Tokens JSON Importer", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Drag and drop a Figma Variables or Tokens Studio .json export to sync variables and CSS class rules.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // TARGET STYLESHEET SELECTOR
            EditorGUILayout.BeginHorizontal();
            targetStyleSheet = (UIStyleSheet)EditorGUILayout.ObjectField("Target StyleSheet", targetStyleSheet, typeof(UIStyleSheet), false);
            if (GUILayout.Button("+ New", GUILayout.Width(55)))
            {
                CreateNewStyleSheet();
            }
            EditorGUILayout.EndHorizontal();

            if (targetStyleSheet == null)
            {
                EditorGUILayout.HelpBox("Select or create a UIStyleSheet to store the imported variables and rules.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // DRAG AND DROP BOX
            var dragArea = GUILayoutUtility.GetRect(0f, 75f, GUILayout.ExpandWidth(true));
            GUI.Box(dragArea, string.IsNullOrEmpty(importedFilePath) ? "DRAG & DROP .JSON FILE HERE\n(or paste JSON below)" : $"Loaded: {Path.GetFileName(importedFilePath)}", EditorStyles.helpBox);

            HandleDragAndDrop(dragArea);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse File...", GUILayout.Height(24)))
            {
                string path = EditorUtility.OpenFilePanel("Select Figma Tokens JSON", "", "json");
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    LoadFile(path);
                }
            }
            if (GUILayout.Button("Paste from Clipboard", GUILayout.Height(24)))
            {
                string clip = EditorGUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clip) && clip.Trim().StartsWith("{"))
                {
                    jsonContent = clip;
                    importedFilePath = "Clipboard JSON";
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(24)))
            {
                jsonContent = "";
                importedFilePath = "";
                lastReport = null;
            }
            EditorGUILayout.EndHorizontal();

            // JSON PREVIEW TEXT AREA
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("JSON Content Preview:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(180));
            jsonContent = EditorGUILayout.TextArea(jsonContent, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            // IMPORT BUTTON
            EditorGUILayout.Space(10);
            EditorGUI.BeginDisabledGroup(targetStyleSheet == null || string.IsNullOrWhiteSpace(jsonContent));
            GUI.backgroundColor = UIElementStyleUtils.UnityAccentBlue;
            if (GUILayout.Button("Import Tokens & Sync Scene Now", GUILayout.Height(32)))
            {
                PerformImport();
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            // REPORT BOX
            if (lastReport != null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Import Results", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(lastReport.Summary, EditorStyles.wordWrappedLabel);

                if (lastReport.warnings != null && lastReport.warnings.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Warnings:", EditorStyles.miniBoldLabel);
                    foreach (var w in lastReport.warnings)
                    {
                        EditorGUILayout.LabelField($"• {w}", EditorStyles.wordWrappedMiniLabel);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            var evt = UnityEngine.Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (!dropArea.Contains(evt.mousePosition)) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    if (DragAndDrop.paths.Length > 0)
                    {
                        string path = DragAndDrop.paths[0];
                        if (Path.GetExtension(path).Equals(".json", System.StringComparison.OrdinalIgnoreCase))
                        {
                            LoadFile(path);
                        }
                    }
                }
                evt.Use();
            }
        }

        private void LoadFile(string path)
        {
            importedFilePath = path;
            jsonContent = File.ReadAllText(path);
            Repaint();
        }

        private void PerformImport()
        {
            lastReport = FigmaTokenImporter.ImportTokensJson(jsonContent, targetStyleSheet);
            EditorUtility.SetDirty(targetStyleSheet);
            AssetDatabase.SaveAssets();

            // Force repaint Scene Views
            SceneView.RepaintAll();
        }

        private void CreateNewStyleSheet()
        {
            string folder = UIQualityLifeSettings.ThemesFolder;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = $"{folder}/UIStyleSheet_Custom.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var newSheet = CreateInstance<UIStyleSheet>();
            AssetDatabase.CreateAsset(newSheet, path);
            AssetDatabase.SaveAssets();
            targetStyleSheet = newSheet;
            Selection.activeObject = newSheet;
        }
    }
}
#endif
