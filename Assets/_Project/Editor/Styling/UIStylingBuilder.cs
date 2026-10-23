#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;
using UIQualityLife.Styling.Editor;

namespace UIQualityLife.Editor
{
    [Serializable]
    public class ColorTokenSpec
    {
        public string id;
        public string hex;
        public string description;
    }

    [Serializable]
    public class FloatTokenSpec
    {
        public string id;
        public float value;
        public string description;
    }

    [Serializable]
    public class ThemeBuildSpec
    {
        public string name;
        public string description;
        public List<ColorTokenSpec> colorTokens = new List<ColorTokenSpec>();
        public List<FloatTokenSpec> floatTokens = new List<FloatTokenSpec>();
    }

    [Serializable]
    public class VisualStateSpec
    {
        public string stateName = "Normal";
        public bool overrideScale;
        public float[] scale; // [x, y, z]
        public bool overrideOffset;
        public float[] offset; // [x, y]
        public bool overrideRotation;
        public float rotation;
        public bool overrideColor;
        public string colorMultiplierHex;
        public bool overrideAlpha;
        public float alpha = 1f;
        public float duration = 0.15f;
        public string ease = "easeInOut"; // "linear", "easeInOut", "easeOut", "bounce", "spring"
    }

    [Serializable]
    public class AnimationBuildSpec
    {
        public string name;
        public string className;
        public string description;
        public List<VisualStateSpec> states = new List<VisualStateSpec>();
        public bool enableClickBounce = true;
        public float bounceMagnitude = 0.08f;
        public float bounceDuration = 0.22f;
        public string loopType = "None"; // "None", "Pulse", "Float", "Shimmer", "Shake"
        public float loopSpeed = 1.5f;
        public float loopMagnitude = 0.05f;
        public bool playOnEnable = false;
        public float enterScaleFrom = 0.85f;
        public float enterFadeFrom = 0.0f;
        public float enterDuration = 0.25f;
    }

    [Serializable]
    public class StyleBuildSpec
    {
        public string name;
        public string className;
        public string description;
        public string colorToken = "primary";
        public string borderColorToken = "border";
        public string gradientEndColorToken = "";
        public string radiusToken = "radius-md";
        public string borderWidthToken = "border-thin";
        public string animationClassName;
    }

    [Serializable]
    public class BuildResult
    {
        public bool success;
        public string name;
        public string assetPath;
        public string thumbPath;
        public string error;
    }

    /// <summary>
    /// Automated builder for Theme Palettes, Animation Classes, and Style Classes.
    /// Can be invoked directly via Official Unity CLI eval, HTTP Bridge, or file queue.
    /// </summary>
    public static class UIStylingBuilder
    {
        private static string ThemesRoot => UIQualityLifeSettings.ThemesFolder;
        private static string AnimationsRoot => UIQualityLifeSettings.AnimationsFolder;
        private static string StylesRoot => UIQualityLifeSettings.StylesFolder;

        public static BuildResult BuildThemeFromJson(string json)
        {
            try
            {
                var spec = JsonUtility.FromJson<ThemeBuildSpec>(json);
                if (spec == null || string.IsNullOrWhiteSpace(spec.name))
                {
                    return new BuildResult { success = false, error = "Invalid theme specification: 'name' is required." };
                }

                return BuildTheme(spec);
            }
            catch (Exception ex)
            {
                return new BuildResult { success = false, error = ex.Message };
            }
        }

