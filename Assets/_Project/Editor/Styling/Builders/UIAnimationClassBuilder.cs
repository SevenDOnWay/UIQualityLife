#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UIQualityLife.Styling;

namespace UIQualityLife.Editor.Styling.Builders
{
    /// <summary>
    /// Builder responsible for creating, updating, and baking waveform thumbnails for <see cref="UIAnimationClass"/>
    /// and standalone <see cref="UIMotionProfile"/> assets.
    /// Follows the Single Responsibility Principle (SRP).
    /// </summary>
    public static class UIAnimationClassBuilder
    {
        private static string AnimationsRoot => UIQualityLifeSettings.AnimationsFolder;

        /// <summary>
        /// Builds a <see cref="UIAnimationClass"/> asset from serialized AnimationBuildSpec JSON.
        /// </summary>
        public static StylingBuildResult BuildAnimationFromJson(string json)
        {
            try
            {
                var spec = JsonUtility.FromJson<AnimationBuildSpec>(json);
                if (spec == null || string.IsNullOrWhiteSpace(spec.name))
                {
                    return new StylingBuildResult { success = false, error = "Invalid animation specification: 'name' is required." };
                }

                return BuildAnimation(spec);
            }
            catch (Exception ex)
            {
                return new StylingBuildResult { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Creates or updates a <see cref="UIAnimationClass"/> asset and generates its waveform thumbnail.
        /// </summary>
        public static StylingBuildResult BuildAnimation(AnimationBuildSpec spec)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.name))
            {
                return new StylingBuildResult { success = false, error = "Invalid animation specification: 'name' is required." };
            }

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

            // 1. Entrance Motion Clip
            var entranceProp = so.FindProperty("m_EntranceClip");
            ConfigureMotionClipProperty(entranceProp, spec.entranceClip, spec, isExit: false);

            // 2. Hover Motion Clip (:hover)
            var hoverProp = so.FindProperty("m_HoverClip");
            if (spec.hoverClip != null)
            {
                ConfigureMotionClipProperty(hoverProp, spec.hoverClip, spec, isExit: false);
            }
            else if (spec.scale != null || spec.rotate3d != null || spec.slide != null)
            {
                var autoHover = new MotionClipSpec
                {
                    enabled = true,
                    duration = spec.enterDuration > 0.01f ? spec.enterDuration : 0.35f,
                    ease = "outCubic",
                    scale = spec.scale,
                    rotate = spec.rotate3d,
                    slide = spec.slide,
                    fade = spec.fade
                };
                ConfigureMotionClipProperty(hoverProp, autoHover, spec, isExit: false);
            }

            // 3. Pressed Motion Clip (:active)
            var pressedProp = so.FindProperty("m_PressedClip");
            if (spec.pressedClip != null)
            {
                ConfigureMotionClipProperty(pressedProp, spec.pressedClip, spec, isExit: false);
            }

            // 4. Exit Motion Clip
            var exitProp = so.FindProperty("m_ExitClip");
            if (spec.exitClip != null)
            {
                ConfigureMotionClipProperty(exitProp, spec.exitClip, spec, isExit: true);
            }

            // 5. Continuous Loop Clip
            var loopProp = so.FindProperty("m_LoopClip");
            ConfigureLoopClipProperty(loopProp, spec.loopClip, spec);

            // 6. Click Punch / Shake
            var punchProp = so.FindProperty("m_ClickPunch");
            ConfigureClickPunchProperty(punchProp, spec.clickPunch, spec);

            // 5. Interactive Visual States
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
                if (s.rotationEuler != null && s.rotationEuler.Length >= 3)
                {
                    elem.FindPropertyRelative("targetRotationEuler").vector3Value = new Vector3(s.rotationEuler[0], s.rotationEuler[1], s.rotationEuler[2]);
                }
                else
                {
                    elem.FindPropertyRelative("targetRotationEuler").vector3Value = new Vector3(0f, 0f, s.rotation);
                }

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

            return new StylingBuildResult
            {
                success = true,
                name = cleanName,
                assetPath = assetPath,
                thumbPath = thumbPath
            };
        }

