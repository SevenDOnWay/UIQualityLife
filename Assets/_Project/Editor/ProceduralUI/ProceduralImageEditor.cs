using UnityEditor;
using UnityEngine;
using UIQualityLife.ProceduralUI;

namespace UIQualityLife.Editor.ProceduralUI
{
    [CustomEditor(typeof(ProceduralImage))]
    [CanEditMultipleObjects]
    public class ProceduralImageEditor : UnityEditor.Editor
    {
        private SerializedProperty m_SpriteProp;
        private SerializedProperty m_TextureProp;
        private SerializedProperty m_ScaleModeProp;
        private SerializedProperty m_TextureBlendModeProp;
        private SerializedProperty m_TextureOpacityProp;
        private SerializedProperty m_UVRectProp;

        private SerializedProperty m_ColorProp;
        private SerializedProperty m_RaycastTargetProp;
        private SerializedProperty m_RaycastPreciseProp;

        private SerializedProperty m_CornerStyleProp;
        private SerializedProperty m_UniformCornersProp;
        private SerializedProperty m_UniformRadiusProp;
        private SerializedProperty m_CornerRadiiProp;

        private SerializedProperty m_FillModeProp;
        private SerializedProperty m_GradientEndColorProp;
        private SerializedProperty m_GradientAngleProp;

        private SerializedProperty m_BorderWidthProp;
        private SerializedProperty m_BorderColorProp;

        private SerializedProperty m_DropShadowSoftnessProp;
        private SerializedProperty m_DropShadowOffsetProp;
        private SerializedProperty m_DropShadowColorProp;

        private SerializedProperty m_InnerShadowSoftnessProp;
        private SerializedProperty m_InnerShadowColorProp;

        private SerializedProperty m_ProgressTypeProp;
        private SerializedProperty m_ProgressAmountProp;

        private void OnEnable()
        {
            m_SpriteProp = serializedObject.FindProperty("m_Sprite");
            m_TextureProp = serializedObject.FindProperty("m_Texture");
            m_ScaleModeProp = serializedObject.FindProperty("m_ScaleMode");
            m_TextureBlendModeProp = serializedObject.FindProperty("m_TextureBlendMode");
            m_TextureOpacityProp = serializedObject.FindProperty("m_TextureOpacity");
            m_UVRectProp = serializedObject.FindProperty("m_UVRect");

            m_ColorProp = serializedObject.FindProperty("m_Color");
            m_RaycastTargetProp = serializedObject.FindProperty("m_RaycastTarget");
            m_RaycastPreciseProp = serializedObject.FindProperty("m_RaycastPrecise");

            m_CornerStyleProp = serializedObject.FindProperty("m_CornerStyle");
            m_UniformCornersProp = serializedObject.FindProperty("m_UniformCorners");
            m_UniformRadiusProp = serializedObject.FindProperty("m_UniformRadius");
            m_CornerRadiiProp = serializedObject.FindProperty("m_CornerRadii");

            m_FillModeProp = serializedObject.FindProperty("m_FillMode");
            m_GradientEndColorProp = serializedObject.FindProperty("m_GradientEndColor");
            m_GradientAngleProp = serializedObject.FindProperty("m_GradientAngle");

            m_BorderWidthProp = serializedObject.FindProperty("m_BorderWidth");
            m_BorderColorProp = serializedObject.FindProperty("m_BorderColor");

            m_DropShadowSoftnessProp = serializedObject.FindProperty("m_DropShadowSoftness");
            m_DropShadowOffsetProp = serializedObject.FindProperty("m_DropShadowOffset");
            m_DropShadowColorProp = serializedObject.FindProperty("m_DropShadowColor");

            m_InnerShadowSoftnessProp = serializedObject.FindProperty("m_InnerShadowSoftness");
            m_InnerShadowColorProp = serializedObject.FindProperty("m_InnerShadowColor");

            m_ProgressTypeProp = serializedObject.FindProperty("m_ProgressType");
            m_ProgressAmountProp = serializedObject.FindProperty("m_ProgressAmount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var img = (ProceduralImage)target;

            // Canvas Status Bar
            DrawCanvasStatus(img);

            // Top Quick Toolbar: Expand / Collapse All
            DrawQuickToolbar();
            EditorGUILayout.Space(2);

            // 1. Fill & Background Section
            string fillBadge = (m_FillModeProp.enumValueIndex == (int)FillMode.Solid) ? "Solid" : $"Gradient {m_GradientAngleProp.floatValue:0}°";
            if (DrawSectionHeader("Fill", "Fill & Background", fillBadge))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(m_FillModeProp, new GUIContent("Fill Mode"));

                    if (m_FillModeProp.enumValueIndex == (int)FillMode.Solid)
                    {
                        EditorGUILayout.PropertyField(m_ColorProp, new GUIContent("Color"));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(m_ColorProp, new GUIContent("Start Color"));
                        EditorGUILayout.PropertyField(m_GradientEndColorProp, new GUIContent("End Color"));
                        EditorGUILayout.Slider(m_GradientAngleProp, 0f, 360f, new GUIContent("Gradient Angle (°)"));
                    }

                    EditorGUILayout.PropertyField(m_RaycastTargetProp, new GUIContent("Raycast Target"));
                    if (m_RaycastTargetProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(m_RaycastPreciseProp, new GUIContent("SDF Precision Hit", "Passes clicks through rounded/chamfered cutouts and outer drop shadow."));
                        EditorGUI.indentLevel--;
                    }
                }
            }
            EditorGUILayout.Space(2);