        public static BuildResult BuildTheme(ThemeBuildSpec spec)
        {
            string cleanName = SanitizeName(spec.name);
            string folder = $"{ThemesRoot}/{cleanName}";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string assetPath = $"{folder}/{cleanName}.asset";
            var palette = AssetDatabase.LoadAssetAtPath<UIThemePalette>(assetPath);
            bool isNew = palette == null;

            if (isNew)
            {
                palette = ScriptableObject.CreateInstance<UIThemePalette>();
                palette.name = cleanName;
            }

            var so = new SerializedObject(palette);
            so.FindProperty("m_ThemeName").stringValue = !string.IsNullOrEmpty(spec.name) ? spec.name : cleanName;
            if (!string.IsNullOrEmpty(spec.description))
            {
                so.FindProperty("m_Description").stringValue = spec.description;
            }
            so.ApplyModifiedProperties();

            // Populate color tokens
            if (spec.colorTokens != null)
            {
                foreach (var ct in spec.colorTokens)
                {
                    if (string.IsNullOrWhiteSpace(ct.id)) continue;
                    Color c = Color.white;
                    if (!string.IsNullOrEmpty(ct.hex))
                    {
                        ColorUtility.TryParseHtmlString(ct.hex.StartsWith("#") ? ct.hex : "#" + ct.hex, out c);
                    }
                    palette.SetColor(ct.id.Trim(), c);
                }
            }

            // Populate float tokens
            if (spec.floatTokens != null)
            {
                foreach (var ft in spec.floatTokens)
                {
                    if (string.IsNullOrWhiteSpace(ft.id)) continue;
                    palette.SetFloat(ft.id.Trim(), ft.value);
                }
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(palette, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(palette);
            }

            AssetDatabase.SaveAssets();

            // Bake Palette Thumbnail (Square dynamic grid up to 64 colors)
            var thumb = UIThemeThumbnailBaker.BakePaletteThumbnail(palette, 256);
            string thumbPath = $"{folder}/{cleanName}_thumb.png";

            AssetDatabase.Refresh();

            return new BuildResult
            {
                success = true,
                name = cleanName,
                assetPath = assetPath,
                thumbPath = thumbPath
            };
        }

        public static BuildResult BuildAnimationFromJson(string json)
        {
            try
            {
                var spec = JsonUtility.FromJson<AnimationBuildSpec>(json);
                if (spec == null || string.IsNullOrWhiteSpace(spec.name))
                {
                    return new BuildResult { success = false, error = "Invalid animation specification: 'name' is required." };
                }

                return BuildAnimation(spec);
            }
            catch (Exception ex)
            {
                return new BuildResult { success = false, error = ex.Message };
            }
        }

        public static BuildResult BuildAnimation(AnimationBuildSpec spec)
        {
            string cleanName = SanitizeName(spec.name);
            string folder = $"{AnimationsRoot}/{cleanName}";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string assetPath = $"{folder}/{cleanName}.asset";
            var anim = AssetDatabase.LoadAssetAtPath<UIAnimationClass>(assetPath);
            bool isNew = anim == null;

            if (isNew)
            {
                anim = ScriptableObject.CreateInstance<UIAnimationClass>();
                anim.name = cleanName;
            }

            var so = new SerializedObject(anim);
            string clsName = !string.IsNullOrEmpty(spec.className) 
                ? (spec.className.StartsWith(".") ? spec.className : "." + spec.className)
                : $".{cleanName.ToLowerInvariant()}";

            so.FindProperty("m_ClassName").stringValue = clsName;
            if (!string.IsNullOrEmpty(spec.description))
            {
                so.FindProperty("m_Description").stringValue = spec.description;
            }

            so.FindProperty("m_EnableClickBounce").boolValue = spec.enableClickBounce;
            so.FindProperty("m_BounceMagnitude").floatValue = spec.bounceMagnitude;
            so.FindProperty("m_BounceDuration").floatValue = spec.bounceDuration;

            // Parse loop type
            UILoopAnimationType loopEnum = UILoopAnimationType.None;
            if (!string.IsNullOrEmpty(spec.loopType))
            {
                Enum.TryParse(spec.loopType, true, out loopEnum);
            }
            so.FindProperty("m_LoopType").enumValueIndex = (int)loopEnum;
            so.FindProperty("m_LoopSpeed").floatValue = spec.loopSpeed;
            so.FindProperty("m_LoopMagnitude").floatValue = spec.loopMagnitude;

            so.FindProperty("m_PlayOnEnable").boolValue = spec.playOnEnable;
            so.FindProperty("m_EnterScaleFrom").floatValue = spec.enterScaleFrom;
            so.FindProperty("m_EnterFadeFrom").floatValue = spec.enterFadeFrom;
            so.FindProperty("m_EnterDuration").floatValue = spec.enterDuration;

            // States property list
            var statesProp = so.FindProperty("m_States");
            statesProp.ClearArray();

            var statesList = spec.states;
            if (statesList == null || statesList.Count == 0)
            {
                statesList = GetDefaultInteractiveStates();
            }

            for (int i = 0; i < statesList.Count; i++)
            {
                var s = statesList[i];
                statesProp.InsertArrayElementAtIndex(i);
                var elem = statesProp.GetArrayElementAtIndex(i);

                elem.FindPropertyRelative("stateName").stringValue = s.stateName;
                elem.FindPropertyRelative("overrideScale").boolValue = s.overrideScale;
                if (s.scale != null && s.scale.Length >= 2)
                {
                    elem.FindPropertyRelative("targetScale").vector3Value = new Vector3(
                        s.scale[0],
                        s.scale[1],
                        s.scale.Length > 2 ? s.scale[2] : 1f
                    );
                }

                elem.FindPropertyRelative("overrideOffset").boolValue = s.overrideOffset;
                if (s.offset != null && s.offset.Length >= 2)
                {
                    elem.FindPropertyRelative("targetOffset").vector2Value = new Vector2(s.offset[0], s.offset[1]);
                }

                elem.FindPropertyRelative("overrideRotation").boolValue = s.overrideRotation;
                elem.FindPropertyRelative("targetRotation").floatValue = s.rotation;

                elem.FindPropertyRelative("overrideColor").boolValue = s.overrideColor;
                if (!string.IsNullOrEmpty(s.colorMultiplierHex))
                {
                    if (ColorUtility.TryParseHtmlString(s.colorMultiplierHex.StartsWith("#") ? s.colorMultiplierHex : "#" + s.colorMultiplierHex, out Color c))
                    {
                        elem.FindPropertyRelative("colorMultiplier").colorValue = c;
                    }
                }

                elem.FindPropertyRelative("overrideAlpha").boolValue = s.overrideAlpha;
                elem.FindPropertyRelative("targetAlpha").floatValue = s.alpha;

                elem.FindPropertyRelative("duration").floatValue = s.duration > 0.01f ? s.duration : 0.15f;
                elem.FindPropertyRelative("easeType").enumValueIndex = (int)ParseEaseType(s.ease);
                elem.FindPropertyRelative("curve").animationCurveValue = ParseCurve(s.ease);
            }

            so.ApplyModifiedProperties();

            if (isNew)
            {
                AssetDatabase.CreateAsset(anim, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(anim);
            }

            AssetDatabase.SaveAssets();

            // Create animation thumbnail inside the folder
            string thumbPath = $"{folder}/{cleanName}_thumb.png";
            BakeAnimationThumbnail(anim, thumbPath);

            AssetDatabase.Refresh();

            return new BuildResult
            {
                success = true,
                name = cleanName,
                assetPath = assetPath,
                thumbPath = thumbPath
            };
        }

        public static BuildResult BuildStyleFromJson(string json)
        {
            try
            {
                var spec = JsonUtility.FromJson<StyleBuildSpec>(json);
                if (spec == null || string.IsNullOrWhiteSpace(spec.name))
                {
                    return new BuildResult { success = false, error = "Invalid style specification: 'name' is required." };
                }

                return BuildStyle(spec);
            }
            catch (Exception ex)
            {
                return new BuildResult { success = false, error = ex.Message };
            }
        }

        public static BuildResult BuildStyle(StyleBuildSpec spec)
        {
            string cleanName = SanitizeName(spec.name);
            string folder = $"{StylesRoot}/{cleanName}";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string assetPath = $"{folder}/{cleanName}.asset";
            var style = AssetDatabase.LoadAssetAtPath<UIStyleClass>(assetPath);
            bool isNew = style == null;

            if (isNew)
            {
                style = ScriptableObject.CreateInstance<UIStyleClass>();
                style.name = cleanName;
            }

            var so = new SerializedObject(style);
            string cls = !string.IsNullOrEmpty(spec.className) 
                ? (spec.className.StartsWith(".") ? spec.className : "." + spec.className)
                : $".{cleanName.ToLowerInvariant()}";

            so.FindProperty("m_ClassName").stringValue = cls;
            if (!string.IsNullOrEmpty(spec.description)) so.FindProperty("m_Description").stringValue = spec.description;
            if (!string.IsNullOrEmpty(spec.colorToken)) so.FindProperty("m_ColorToken").stringValue = spec.colorToken;
            if (!string.IsNullOrEmpty(spec.borderColorToken)) so.FindProperty("m_BorderColorToken").stringValue = spec.borderColorToken;
            if (!string.IsNullOrEmpty(spec.gradientEndColorToken)) so.FindProperty("m_GradientEndColorToken").stringValue = spec.gradientEndColorToken;
            if (!string.IsNullOrEmpty(spec.radiusToken)) so.FindProperty("m_RadiusToken").stringValue = spec.radiusToken;
            if (!string.IsNullOrEmpty(spec.borderWidthToken)) so.FindProperty("m_BorderWidthToken").stringValue = spec.borderWidthToken;

            // Link Animation Class if provided
            if (!string.IsNullOrEmpty(spec.animationClassName))
            {
                var anim = FindAnimationClassByName(spec.animationClassName);
                if (anim != null)
                {
                    so.FindProperty("m_AnimationClass").objectReferenceValue = anim;
                }
            }

            so.ApplyModifiedProperties();

            if (isNew)
            {
                AssetDatabase.CreateAsset(style, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(style);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new BuildResult
            {
                success = true,
                name = cleanName,
                assetPath = assetPath
            };
        }

        private static UIAnimationClass FindAnimationClassByName(string name)
        {
            string[] guids = AssetDatabase.FindAssets("t:UIAnimationClass");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var a = AssetDatabase.LoadAssetAtPath<UIAnimationClass>(path);
                if (a != null && (string.Equals(a.name, name, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(a.ClassName, name, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(a.ClassName, "." + name, StringComparison.OrdinalIgnoreCase)))
                {
                    return a;
                }
            }
            return null;
        }

        private static void BakeAnimationThumbnail(UIAnimationClass anim, string targetPath, int size = 256)
        {
            // Create dedicated waveform thumbnail for this animation class
            Color darkBg = new Color(0.118f, 0.125f, 0.145f, 1f);
            Color accent = new Color(0.55f, 0.34f, 0.96f, 1f); // Purple
            if (anim.LoopType != UILoopAnimationType.None)
            {
                accent = new Color(0.06f, 0.73f, 0.51f, 1f); // Emerald for loopers
            }

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++) pixels[i] = darkBg;

            float centerY = size * 0.5f;
            float amplitude = size * 0.22f;
            float waveStart = size * 0.2f;
            float waveEnd = size * 0.8f;
            float thickness = 7f;

            for (int x = (int)waveStart; x <= (int)waveEnd; x++)
            {
                float t = (x - waveStart) / (waveEnd - waveStart);
                float yVal = centerY + Mathf.Sin(t * Mathf.PI * 2f) * amplitude;

                int minY = Mathf.Clamp((int)(yVal - thickness), 0, size - 1);
                int maxY = Mathf.Clamp((int)(yVal + thickness), 0, size - 1);

                for (int y = minY; y <= maxY; y++)
                {
                    float dist = Mathf.Abs(y - yVal);
                    float alpha = Mathf.Clamp01(1f - (dist / thickness));
                    pixels[y * size + x] = Color.Lerp(darkBg, accent, alpha);
                }
            }

            int dotCenterX = (int)(waveStart + (waveEnd - waveStart) * 0.25f);
            int dotCenterY = (int)(centerY + amplitude);
            int dotRadius = 14;

            for (int y = dotCenterY - dotRadius; y <= dotCenterY + dotRadius; y++)
            {
                for (int x = dotCenterX - dotRadius; x <= dotCenterX + dotRadius; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(dotCenterX, dotCenterY)) <= dotRadius)
                        {
                            pixels[y * size + x] = Color.white;
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            File.WriteAllBytes(targetPath, png);
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(targetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.GUI;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        public static UIEaseType ParseEaseType(string ease)
        {
            if (string.IsNullOrEmpty(ease)) return UIEaseType.OutQuad;
            string e = ease.ToLowerInvariant().Trim();

            switch (e)
            {
                case "linear": return UIEaseType.Linear;
                case "easein": case "insine": return UIEaseType.InSine;
                case "easeout": case "outsine": return UIEaseType.OutSine;
                case "easeinout": case "inoutsine": return UIEaseType.InOutSine;
                case "inquad": return UIEaseType.InQuad;
                case "outquad": return UIEaseType.OutQuad;
                case "inoutquad": return UIEaseType.InOutQuad;
                case "incubic": return UIEaseType.InCubic;
                case "outcubic": return UIEaseType.OutCubic;
                case "inoutcubic": return UIEaseType.InOutCubic;
                case "inquart": return UIEaseType.InQuart;
                case "outquart": return UIEaseType.OutQuart;
                case "inoutquart": return UIEaseType.InOutQuart;
                case "inquint": return UIEaseType.InQuint;
                case "outquint": return UIEaseType.OutQuint;
                case "inoutquint": return UIEaseType.InOutQuint;
                case "inexpo": return UIEaseType.InExpo;
                case "outexpo": return UIEaseType.OutExpo;
                case "inoutexpo": return UIEaseType.InOutExpo;
                case "incirc": return UIEaseType.InCirc;
                case "outcirc": return UIEaseType.OutCirc;
                case "inoutcirc": return UIEaseType.InOutCirc;
                case "inback": return UIEaseType.InBack;
                case "outback": case "back": case "spring": return UIEaseType.OutBack;
                case "inoutback": return UIEaseType.InOutBack;
                case "inelastic": return UIEaseType.InElastic;
                case "outelastic": case "elastic": return UIEaseType.OutElastic;
                case "inoutelastic": return UIEaseType.InOutElastic;
                case "inbounce": return UIEaseType.InBounce;
                case "outbounce": case "bounce": return UIEaseType.OutBounce;
                case "inoutbounce": return UIEaseType.InOutBounce;
                case "custom": case "customcurve": return UIEaseType.CustomCurve;
                default: return UIEaseType.OutQuad;
            }
        }

        private static AnimationCurve ParseCurve(string ease)
        {
            if (string.IsNullOrEmpty(ease)) return AnimationCurve.EaseInOut(0, 0, 1, 1);
            ease = ease.ToLowerInvariant().Trim();

            if (ease == "linear") return AnimationCurve.Linear(0, 0, 1, 1);
            if (ease == "easeout")
            {
                Keyframe k0 = new Keyframe(0f, 0f, 2f, 2f);
                Keyframe k1 = new Keyframe(1f, 1f, 0f, 0f);
                return new AnimationCurve(k0, k1);
            }
            if (ease == "easein")
            {
                Keyframe k0 = new Keyframe(0f, 0f, 0f, 0f);
                Keyframe k1 = new Keyframe(1f, 1f, 2f, 2f);
                return new AnimationCurve(k0, k1);
            }
            if (ease == "bounce" || ease == "spring")
            {
                Keyframe[] keys = new Keyframe[4];
                keys[0] = new Keyframe(0f, 0f, 3f, 3f);
                keys[1] = new Keyframe(0.6f, 1.15f, 0f, 0f);
                keys[2] = new Keyframe(0.85f, 0.95f, 0f, 0f);
                keys[3] = new Keyframe(1f, 1f, 0f, 0f);
                return new AnimationCurve(keys);
            }

            return AnimationCurve.EaseInOut(0, 0, 1, 1);
        }

        private static List<VisualStateSpec> GetDefaultInteractiveStates()
        {
            return new List<VisualStateSpec>
            {
                new VisualStateSpec { stateName = "Normal", duration = 0.15f },
                new VisualStateSpec
                {
                    stateName = "Hover",
                    overrideScale = true,
                    scale = new float[] { 1.05f, 1.05f, 1f },
                    overrideOffset = true,
                    offset = new float[] { 0f, 2f },
                    overrideColor = true,
                    colorMultiplierHex = "#ffffff",
                    duration = 0.15f,
                    ease = "easeInOut"
                },
                new VisualStateSpec
                {
                    stateName = "Pressed",
                    overrideScale = true,
                    scale = new float[] { 0.95f, 0.95f, 1f },
                    overrideOffset = true,
                    offset = new float[] { 0f, -2f },
                    duration = 0.08f,
                    ease = "easeInOut"
                },
                new VisualStateSpec
                {
                    stateName = "Focused",
                    overrideScale = true,
                    scale = new float[] { 1.03f, 1.03f, 1f },
                    duration = 0.12f,
                    ease = "easeInOut"
                },
                new VisualStateSpec
                {
                    stateName = "Disabled",
                    overrideAlpha = true,
                    alpha = 0.45f,
                    overrideScale = true,
                    scale = new float[] { 0.98f, 0.98f, 1f },
                    duration = 0.15f,
                    ease = "easeInOut"
                }
            };
        }

        private static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Unnamed";
            char[] arr = raw.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (!char.IsLetterOrDigit(arr[i]) && arr[i] != '_' && arr[i] != '-')
                {
                    arr[i] = '_';
                }
            }
            return new string(arr).Trim('_');
        }
    }
}
#endif