        private static void ConfigureMotionClipProperty(SerializedProperty clipProp, MotionClipSpec clipSpec, AnimationBuildSpec rootSpec, bool isExit)
        {
            if (clipProp == null) return;

            bool hasEntrance = !isExit && (rootSpec.playOnEnable || rootSpec.slide != null || rootSpec.scale != null || rootSpec.rotate3d != null || rootSpec.fade != null || clipSpec != null);
            clipProp.FindPropertyRelative("enabled").boolValue = clipSpec != null ? clipSpec.enabled : hasEntrance;
            float dur = (clipSpec != null && clipSpec.duration > 0.01f) ? clipSpec.duration : (rootSpec.enterDuration > 0.01f ? rootSpec.enterDuration : 0.35f);
            clipProp.FindPropertyRelative("duration").floatValue = dur;
            clipProp.FindPropertyRelative("startDelay").floatValue = clipSpec != null ? clipSpec.startDelay : 0f;

            string easeStr = clipSpec != null ? clipSpec.ease : "outCubic";
            clipProp.FindPropertyRelative("easeType").enumValueIndex = (int)ParseEaseType(easeStr);
            clipProp.FindPropertyRelative("customCurve").animationCurveValue = ParseCurve(easeStr);

            // Pivot
            var pivotProp = clipProp.FindPropertyRelative("pivot");
            float[] pivArr = clipSpec?.pivot ?? rootSpec.pivot;
            if (pivArr != null && pivArr.Length >= 2)
            {
                pivotProp.FindPropertyRelative("overridePivot").boolValue = true;
                pivotProp.FindPropertyRelative("targetPivot").vector2Value = new Vector2(pivArr[0], pivArr[1]);
            }

            // Slide Channel
            var slideProp = clipProp.FindPropertyRelative("slide");
            SlideSpec slideSpec = clipSpec?.slide ?? rootSpec.slide;
            if (slideSpec != null && slideSpec.enabled)
            {
                slideProp.FindPropertyRelative("enabled").boolValue = true;
                slideProp.FindPropertyRelative("playMode").enumValueIndex = (int)(isExit ? MotionPlayMode.Exit : MotionPlayMode.Entrance);
                SlideDirection dir = SlideDirection.Custom;
                Enum.TryParse(slideSpec.direction, true, out dir);
                slideProp.FindPropertyRelative("direction").enumValueIndex = (int)dir;
                if (slideSpec.offset != null && slideSpec.offset.Length >= 2)
                {
                    slideProp.FindPropertyRelative("customOffset").vector2Value = new Vector2(slideSpec.offset[0], slideSpec.offset[1]);
                }
                slideProp.FindPropertyRelative("useScreenRelative").boolValue = slideSpec.useScreenRelative;
                ConfigureTimingOverride(slideProp.FindPropertyRelative("timing"), slideSpec.timing);
            }

            // Scale Channel
            var scaleProp = clipProp.FindPropertyRelative("scale");
            ScaleSpec scaleSpec = clipSpec?.scale ?? rootSpec.scale;
            if (scaleSpec != null && scaleSpec.enabled)
            {
                scaleProp.FindPropertyRelative("enabled").boolValue = true;
                scaleProp.FindPropertyRelative("playMode").enumValueIndex = (int)(isExit ? MotionPlayMode.Exit : MotionPlayMode.Entrance);
                if (scaleSpec.fromScale != null && scaleSpec.fromScale.Length >= 3)
                    scaleProp.FindPropertyRelative("fromScale").vector3Value = new Vector3(scaleSpec.fromScale[0], scaleSpec.fromScale[1], scaleSpec.fromScale[2]);
                else if (scaleSpec.fromScale != null && scaleSpec.fromScale.Length >= 2)
                    scaleProp.FindPropertyRelative("fromScale").vector3Value = new Vector3(scaleSpec.fromScale[0], scaleSpec.fromScale[1], 1f);

                if (scaleSpec.toScale != null && scaleSpec.toScale.Length >= 3)
                    scaleProp.FindPropertyRelative("toScale").vector3Value = new Vector3(scaleSpec.toScale[0], scaleSpec.toScale[1], scaleSpec.toScale[2]);
                else
                    scaleProp.FindPropertyRelative("toScale").vector3Value = Vector3.one;

                scaleProp.FindPropertyRelative("useIndependentAxisCurves").boolValue = scaleSpec.useIndependentAxisCurves;
                ConfigureTimingOverride(scaleProp.FindPropertyRelative("timing"), scaleSpec.timing);
            }
            else if (!isExit && rootSpec.playOnEnable)
            {
                scaleProp.FindPropertyRelative("enabled").boolValue = true;
                scaleProp.FindPropertyRelative("playMode").enumValueIndex = (int)MotionPlayMode.Entrance;
                float s0 = rootSpec.enterScaleFrom;
                scaleProp.FindPropertyRelative("fromScale").vector3Value = new Vector3(s0, s0, 1f);
                scaleProp.FindPropertyRelative("toScale").vector3Value = Vector3.one;
            }

            // Rotate Channel
            var rotProp = clipProp.FindPropertyRelative("rotate");
            RotateSpec rotSpec = clipSpec?.rotate ?? rootSpec.rotate3d;
            if (rotSpec != null && rotSpec.enabled)
            {
                rotProp.FindPropertyRelative("enabled").boolValue = true;
                rotProp.FindPropertyRelative("playMode").enumValueIndex = (int)(isExit ? MotionPlayMode.Exit : MotionPlayMode.Entrance);
                if (rotSpec.fromEuler != null && rotSpec.fromEuler.Length >= 3)
                    rotProp.FindPropertyRelative("fromEuler").vector3Value = new Vector3(rotSpec.fromEuler[0], rotSpec.fromEuler[1], rotSpec.fromEuler[2]);
                if (rotSpec.toEuler != null && rotSpec.toEuler.Length >= 3)
                    rotProp.FindPropertyRelative("toEuler").vector3Value = new Vector3(rotSpec.toEuler[0], rotSpec.toEuler[1], rotSpec.toEuler[2]);

                rotProp.FindPropertyRelative("isOscillating").boolValue = rotSpec.isOscillating;
                rotProp.FindPropertyRelative("oscillationCycles").floatValue = rotSpec.oscillationCycles > 0 ? rotSpec.oscillationCycles : 2.5f;
                rotProp.FindPropertyRelative("damping").floatValue = rotSpec.damping > 0 ? rotSpec.damping : 1.5f;
                ConfigureTimingOverride(rotProp.FindPropertyRelative("timing"), rotSpec.timing);
            }

            // Fade Channel
            var fadeProp = clipProp.FindPropertyRelative("fade");
            FadeSpec fadeSpec = clipSpec?.fade ?? rootSpec.fade;
            if (fadeSpec != null && fadeSpec.enabled)
            {
                fadeProp.FindPropertyRelative("enabled").boolValue = true;
                fadeProp.FindPropertyRelative("playMode").enumValueIndex = (int)(isExit ? MotionPlayMode.Exit : MotionPlayMode.Entrance);
                fadeProp.FindPropertyRelative("fromAlpha").floatValue = fadeSpec.fromAlpha;
                fadeProp.FindPropertyRelative("toAlpha").floatValue = fadeSpec.toAlpha;
                ConfigureTimingOverride(fadeProp.FindPropertyRelative("timing"), fadeSpec.timing);
            }
            else if (!isExit && rootSpec.playOnEnable && rootSpec.enterFadeFrom < 0.99f)
            {
                fadeProp.FindPropertyRelative("enabled").boolValue = true;
                fadeProp.FindPropertyRelative("playMode").enumValueIndex = (int)MotionPlayMode.Entrance;
                fadeProp.FindPropertyRelative("fromAlpha").floatValue = rootSpec.enterFadeFrom;
                fadeProp.FindPropertyRelative("toAlpha").floatValue = 1f;
            }
        }