            // 2. Corners Section
            string cornerBadge;
            if (m_UniformCornersProp.boolValue)
                cornerBadge = $"{m_CornerStyleProp.enumDisplayNames[m_CornerStyleProp.enumValueIndex]} ({m_UniformRadiusProp.floatValue:0}px)";
            else
                cornerBadge = $"{m_CornerStyleProp.enumDisplayNames[m_CornerStyleProp.enumValueIndex]} (Custom)";

            if (DrawSectionHeader("Corners", "Corners", cornerBadge))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(m_CornerStyleProp, new GUIContent("Corner Style"));
                    EditorGUILayout.PropertyField(m_UniformCornersProp, new GUIContent("Uniform Radius"));

                    if (m_UniformCornersProp.boolValue)
                    {
                        EditorGUILayout.Slider(m_UniformRadiusProp, 0f, 150f, new GUIContent("Radius (px)"));
                    }
                    else
                    {
                        Vector4 radii = m_CornerRadiiProp.vector4Value;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Top-Left", GUILayout.Width(70));
                        radii.x = EditorGUILayout.FloatField(Mathf.Max(0f, radii.x));
                        EditorGUILayout.LabelField("Top-Right", GUILayout.Width(70));
                        radii.y = EditorGUILayout.FloatField(Mathf.Max(0f, radii.y));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Bottom-Left", GUILayout.Width(70));
                        radii.w = EditorGUILayout.FloatField(Mathf.Max(0f, radii.w));
                        EditorGUILayout.LabelField("Bottom-Right", GUILayout.Width(70));
                        radii.z = EditorGUILayout.FloatField(Mathf.Max(0f, radii.z));
                        EditorGUILayout.EndHorizontal();

                        m_CornerRadiiProp.vector4Value = radii;
                    }
                }
            }
            EditorGUILayout.Space(2);

            // 3. Image / Texture Section
            bool hasImage = (m_SpriteProp.objectReferenceValue != null || m_TextureProp.objectReferenceValue != null);
            string imgBadge = hasImage ? $"{m_ScaleModeProp.enumDisplayNames[m_ScaleModeProp.enumValueIndex]}" : "None";
            if (DrawSectionHeader("Texture", "Image / Texture", imgBadge))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(m_SpriteProp, new GUIContent("Sprite (2D/UI)"));
                    EditorGUILayout.PropertyField(m_TextureProp, new GUIContent("Texture / RenderTex"));

                    if (hasImage)
                    {
                        EditorGUILayout.PropertyField(m_ScaleModeProp, new GUIContent("Scale Mode"));
                        if (m_ScaleModeProp.enumValueIndex == (int)TextureScaleMode.Tile)
                        {
                            EditorGUILayout.PropertyField(m_UVRectProp, new GUIContent("UV Tiling / Offset"));
                        }
                        EditorGUILayout.PropertyField(m_TextureBlendModeProp, new GUIContent("Blend Mode", "Multiply tints with background; AlphaBlend overlays on top."));
                        EditorGUILayout.Slider(m_TextureOpacityProp, 0f, 1f, new GUIContent("Texture Opacity"));
                    }
                }
            }
            EditorGUILayout.Space(2);

            // 4. Border / Outline Section
            string borderBadge = (m_BorderWidthProp.floatValue > 0f) ? $"{m_BorderWidthProp.floatValue:0.#} px" : "Off";
            if (DrawSectionHeader("Border", "Border / Outline", borderBadge))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.Slider(m_BorderWidthProp, 0f, 32f, new GUIContent("Border Width (px)"));
                    if (m_BorderWidthProp.floatValue > 0f)
                    {
                        EditorGUILayout.PropertyField(m_BorderColorProp, new GUIContent("Border Color"));
                    }
                }
            }
            EditorGUILayout.Space(2);

            // 5. Shadows & Depth Section
            bool hasDrop = m_DropShadowSoftnessProp.floatValue > 0f;
            bool hasInner = m_InnerShadowSoftnessProp.floatValue > 0f;
            string shadowBadge = (hasDrop && hasInner) ? "Drop + Inner" : (hasDrop ? "Drop" : (hasInner ? "Inner" : "Off"));
            if (DrawSectionHeader("Shadows", "Shadows & Depth", shadowBadge))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Outer Drop Shadow", EditorStyles.miniBoldLabel);
                    EditorGUILayout.Slider(m_DropShadowSoftnessProp, 0f, 64f, new GUIContent("Blur / Softness (px)"));
                    if (m_DropShadowSoftnessProp.floatValue > 0f)
                    {
                        EditorGUILayout.PropertyField(m_DropShadowOffsetProp, new GUIContent("Offset (X, Y)"));
                        EditorGUILayout.PropertyField(m_DropShadowColorProp, new GUIContent("Shadow Color"));
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Inner Shadow (Vignette)", EditorStyles.miniBoldLabel);
                    EditorGUILayout.Slider(m_InnerShadowSoftnessProp, 0f, 48f, new GUIContent("Inner Softness (px)"));
                    if (m_InnerShadowSoftnessProp.floatValue > 0f)
                    {
                        EditorGUILayout.PropertyField(m_InnerShadowColorProp, new GUIContent("Inner Color"));
                    }
                }
            }
            EditorGUILayout.Space(2);

            // 6. Progression / Gauge Section
            string progressBadge = (m_ProgressTypeProp.enumValueIndex != (int)ProgressType.None)
                ? $"{m_ProgressTypeProp.enumDisplayNames[m_ProgressTypeProp.enumValueIndex]} ({m_ProgressAmountProp.floatValue * 100f:0}%)"
                : "Off";
            if (DrawSectionHeader("Progress", "Progression / Gauge", progressBadge))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(m_ProgressTypeProp, new GUIContent("Progress Type"));
                    if (m_ProgressTypeProp.enumValueIndex != (int)ProgressType.None)
                    {
                        EditorGUILayout.Slider(m_ProgressAmountProp, 0f, 1f, new GUIContent("Fill Amount"));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool DrawSectionHeader(string key, string title, string badge = null)
        {
            string prefKey = "ProcUI_Fold_" + key;
            bool expanded = EditorPrefs.GetBool(prefKey, true);

            var rect = GUILayoutUtility.GetRect(18f, 24f, GUILayout.ExpandWidth(true));

            // Background bar styling
            Color barColor = EditorGUIUtility.isProSkin
                ? (expanded ? new Color(0.24f, 0.27f, 0.32f, 1f) : new Color(0.19f, 0.20f, 0.24f, 1f))
                : (expanded ? new Color(0.78f, 0.81f, 0.86f, 1f) : new Color(0.85f, 0.87f, 0.90f, 1f));
            EditorGUI.DrawRect(rect, barColor);

            // Foldout arrow & label
            var foldoutRect = new Rect(rect.x + 4f, rect.y + 3f, rect.width - 8f, 18f);
            bool newExpanded = EditorGUI.Foldout(foldoutRect, expanded, "  " + title, true, EditorStyles.boldLabel);

            // Status Badge on the right
            if (!string.IsNullOrEmpty(badge))
            {
                var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(0.55f, 0.78f, 1.0f, 0.85f) },
                    fontStyle = FontStyle.Bold
                };
                var badgeRect = new Rect(rect.xMax - 130f, rect.y + 3f, 120f, 18f);
                EditorGUI.LabelField(badgeRect, badge, badgeStyle);
            }

            if (newExpanded != expanded)
            {
                EditorPrefs.SetBool(prefKey, newExpanded);
                expanded = newExpanded;
            }

            return expanded;
        }

        private void DrawQuickToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Expand All", EditorStyles.miniButtonLeft, GUILayout.Width(75)))
            {
                SetAllSections(true);
            }
            if (GUILayout.Button("Collapse All", EditorStyles.miniButtonRight, GUILayout.Width(80)))
            {
                SetAllSections(false);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void SetAllSections(bool state)
        {
            string[] keys = { "Fill", "Corners", "Texture", "Border", "Shadows", "Progress" };
            foreach (var k in keys)
            {
                EditorPrefs.SetBool("ProcUI_Fold_" + k, state);
            }
        }

        private void DrawCanvasStatus(ProceduralImage img)
        {
            Canvas canvas = img.canvas;
            if (canvas == null) canvas = img.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            AdditionalCanvasShaderChannels required =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.TexCoord2 |
                AdditionalCanvasShaderChannels.TexCoord3 |
                AdditionalCanvasShaderChannels.Tangent |
                AdditionalCanvasShaderChannels.Normal;

            bool isReady = (canvas.additionalShaderChannels & required) == required;

            if (!isReady)
            {
                EditorGUILayout.HelpBox("Parent Canvas requires additional vertex shader channels for Procedural UI batching.", MessageType.Warning);
                if (GUILayout.Button("Enable Vertex Shader Channels"))
                {
                    Undo.RecordObject(canvas, "Enable Shader Channels");
                    canvas.additionalShaderChannels |= required;
                    EditorUtility.SetDirty(canvas);
                }
                EditorGUILayout.Space(2);
            }
        }
    }
}
