#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIQualityLife.Editor
{
    /// <summary>
    /// Pipeline engine that converts declarative UIComponentSpec JSON into functional UGUI prefabs,
    /// ScriptableObject presets, and baked auto-framed thumbnails.
    /// Can be invoked in-editor, via CLI (-executeMethod), or through the Live Editor Bridge.
    /// </summary>
    public static class UIComponentBuilder
    {
        public class BuildResult
        {
            public bool success;
            public string error;
            public string presetName;
            public string prefabPath;
            public string assetPath;
            public string thumbnailPath;
        }

        public static BuildResult BuildFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new BuildResult { success = false, error = "JSON payload was empty or null." };

            UIComponentSpec spec;
            try
            {
                spec = JsonUtility.FromJson<UIComponentSpec>(json);
            }
            catch (Exception ex)
            {
                return new BuildResult { success = false, error = $"Failed to parse JSON: {ex.Message}" };
            }

            return BuildFromSpec(spec);
        }

        public static BuildResult BuildFromSpec(UIComponentSpec spec)
        {
            if (spec == null || spec.root == null)
                return new BuildResult { success = false, error = "Component specification or root node is null." };

            string cleanName = Sanitize(string.IsNullOrEmpty(spec.name) ? "NewComponent" : spec.name);
            string[] categories = (spec.categories != null && spec.categories.Length > 0) ? spec.categories : new string[] { "General" };
            string primaryCategory = Sanitize(categories[0]);

            string categoryFolder = $"{UIPresetDatabase.PresetsRootFolder}/{primaryCategory}";
            if (!Directory.Exists(categoryFolder))
            {
                Directory.CreateDirectory(categoryFolder);
                AssetDatabase.Refresh();
            }

            string prefabPath = $"{categoryFolder}/{cleanName}.prefab";
            string assetPath = $"{categoryFolder}/{cleanName}.asset";

            // 1. Build GameObject hierarchy
            GameObject rootGO = BuildNodeHierarchy(spec.root, null);
            rootGO.name = cleanName;

            // 2. Save as Prefab Asset
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);
            UnityEngine.Object.DestroyImmediate(rootGO);

            if (prefabAsset == null)
            {
                return new BuildResult { success = false, error = $"Failed to save prefab to '{prefabPath}'." };
            }

            // 3. Create or update UIPresetItem ScriptableObject
            var preset = AssetDatabase.LoadAssetAtPath<UIPresetItem>(assetPath);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<UIPresetItem>();
                AssetDatabase.CreateAsset(preset, assetPath);
            }

            preset.displayName = spec.name;
            preset.categories = categories;
            preset.description = spec.description ?? "";
            preset.previewColor = ParseColor(spec.previewColorHex, new Color(0.14f, 0.45f, 0.95f));
            preset.prefab = prefabAsset;
            preset.systemType = UISystemType.UGUI;

            // 4. Auto-bake 256x256 thumbnail
            Texture2D thumbnail = UIThumbnailBaker.BakeThumbnail(prefabAsset, cleanName);
            if (thumbnail != null)
            {
                preset.customThumbnail = thumbnail;
            }

            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string thumbPath = $"{UIThumbnailBaker.ThumbnailsFolder}/{cleanName}_thumb.png";

            Debug.Log($"[UI Pipeline] Successfully built UI component '{cleanName}' into '{prefabPath}' with thumbnail!");

            return new BuildResult
            {
                success = true,
                presetName = cleanName,
                prefabPath = prefabPath,
                assetPath = assetPath,
                thumbnailPath = thumbPath
            };
        }

        private static GameObject BuildNodeHierarchy(UINodeSpec node, Transform parent)
        {
            GameObject go = new GameObject(string.IsNullOrEmpty(node.name) ? "Node" : node.name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = go.AddComponent<RectTransform>();
            ConfigureRectTransform(rt, node);

            string type = (node.type ?? "Container").ToLowerInvariant();

            switch (type)
            {
                case "image":
                case "container":
                    ConfigureImageOrContainer(go, node);
                    break;

                case "text":
                    ConfigureText(go, node);
                    break;

                case "button":
                    ConfigureButton(go, node);
                    break;

                case "inputfield":
                case "input":
                    ConfigureInputField(go, node);
                    break;

                case "slider":
                    ConfigureSlider(go, node);
                    break;

                case "toggle":
                    ConfigureToggle(go, node);
                    break;

                default:
                    ConfigureImageOrContainer(go, node);
                    break;
            }

            // Apply layout group if specified
            if (node.layout != null && node.layout.type != "None" && !string.IsNullOrEmpty(node.layout.type))
            {
                ConfigureLayoutGroup(go, node.layout);
            }

            // Recursively build children
            if (node.children != null)
            {
                foreach (var childNode in node.children)
                {
                    if (childNode != null)
                    {
                        BuildNodeHierarchy(childNode, go.transform);
                    }
                }
            }

            return go;
        }

        private static void ConfigureRectTransform(RectTransform rt, UINodeSpec node)
        {
            if (node.anchorMin != null && node.anchorMin.Length == 2)
                rt.anchorMin = new Vector2(node.anchorMin[0], node.anchorMin[1]);
            else
                rt.anchorMin = new Vector2(0.5f, 0.5f);

            if (node.anchorMax != null && node.anchorMax.Length == 2)
                rt.anchorMax = new Vector2(node.anchorMax[0], node.anchorMax[1]);
            else
                rt.anchorMax = new Vector2(0.5f, 0.5f);

            if (node.pivot != null && node.pivot.Length == 2)
                rt.pivot = new Vector2(node.pivot[0], node.pivot[1]);
            else
                rt.pivot = new Vector2(0.5f, 0.5f);

            if (node.anchoredPosition != null && node.anchoredPosition.Length == 2)
                rt.anchoredPosition = new Vector2(node.anchoredPosition[0], node.anchoredPosition[1]);
            else
                rt.anchoredPosition = Vector2.zero;

            rt.sizeDelta = new Vector2(node.width > 0 ? node.width : 160f, node.height > 0 ? node.height : 44f);
        }

        private static void ConfigureImageOrContainer(GameObject go, UINodeSpec node)
        {
            if (!string.IsNullOrEmpty(node.backgroundColorHex))
            {
                Image img = go.AddComponent<Image>();
                img.color = ParseColor(node.backgroundColorHex, Color.white);
                img.raycastTarget = true;

                Sprite bgSprite = LoadBuiltinSprite(node.spriteResource);
                if (bgSprite != null)
                {
                    img.sprite = bgSprite;
                    img.type = Image.Type.Sliced;
                }
            }
        }

        private static void ConfigureText(GameObject go, UINodeSpec node)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = node.text ?? "Text Label";
            tmp.fontSize = node.fontSize > 0 ? node.fontSize : 14f;
            tmp.color = ParseColor(node.textColorHex, Color.white);
            tmp.alignment = ParseAlignment(node.alignment);
            tmp.fontStyle = ParseFontStyle(node.fontStyle);
            tmp.textWrappingMode = node.wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }
        }

        private static void ConfigureButton(GameObject go, UINodeSpec node)
        {
            Image bg = go.AddComponent<Image>();
            Color normal = ParseColor(node.normalColorHex, new Color(0.14f, 0.45f, 0.95f));
            bg.color = normal;
            bg.type = Image.Type.Sliced;
            Sprite sprite = LoadBuiltinSprite("UISprite");
            if (sprite != null) bg.sprite = sprite;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = normal;
            cb.highlightedColor = ParseColor(node.highlightColorHex, new Color(0.23f, 0.51f, 0.96f));
            cb.pressedColor = ParseColor(node.pressedColorHex, new Color(0.11f, 0.31f, 0.85f));
            cb.disabledColor = ParseColor(node.disabledColorHex, new Color(0.4f, 0.4f, 0.4f));
            btn.colors = cb;

            // Add label if specified
            if (!string.IsNullOrEmpty(node.text))
            {
                GameObject labelGO = new GameObject("Text (TMP)");
                labelGO.transform.SetParent(go.transform, false);

                RectTransform textRt = labelGO.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(8, 4);
                textRt.offsetMax = new Vector2(-8, -4);

                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text = node.text;
                tmp.fontSize = node.fontSize > 0 ? node.fontSize : 14f;
                tmp.fontStyle = ParseFontStyle(node.fontStyle);
                tmp.color = ParseColor(node.textColorHex, Color.white);
                tmp.alignment = TextAlignmentOptions.Center;
                if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            }
        }

        private static void ConfigureInputField(GameObject go, UINodeSpec node)
        {
            Image bg = go.AddComponent<Image>();
            bg.color = ParseColor(node.backgroundColorHex, new Color(0.14f, 0.14f, 0.16f));
            bg.type = Image.Type.Sliced;
            Sprite sprite = LoadBuiltinSprite("InputFieldBackground");
            if (sprite != null) bg.sprite = sprite;

            // Text Area
            GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(go.transform, false);
            RectTransform areaRt = textArea.GetComponent<RectTransform>();
            areaRt.anchorMin = Vector2.zero;
            areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(10, 6);
            areaRt.offsetMax = new Vector2(-10, -6);
            textArea.AddComponent<RectMask2D>();

            // Placeholder
            GameObject phGO = new GameObject("Placeholder", typeof(RectTransform));
            phGO.transform.SetParent(textArea.transform, false);
            RectTransform phRt = phGO.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;

            var phTMP = phGO.AddComponent<TextMeshProUGUI>();
            phTMP.text = node.placeholderText ?? "Enter text...";
            phTMP.fontSize = node.fontSize > 0 ? node.fontSize : 14f;
            phTMP.color = ParseColor(node.placeholderColorHex, new Color(0.6f, 0.6f, 0.6f));
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (TMP_Settings.defaultFontAsset != null) phTMP.font = TMP_Settings.defaultFontAsset;

            // Content Text
            GameObject textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(textArea.transform, false);
            RectTransform textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.text = node.text ?? "";
            textTMP.fontSize = node.fontSize > 0 ? node.fontSize : 14f;
            textTMP.color = ParseColor(node.textColorHex, Color.white);
            textTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (TMP_Settings.defaultFontAsset != null) textTMP.font = TMP_Settings.defaultFontAsset;

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = textTMP;
            input.placeholder = phTMP;
        }

        private static void ConfigureSlider(GameObject go, UINodeSpec node)
        {
            var slider = go.AddComponent<Slider>();
            slider.minValue = node.sliderMinValue;
            slider.maxValue = node.sliderMaxValue > node.sliderMinValue ? node.sliderMaxValue : 1f;
            slider.value = node.sliderValue;

            // Background track
            Image bg = go.AddComponent<Image>();
            bg.color = ParseColor(node.backgroundColorHex, new Color(0.2f, 0.22f, 0.25f));
            slider.targetGraphic = bg;

            // Fill area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = ParseColor(node.fillColorHex, new Color(0.14f, 0.45f, 0.95f));
            slider.fillRect = fillRt;
        }

        private static void ConfigureToggle(GameObject go, UINodeSpec node)
        {
            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = node.toggleState;

            // Background box
            Image bg = go.AddComponent<Image>();
            bg.color = ParseColor(node.backgroundColorHex, new Color(0.18f, 0.2f, 0.24f));
            toggle.targetGraphic = bg;

            // Checkmark
            GameObject checkmarkGO = new GameObject("Checkmark", typeof(RectTransform));
            checkmarkGO.transform.SetParent(go.transform, false);
            RectTransform checkRt = checkmarkGO.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.2f, 0.2f);
            checkRt.anchorMax = new Vector2(0.8f, 0.8f);
            checkRt.sizeDelta = Vector2.zero;

            Image checkImg = checkmarkGO.AddComponent<Image>();
            checkImg.color = ParseColor(node.checkmarkColorHex, new Color(0.14f, 0.45f, 0.95f));
            Sprite checkSprite = LoadBuiltinSprite("Checkmark");
            if (checkSprite != null) checkImg.sprite = checkSprite;

            toggle.graphic = checkImg;
        }

        private static void ConfigureLayoutGroup(GameObject go, UILayoutSpec layout)
        {
            string t = layout.type.ToLowerInvariant();
            HorizontalOrVerticalLayoutGroup lg = null;

            if (t == "horizontal")
            {
                lg = go.AddComponent<HorizontalLayoutGroup>();
            }
            else if (t == "vertical")
            {
                lg = go.AddComponent<VerticalLayoutGroup>();
            }
            else if (t == "grid")
            {
                var grid = go.AddComponent<GridLayoutGroup>();
                grid.spacing = new Vector2(layout.spacing, layout.spacing);
                grid.padding = new RectOffset(layout.paddingLeft, layout.paddingRight, layout.paddingTop, layout.paddingBottom);
                return;
            }

            if (lg != null)
            {
                lg.spacing = layout.spacing;
                lg.padding = new RectOffset(layout.paddingLeft, layout.paddingRight, layout.paddingTop, layout.paddingBottom);
                lg.childControlWidth = layout.childControlWidth;
                lg.childControlHeight = layout.childControlHeight;
                lg.childForceExpandWidth = layout.childForceExpandWidth;
                lg.childForceExpandHeight = layout.childForceExpandHeight;
            }
        }

        // =========================================================================
        // CLI & COMMAND-LINE ENTRY POINT
        // =========================================================================

        public static void BuildFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string specPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-componentSpec" && i + 1 < args.Length)
                {
                    specPath = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(specPath) || !File.Exists(specPath))
            {
                Debug.LogError("[UI Pipeline CLI] Invalid or missing -componentSpec path argument.");
                EditorApplication.Exit(1);
                return;
            }

            string json = File.ReadAllText(specPath);
            BuildResult res = BuildFromJson(json);

            if (res.success)
            {
                Debug.Log($"[UI Pipeline CLI] Success! Generated preset: {res.presetName} at {res.prefabPath}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[UI Pipeline CLI] Error: {res.error}");
                EditorApplication.Exit(2);
            }
        }

        // =========================================================================
        // UTILITIES
        // =========================================================================

        private static Color ParseColor(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private static TextAlignmentOptions ParseAlignment(string alignment)
        {
            if (string.IsNullOrEmpty(alignment)) return TextAlignmentOptions.Center;
            switch (alignment.ToLowerInvariant())
            {
                case "left":
                case "midlineleft": return TextAlignmentOptions.MidlineLeft;
                case "right":
                case "midlineright": return TextAlignmentOptions.MidlineRight;
                case "top":
                case "topcenter": return TextAlignmentOptions.Top;
                case "topleft": return TextAlignmentOptions.TopLeft;
                case "topright": return TextAlignmentOptions.TopRight;
                case "bottom": return TextAlignmentOptions.Bottom;
                case "bottomleft": return TextAlignmentOptions.BottomLeft;
                case "bottomright": return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static FontStyles ParseFontStyle(string style)
        {
            if (string.IsNullOrEmpty(style)) return FontStyles.Normal;
            switch (style.ToLowerInvariant())
            {
                case "bold": return FontStyles.Bold;
                case "italic": return FontStyles.Italic;
                case "bolditalic": return FontStyles.Bold | FontStyles.Italic;
                default: return FontStyles.Normal;
            }
        }

        private static Sprite LoadBuiltinSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            return AssetDatabase.GetBuiltinExtraResource<Sprite>($"UI/Skin/{spriteName}.psd");
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}
#endif