        private static void ConfigureLoopClipProperty(SerializedProperty loopProp, MotionClipSpec loopSpec, AnimationBuildSpec rootSpec)
        {
            if (loopProp == null) return;

            bool isLooper = loopSpec != null ? loopSpec.enabled : (!string.IsNullOrEmpty(rootSpec.loopType) && !string.Equals(rootSpec.loopType, "None", StringComparison.OrdinalIgnoreCase));
            loopProp.FindPropertyRelative("enabled").boolValue = isLooper;
            if (!isLooper) return;

            loopProp.FindPropertyRelative("loopCount").intValue = -1; // Infinite by default
            loopProp.FindPropertyRelative("duration").floatValue = (rootSpec.loopSpeed > 0.01f) ? (1f / rootSpec.loopSpeed) : 1.5f;
            loopProp.FindPropertyRelative("loopType").enumValueIndex = (int)MotionLoopType.Yoyo;

            string loopKind = (rootSpec.loopType ?? "").ToLowerInvariant();
            if (loopKind.Contains("pulse"))
            {
                var scaleProp = loopProp.FindPropertyRelative("scale");
                scaleProp.FindPropertyRelative("enabled").boolValue = true;
                scaleProp.FindPropertyRelative("fromScale").vector3Value = Vector3.one;
                float mag = rootSpec.loopMagnitude > 0f ? rootSpec.loopMagnitude : 0.06f;
                scaleProp.FindPropertyRelative("toScale").vector3Value = Vector3.one * (1f + mag);
            }
            else if (loopKind.Contains("float") || loopKind.Contains("bounce"))
            {
                var slideProp = loopProp.FindPropertyRelative("slide");
                slideProp.FindPropertyRelative("enabled").boolValue = true;
                float mag = rootSpec.loopMagnitude > 0f ? rootSpec.loopMagnitude : 0.08f;
                slideProp.FindPropertyRelative("customOffset").vector2Value = new Vector2(0f, mag * 100f);
            }
            else if (loopKind.Contains("shimmer"))
            {
                var fadeProp = loopProp.FindPropertyRelative("fade");
                fadeProp.FindPropertyRelative("enabled").boolValue = true;
                float mag = rootSpec.loopMagnitude > 0f ? rootSpec.loopMagnitude : 0.2f;
                fadeProp.FindPropertyRelative("fromAlpha").floatValue = Mathf.Clamp01(1f - mag);
                fadeProp.FindPropertyRelative("toAlpha").floatValue = 1f;
            }
            else if (loopKind.Contains("shake") || loopKind.Contains("wiggle"))
            {
                var rotProp = loopProp.FindPropertyRelative("rotate");
                rotProp.FindPropertyRelative("enabled").boolValue = true;
                float mag = rootSpec.loopMagnitude > 0f ? rootSpec.loopMagnitude : 0.08f;
                rotProp.FindPropertyRelative("fromEuler").vector3Value = new Vector3(0f, 0f, -mag * 50f);
                rotProp.FindPropertyRelative("toEuler").vector3Value = new Vector3(0f, 0f, mag * 50f);
            }
        }

