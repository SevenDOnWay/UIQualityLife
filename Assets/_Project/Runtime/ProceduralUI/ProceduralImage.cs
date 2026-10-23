using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace UIQualityLife.ProceduralUI
{
    /// <summary>
    /// Geometric shape style for corners of a <see cref="ProceduralImage"/>.
    /// </summary>
    public enum CornerStyle
    {
        /// <summary>Standard smooth rounded corner.</summary>
        Round = 0,
        /// <summary>45-degree chamfer / cut corner style.</summary>
        Octagonal = 1,
        /// <summary>Concave / inverted round corner style.</summary>
        Scooped = 2
    }

    /// <summary>
    /// Background fill modes for a <see cref="ProceduralImage"/>.
    /// </summary>
    public enum FillMode
    {
        /// <summary>Single solid color fill.</summary>
        Solid = 0,
        /// <summary>Two-color linear gradient fill evaluated across a customizable angle.</summary>
        LinearGradient = 1
    }

    /// <summary>
    /// Progression / fill direction mode for progress bars and circular cooldowns.
    /// </summary>
    public enum ProgressType
    {
        /// <summary>No progression clipping applied (full graphic rendered).</summary>
        None = 0,
        /// <summary>Horizontal bar fill from left to right.</summary>
        Horizontal = 1,
        /// <summary>Vertical bar fill from bottom to top.</summary>
        Vertical = 2,
        /// <summary>360-degree radial clock fill starting from top center.</summary>
        Radial360 = 3
    }

    /// <summary>
    /// Scaling behavior for textures and sprites applied to a <see cref="ProceduralImage"/>.
    /// </summary>
    public enum TextureScaleMode
    {
        /// <summary>Stretches the texture to completely fill the rectangle dimensions.</summary>
        Stretch = 0,
        /// <summary>Preserves source aspect ratio with letterboxing or pillarboxing.</summary>
        Fit = 1,
        /// <summary>Preserves source aspect ratio and scales to cover entire bounds, cropping overflow.</summary>
        Fill = 2,
        /// <summary>Repeats the texture across the rectangle according to <see cref="ProceduralImage.uvRect"/>.</summary>
        Tile = 3
    }

    /// <summary>
    /// Blending mode between applied texture and background fill color/gradient.
    /// </summary>
    public enum TextureBlendMode
    {
        /// <summary>Multiplies texture pixel colors with background fill color or gradient.</summary>
        Multiply = 0,
        /// <summary>Alpha-blends texture on top of background fill color or gradient.</summary>
        AlphaBlend = 1
    }

    /// <summary>
    /// Advanced Procedural UI graphic inheriting directly from UnityEngine.UI.Image.
    /// Drop-in compatible with GetComponent&lt;Image&gt;(), standard Button targetGraphic,
    /// and tween libraries (e.g. DOTween DOColor, DOFillAmount).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Procedural UI/Procedural Image")]
    public class ProceduralImage : Image
    {
        // --- Image / Texture Settings ---
        [SerializeField] private Texture m_Texture;
        [SerializeField] private TextureScaleMode m_ScaleMode = TextureScaleMode.Stretch;
        [SerializeField] private TextureBlendMode m_TextureBlendMode = TextureBlendMode.Multiply;
        [Range(0f, 1f)]
        [SerializeField] private float m_TextureOpacity = 1f;
        [SerializeField] private Rect m_UVRect = new Rect(0, 0, 1, 1);

        // --- Corner Settings ---
        [SerializeField] private CornerStyle m_CornerStyle = CornerStyle.Round;
        [SerializeField] private bool m_UniformCorners = true;
        [SerializeField] private float m_UniformRadius = 16f;
        [SerializeField] private Vector4 m_CornerRadii = new Vector4(16f, 16f, 16f, 16f); // x: TL, y: TR, z: BR, w: BL

        // --- Fill Settings ---
        [SerializeField] private FillMode m_FillMode = FillMode.Solid;
        [SerializeField] private Color m_GradientEndColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        [Range(0f, 360f)]
        [SerializeField] private float m_GradientAngle = 0f;

        // --- Border Settings ---
        [SerializeField] private float m_BorderWidth = 0f;
        [SerializeField] private Color m_BorderColor = new Color(1f, 1f, 1f, 0.5f);

        // --- Drop Shadow (Outer) ---
        [SerializeField] private float m_DropShadowSoftness = 0f;
        [SerializeField] private Vector2 m_DropShadowOffset = new Vector2(0f, -4f);
        [SerializeField] private Color m_DropShadowColor = new Color(0f, 0f, 0f, 0.5f);

        // --- Inner Shadow ---
        [SerializeField] private float m_InnerShadowSoftness = 0f;
        [SerializeField] private Color m_InnerShadowColor = new Color(0f, 0f, 0f, 0.4f);

        // --- Progression / Fill ---
        [SerializeField] private ProgressType m_ProgressType = ProgressType.None;
        [Range(0f, 1f)]
        [SerializeField] private float m_ProgressAmount = 1f;

        // --- Raycast ---
        [SerializeField] private bool m_RaycastPrecise = true;

        private static Material s_DefaultMaterial;

        #region Standard Image Overrides

        /// <summary>
        /// Gets or sets an optional raw texture to render instead of the standard sprite.
        /// When modified at runtime, marks UI vertices and material dirty.
        /// </summary>
        public Texture customTexture
        {
            get => m_Texture;
            set { if (m_Texture != value) { m_Texture = value; SetVerticesDirty(); SetMaterialDirty(); } }
        }

        /// <summary>
        /// Active main texture used by the CanvasRenderer (returns customTexture if set, otherwise sprite texture).
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (m_Texture != null) return m_Texture;
                return base.mainTexture;
            }
        }

        /// <summary>
        /// Default material with procedural shader ("UI/ProceduralUI") used to render corners, shadows, and gradients.
        /// </summary>
        public override Material defaultMaterial
        {
            get
            {
                if (s_DefaultMaterial == null)
                {
                    Shader shader = Shader.Find("UI/ProceduralUI");
                    if (shader != null)
                    {
                        s_DefaultMaterial = new Material(shader)
                        {
                            name = "ProceduralUI-Default",
                            hideFlags = HideFlags.DontSaveInEditor
                        };
                    }
                }
                return s_DefaultMaterial ?? base.defaultMaterial;
            }
        }

        /// <summary>
        /// Gets or sets whether aspect ratio is preserved (maps to TextureScaleMode.Fit vs TextureScaleMode.Stretch).
        /// </summary>
        public new bool preserveAspect
        {
            get => m_ScaleMode == TextureScaleMode.Fit;
            set
            {
                base.preserveAspect = value;
                m_ScaleMode = value ? TextureScaleMode.Fit : TextureScaleMode.Stretch;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Progress fill amount in normalized range [0, 1]. Compatible with DOTween DOFillAmount.
        /// Automatically switches ProgressType to Horizontal if set to None.
        /// </summary>
        public new float fillAmount
        {
            get => m_ProgressAmount;
            set
            {
                base.fillAmount = value;
                m_ProgressAmount = Mathf.Clamp01(value);
                if (m_ProgressType == ProgressType.None)
                {
                    m_ProgressType = ProgressType.Horizontal;
                }
                SetVerticesDirty();
            }
        }

        #endregion

        #region Procedural Properties

        /// <summary>
        /// Scaling behavior for texture/sprite rendering (Stretch, Fit, Fill, or Tile).
        /// </summary>
        public TextureScaleMode scaleMode
        {
            get => m_ScaleMode;
            set { if (m_ScaleMode != value) { m_ScaleMode = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// Blend mode between texture and fill color/gradient (Multiply or AlphaBlend).
        /// </summary>
        public TextureBlendMode textureBlendMode
        {
            get => m_TextureBlendMode;
            set { if (m_TextureBlendMode != value) { m_TextureBlendMode = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// Opacity multiplier for texture layer in range [0, 1].
        /// </summary>
        public float textureOpacity
        {
            get => m_TextureOpacity;
            set { m_TextureOpacity = Mathf.Clamp01(value); SetVerticesDirty(); }
        }

        /// <summary>
        /// UV coordinate rectangle used when scaleMode is set to <see cref="TextureScaleMode.Tile"/>.
        /// </summary>
        public Rect uvRect
        {
            get => m_UVRect;
            set { m_UVRect = value; SetVerticesDirty(); }
        }

        /// <summary>
        /// Geometric style applied to the corners (Round, Octagonal, or Scooped).
        /// </summary>
        public CornerStyle cornerStyle
        {
            get => m_CornerStyle;
            set { if (m_CornerStyle != value) { m_CornerStyle = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// If true, all four corners share the single <see cref="uniformRadius"/> value.
        /// If false, individual corners are governed by <see cref="cornerRadii"/>.
        /// </summary>
        public bool uniformCorners
        {
            get => m_UniformCorners;
            set { if (m_UniformCorners != value) { m_UniformCorners = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// Uniform corner radius in pixels for all four corners.
        /// </summary>
        public float uniformRadius
        {
            get => m_UniformRadius;
            set
            {
                m_UniformRadius = Mathf.Max(0f, value);
                if (m_UniformCorners)
                {
                    m_CornerRadii = new Vector4(m_UniformRadius, m_UniformRadius, m_UniformRadius, m_UniformRadius);
                    SetVerticesDirty();
                }
            }
        }

        /// <summary>
        /// Independent corner radii in pixels.
        /// Vector4 components represent: X = Top-Left, Y = Top-Right, Z = Bottom-Right, W = Bottom-Left.
        /// </summary>
        public Vector4 cornerRadii
        {
            get => m_CornerRadii;
            set
            {
                m_CornerRadii = new Vector4(
                    Mathf.Max(0f, value.x),
                    Mathf.Max(0f, value.y),
                    Mathf.Max(0f, value.z),
                    Mathf.Max(0f, value.w)
                );
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Fill mode: Solid color or LinearGradient.
        /// </summary>
        public FillMode fillMode
        {
            get => m_FillMode;
            set { if (m_FillMode != value) { m_FillMode = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// Target secondary color when <see cref="fillMode"/> is set to <see cref="FillMode.LinearGradient"/>.
        /// </summary>
        public Color gradientEndColor
        {
            get => m_GradientEndColor;
            set { if (m_GradientEndColor != value) { m_GradientEndColor = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// Angle of the linear gradient in degrees [0, 360].
        /// </summary>
        public float gradientAngle
        {
            get => m_GradientAngle;
            set { if (m_GradientAngle != value) { m_GradientAngle = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// Width of outline border stroke in pixels.
        /// </summary>
        public float borderWidth
        {
            get => m_BorderWidth;
            set { m_BorderWidth = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        /// <summary>
        /// Outline border stroke color.
        /// </summary>
        public Color borderColor
        {
            get => m_BorderColor;
            set { m_BorderColor = value; SetVerticesDirty(); }
        }

        /// <summary>
        /// Outer drop shadow softness / blur radius in pixels.
        /// </summary>
        public float dropShadowSoftness
        {
            get => m_DropShadowSoftness;
            set { m_DropShadowSoftness = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        /// <summary>
        /// Alias for <see cref="dropShadowSoftness"/>. Useful for outer neon glows.
        /// </summary>
        public float glowSoftness
        {
            get => dropShadowSoftness;
            set => dropShadowSoftness = value;
        }

        /// <summary>
        /// Outer drop shadow directional offset in pixels.
        /// </summary>
        public Vector2 dropShadowOffset
        {
            get => m_DropShadowOffset;
            set { m_DropShadowOffset = value; SetVerticesDirty(); }
        }

        /// <summary>
        /// Color and opacity of the outer drop shadow.
        /// </summary>
        public Color dropShadowColor
        {
            get => m_DropShadowColor;
            set { m_DropShadowColor = value; SetVerticesDirty(); }
        }

        /// <summary>
        /// Alias for <see cref="dropShadowColor"/>. Useful for outer neon glows.
        /// </summary>
        public Color glowColor
        {
            get => dropShadowColor;
            set => dropShadowColor = value;
        }

        /// <summary>
        /// Softness / depth of inner inset shadow in pixels.
        /// </summary>
        public float innerShadowSoftness
        {
            get => m_InnerShadowSoftness;
            set { m_InnerShadowSoftness = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        /// <summary>
        /// Color and opacity of inner inset shadow.
        /// </summary>
        public Color innerShadowColor
        {
            get => m_InnerShadowColor;
            set { m_InnerShadowColor = value; SetVerticesDirty(); }
        }

        /// <summary>
        /// Progression clipping mode (None, Horizontal, Vertical, Radial360).
        /// </summary>
        public ProgressType progressType
        {
            get => m_ProgressType;
            set { if (m_ProgressType != value) { m_ProgressType = value; SetVerticesDirty(); } }
        }

        /// <summary>
        /// Progress completion fraction in normalized range [0, 1].
        /// </summary>
        public float progressAmount
        {
            get => m_ProgressAmount;
            set
            {
                m_ProgressAmount = Mathf.Clamp01(value);
                base.fillAmount = m_ProgressAmount;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// If true, performs mathematical SDF hit testing to ensure clicks outside rounded/chamfered corners pass through.
        /// </summary>
        public bool raycastPrecise
        {
            get => m_RaycastPrecise;
            set => m_RaycastPrecise = value;
        }

        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureCanvasChannels();
            SetVerticesDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_BorderWidth = Mathf.Max(0f, m_BorderWidth);
            m_DropShadowSoftness = Mathf.Max(0f, m_DropShadowSoftness);
            m_InnerShadowSoftness = Mathf.Clamp(m_InnerShadowSoftness, 0f, 99f);
            m_ProgressAmount = Mathf.Clamp01(m_ProgressAmount);
            m_TextureOpacity = Mathf.Clamp01(m_TextureOpacity);

            if (m_UniformCorners)
            {
                m_UniformRadius = Mathf.Max(0f, m_UniformRadius);
                m_CornerRadii = new Vector4(m_UniformRadius, m_UniformRadius, m_UniformRadius, m_UniformRadius);
            }
            else
            {
                m_CornerRadii.x = Mathf.Max(0f, m_CornerRadii.x);
                m_CornerRadii.y = Mathf.Max(0f, m_CornerRadii.y);
                m_CornerRadii.z = Mathf.Max(0f, m_CornerRadii.z);
                m_CornerRadii.w = Mathf.Max(0f, m_CornerRadii.w);
            }

            EnsureCanvasChannels();
            SetVerticesDirty();
        }
#endif

        /// <summary>
        /// Inspects the parent <see cref="Canvas"/> and enables required vertex shader channels
        /// (TexCoord1, TexCoord2, TexCoord3, Tangent, Normal) for procedural corner and gradient rendering.
        /// Call this method if instantiating or reparenting a <see cref="ProceduralImage"/> via script at runtime.
        /// </summary>
        public void EnsureCanvasChannels()
        {
            Canvas c = canvas;
            if (c == null) c = GetComponentInParent<Canvas>();
            if (c == null) return;

            AdditionalCanvasShaderChannels required =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.TexCoord2 |
                AdditionalCanvasShaderChannels.TexCoord3 |
                AdditionalCanvasShaderChannels.Tangent |
                AdditionalCanvasShaderChannels.Normal;

            if ((c.additionalShaderChannels & required) != required)
            {
                c.additionalShaderChannels |= required;
            }
        }

        private static float PackColor(Color col)
        {
            byte r = (byte)(Mathf.Clamp01(col.r) * 255f);
            byte g = (byte)(Mathf.Clamp01(col.g) * 255f);
            byte b = (byte)(Mathf.Clamp01(col.b) * 255f);
            byte a = (byte)(Mathf.Clamp01(col.a) * 255f);
            uint packed = ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | (uint)a;
            return BitConverter.ToSingle(BitConverter.GetBytes(packed), 0);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            float padX = Mathf.Max(0f, m_DropShadowSoftness + Mathf.Abs(m_DropShadowOffset.x));
            float padY = Mathf.Max(0f, m_DropShadowSoftness + Mathf.Abs(m_DropShadowOffset.y));
            float pad = Mathf.Max(padX, padY);

            float xMin = rect.xMin - pad;
            float xMax = rect.xMax + pad;
            float yMin = rect.yMin - pad;
            float yMax = rect.yMax + pad;

            // Texture / Sprite UV calculation with ScaleMode
            bool hasTexture = (sprite != null || m_Texture != null);
            float uStart = 0f, uEnd = 1f, vStart = 0f, vEnd = 1f;
            float imgW = rect.width, imgH = rect.height;

            if (sprite != null)
            {
                Vector4 outer = DataUtility.GetOuterUV(sprite);
                uStart = outer.x; vStart = outer.y; uEnd = outer.z; vEnd = outer.w;
                imgW = sprite.rect.width;
                imgH = sprite.rect.height;
            }
            else if (m_Texture != null)
            {
                imgW = m_Texture.width;
                imgH = m_Texture.height;
            }

            if (hasTexture && imgW > 0f && imgH > 0f)
            {
                float cardAspect = rect.width / rect.height;
                float imgAspect = imgW / imgH;

                if (m_ScaleMode == TextureScaleMode.Fit)
                {
                    if (imgAspect > cardAspect)
                    {
                        float scaleY = cardAspect / imgAspect;
                        float vRange = (vEnd - vStart) / scaleY;
                        float vMid = (vStart + vEnd) * 0.5f;
                        vStart = vMid - vRange * 0.5f;
                        vEnd = vMid + vRange * 0.5f;
                    }
                    else
                    {
                        float scaleX = imgAspect / cardAspect;
                        float uRange = (uEnd - uStart) / scaleX;
                        float uMid = (uStart + uEnd) * 0.5f;
                        uStart = uMid - uRange * 0.5f;
                        uEnd = uMid + uRange * 0.5f;
                    }
                }
                else if (m_ScaleMode == TextureScaleMode.Fill)
                {
                    if (imgAspect > cardAspect)
                    {
                        float scaleX = cardAspect / imgAspect;
                        float uRange = (uEnd - uStart) * scaleX;
                        float uMid = (uStart + uEnd) * 0.5f;
                        uStart = uMid - uRange * 0.5f;
                        uEnd = uMid + uRange * 0.5f;
                    }
                    else
                    {
                        float scaleY = imgAspect / cardAspect;
                        float vRange = (vEnd - vStart) * scaleY;
                        float vMid = (vStart + vEnd) * 0.5f;
                        vStart = vMid - vRange * 0.5f;
                        vEnd = vMid + vRange * 0.5f;
                    }
                }
                else if (m_ScaleMode == TextureScaleMode.Tile)
                {
                    uStart = m_UVRect.x;
                    uEnd = m_UVRect.x + m_UVRect.width * (rect.width / imgW);
                    vStart = m_UVRect.y;
                    vEnd = m_UVRect.y + m_UVRect.height * (rect.height / imgH);
                }
            }

            // Extrapolate UVs beyond padding margin
            float uvMinX = (pad > 0f) ? Mathf.LerpUnclamped(uStart, uEnd, -pad / rect.width) : uStart;
            float uvMaxX = (pad > 0f) ? Mathf.LerpUnclamped(uStart, uEnd, (rect.width + pad) / rect.width) : uEnd;
            float uvMinY = (pad > 0f) ? Mathf.LerpUnclamped(vStart, vEnd, -pad / rect.height) : vStart;
            float uvMaxY = (pad > 0f) ? Mathf.LerpUnclamped(vStart, vEnd, (rect.height + pad) / rect.height) : vEnd;

            Vector4 effectiveRadii = m_UniformCorners
                ? new Vector4(m_UniformRadius, m_UniformRadius, m_UniformRadius, m_UniformRadius)
                : m_CornerRadii;

            Vector4 halfDimCenter = new Vector4(rect.width * 0.5f, rect.height * 0.5f, rect.center.x, rect.center.y);
            Vector4 cornerRadiiStream = effectiveRadii;

            float packedBorder = PackColor(m_BorderColor);
            float packedGradEnd = (m_FillMode == FillMode.LinearGradient) ? PackColor(m_GradientEndColor) : 0f;
            float packedDropShadow = PackColor(m_DropShadowColor);
            float packedInnerShadow = PackColor(m_InnerShadowColor);
            Vector4 packedColors = new Vector4(packedBorder, packedGradEnd, packedDropShadow, packedInnerShadow);

            float effectiveProgress = m_ProgressAmount;
            int effectiveProgressType = (int)m_ProgressType;

            // If a script or tween set standard Image.fillAmount on an Image reference
            if (base.fillAmount < 0.999f && m_ProgressType == ProgressType.None)
            {
                effectiveProgress = base.fillAmount;
                effectiveProgressType = (int)ProgressType.Horizontal;
            }

            Vector4 tangentStream = new Vector4(
                m_BorderWidth,
                m_DropShadowSoftness,
                m_GradientAngle * Mathf.Deg2Rad,
                effectiveProgress
            );

            float packedFlags = (int)m_CornerStyle * 1000f + effectiveProgressType * 100f + Mathf.Clamp(m_InnerShadowSoftness, 0f, 99f);
            Vector3 normalStream = new Vector3(m_DropShadowOffset.x, m_DropShadowOffset.y, packedFlags);

            float effectiveTexOpacity = hasTexture ? m_TextureOpacity : 0f;
            float effectiveBlendMode = (float)m_TextureBlendMode;

            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;
            vert.uv1 = halfDimCenter;
            vert.uv2 = cornerRadiiStream;
            vert.uv3 = packedColors;
            vert.tangent = tangentStream;
            vert.normal = normalStream;

            // 0: Bottom-Left
            vert.position = new Vector3(xMin, yMin, 0f);
            vert.uv0 = new Vector4(uvMinX, uvMinY, effectiveTexOpacity, effectiveBlendMode);
            vh.AddVert(vert);

            // 1: Top-Left
            vert.position = new Vector3(xMin, yMax, 0f);
            vert.uv0 = new Vector4(uvMinX, uvMaxY, effectiveTexOpacity, effectiveBlendMode);
            vh.AddVert(vert);

            // 2: Top-Right
            vert.position = new Vector3(xMax, yMax, 0f);
            vert.uv0 = new Vector4(uvMaxX, uvMaxY, effectiveTexOpacity, effectiveBlendMode);
            vh.AddVert(vert);

            // 3: Bottom-Right
            vert.position = new Vector3(xMax, yMin, 0f);
            vert.uv0 = new Vector4(uvMaxX, uvMinY, effectiveTexOpacity, effectiveBlendMode);
            vh.AddVert(vert);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        /// <summary>
        /// Validates raycast hit location against exact signed distance field (SDF) geometry.
        /// When <see cref="raycastPrecise"/> is true, pointer clicks outside rounded/chamfered/scooped corners are rejected.
        /// </summary>
        /// <param name="screenPoint">Screen coordinate of pointer event.</param>
        /// <param name="eventCamera">Camera associated with the event.</param>
        /// <returns>True if the point lies inside the visible geometric bounds; otherwise false.</returns>
        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!m_RaycastPrecise)
            {
                return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera);
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            Vector2 halfDim = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            Vector2 p = localPoint - rect.center;

            Vector4 radii = m_UniformCorners
                ? new Vector4(m_UniformRadius, m_UniformRadius, m_UniformRadius, m_UniformRadius)
                : m_CornerRadii;

            float r;
            if (p.x >= 0f)
                r = (p.y >= 0f) ? radii.y : radii.z;
            else
                r = (p.y >= 0f) ? radii.x : radii.w;

            r = Mathf.Min(r, Mathf.Min(halfDim.x, halfDim.y));

            float dist;
            if (r <= 0.001f)
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x) - halfDim.x, Mathf.Abs(p.y) - halfDim.y);
                dist = Mathf.Min(Mathf.Max(q.x, q.y), 0f) + Vector2.Max(q, Vector2.zero).magnitude;
            }
            else if (m_CornerStyle == CornerStyle.Octagonal)
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x) - halfDim.x, Mathf.Abs(p.y) - halfDim.y);
                dist = Mathf.Max(Mathf.Max(q.x, q.y), (q.x + q.y + r) * 0.70710678f);
            }
            else if (m_CornerStyle == CornerStyle.Scooped)
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x) - halfDim.x, Mathf.Abs(p.y) - halfDim.y);
                if (q.x > -r && q.y > -r)
                    dist = r - q.magnitude;
                else
                    dist = Mathf.Max(q.x, q.y);
            }
            else // Round
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x) - halfDim.x + r, Mathf.Abs(p.y) - halfDim.y + r);
                dist = Mathf.Min(Mathf.Max(q.x, q.y), 0f) + Vector2.Max(q, Vector2.zero).magnitude - r;
            }

            return dist <= 0f;
        }
    }
}