        private static void ConfigureClickPunchProperty(SerializedProperty punchProp, PunchShakeSpec punchSpec, AnimationBuildSpec rootSpec)
        {
            if (punchProp == null) return;

            PunchShakeSpec punch = punchSpec ?? rootSpec.clickPunch ?? rootSpec.punchShake;
            if (punch != null && punch.enabled)
            {
                punchProp.FindPropertyRelative("enabled").boolValue = true;
                if (punch.punchPosition != null && punch.punchPosition.Length >= 3)
                    punchProp.FindPropertyRelative("punchPosition").vector3Value = new Vector3(punch.punchPosition[0], punch.punchPosition[1], punch.punchPosition[2]);
                if (punch.punchScale != null && punch.punchScale.Length >= 3)
                    punchProp.FindPropertyRelative("punchScale").vector3Value = new Vector3(punch.punchScale[0], punch.punchScale[1], punch.punchScale[2]);

                punchProp.FindPropertyRelative("punchRotation").floatValue = punch.punchRotation;
                punchProp.FindPropertyRelative("vibrato").intValue = punch.vibrato > 0 ? punch.vibrato : 8;
                punchProp.FindPropertyRelative("elasticity").floatValue = punch.elasticity;
                punchProp.FindPropertyRelative("duration").floatValue = punch.duration > 0.01f ? punch.duration : 0.22f;
            }
            else
            {
                punchProp.FindPropertyRelative("enabled").boolValue = rootSpec.enableClickBounce;
                float mag = rootSpec.bounceMagnitude > 0f ? rootSpec.bounceMagnitude : 0.08f;
                punchProp.FindPropertyRelative("punchScale").vector3Value = new Vector3(mag, -mag * 0.5f, 0f);
                punchProp.FindPropertyRelative("duration").floatValue = rootSpec.bounceDuration > 0.01f ? rootSpec.bounceDuration : 0.22f;
                punchProp.FindPropertyRelative("vibrato").intValue = 8;
                punchProp.FindPropertyRelative("elasticity").floatValue = 0.85f;
            }
        }

        private static void ConfigureTimingOverride(SerializedProperty timingProp, TimingSpec spec)
        {
            if (timingProp == null || spec == null) return;
            timingProp.FindPropertyRelative("overrideTiming").boolValue = spec.overrideTiming;
            timingProp.FindPropertyRelative("delay").floatValue = spec.delay;
            timingProp.FindPropertyRelative("duration").floatValue = spec.duration;
            timingProp.FindPropertyRelative("easeType").enumValueIndex = (int)ParseEaseType(spec.ease);
            timingProp.FindPropertyRelative("customCurve").animationCurveValue = ParseCurve(spec.ease);
        }

        public static void BakeAnimationThumbnail(UIAnimationClass anim, string targetPath, int size = 256)
        {
            Color darkBg = new Color(0.118f, 0.125f, 0.145f, 1f);
            Color accent = new Color(0.55f, 0.34f, 0.96f, 1f); // Purple
            if (anim != null && anim.LoopClip != null && anim.LoopClip.enabled)
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
                importer.SaveAndReimport();
            }
        }

        public static UIEaseType ParseEaseType(string ease)
        {
            if (string.IsNullOrEmpty(ease)) return UIEaseType.OutQuad;
            ease = ease.ToLowerInvariant().Trim();

            if (ease == "linear") return UIEaseType.Linear;
            if (ease == "bell" || ease == "apex" || ease == "arc" || ease == "peak" || ease == "custom") return UIEaseType.CustomCurve;
            if (ease.Contains("outback") || ease == "spring") return UIEaseType.OutBack;
            if (ease.Contains("outbounce") || ease == "bounce") return UIEaseType.OutBounce;
            if (ease.Contains("outcubic") || ease == "easeout") return UIEaseType.OutCubic;
            if (ease.Contains("inoutcubic") || ease == "easeinout") return UIEaseType.InOutCubic;
            if (ease.Contains("inexpo") || ease == "easein") return UIEaseType.InExpo;

            return UIEaseType.OutQuad;
        }

        public static AnimationCurve ParseCurve(string ease)
        {
            if (string.IsNullOrEmpty(ease)) return AnimationCurve.EaseInOut(0, 0, 1, 1);
            ease = ease.ToLowerInvariant().Trim();

            if (ease == "linear") return AnimationCurve.Linear(0, 0, 1, 1);
            if (ease == "bell" || ease == "apex" || ease == "arc" || ease == "peak")
            {
                Keyframe[] keys = new Keyframe[3];
                keys[0] = new Keyframe(0f, 0f, 2f, 2f);
                keys[1] = new Keyframe(0.5f, 1f, 0f, 0f);
                keys[2] = new Keyframe(1f, 0f, -2f, -2f);
                return new AnimationCurve(keys);
            }
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
