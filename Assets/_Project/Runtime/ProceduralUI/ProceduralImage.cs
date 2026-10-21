using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

namespace UIQualityLife.ProceduralUI
{
    public enum CornerStyle
    {
        Round = 0,
        Octagonal = 1,      // 45-degree chamfer / cut corner
        Scooped = 2         // Concave / inverted round
    }

    public enum FillMode
    {
        Solid = 0,
        LinearGradient = 1
    }

    public enum ProgressType
    {
        None = 0,
        Horizontal = 1,     // Left to right
        Vertical = 2,       // Bottom to top
        Radial360 = 3       // Clockwise circle
    }

    public enum TextureScaleMode
    {
        Stretch = 0,        // Fit rectangle directly
        Fit = 1,            // Preserve aspect ratio (letterbox / pillarbox)
        Fill = 2,           // Cover rectangle (crop overflow)
        Tile = 3            // Repeat / pattern tiling
    }

    public enum TextureBlendMode
    {
        Multiply = 0,       // Tints texture with background fill/gradient
        AlphaBlend = 1      // Overlays texture on top of fill color/gradient
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

        public Texture customTexture
        {
            get => m_Texture;
            set { if (m_Texture != value) { m_Texture = value; SetVerticesDirty(); SetMaterialDirty(); } }
        }

        public override Texture mainTexture
        {
            get
            {
                if (m_Texture != null) return m_Texture;
                return base.mainTexture;
            }
        }

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

        public TextureScaleMode scaleMode
        {
            get => m_ScaleMode;
            set { if (m_ScaleMode != value) { m_ScaleMode = value; SetVerticesDirty(); } }
        }

        public TextureBlendMode textureBlendMode
        {
            get => m_TextureBlendMode;
            set { if (m_TextureBlendMode != value) { m_TextureBlendMode = value; SetVerticesDirty(); } }
        }

        public float textureOpacity
        {
            get => m_TextureOpacity;
            set { m_TextureOpacity = Mathf.Clamp01(value); SetVerticesDirty(); }
        }

        public Rect uvRect
        {
            get => m_UVRect;
            set { m_UVRect = value; SetVerticesDirty(); }
        }

        public CornerStyle cornerStyle
        {
            get => m_CornerStyle;
            set { if (m_CornerStyle != value) { m_CornerStyle = value; SetVerticesDirty(); } }
        }

        public bool uniformCorners
        {
            get => m_UniformCorners;
            set { if (m_UniformCorners != value) { m_UniformCorners = value; SetVerticesDirty(); } }
        }

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

        public FillMode fillMode
        {
            get => m_FillMode;
            set { if (m_FillMode != value) { m_FillMode = value; SetVerticesDirty(); } }
        }

        public Color gradientEndColor
        {
            get => m_GradientEndColor;
            set { if (m_GradientEndColor != value) { m_GradientEndColor = value; SetVerticesDirty(); } }
        }

        public float gradientAngle
        {
            get => m_GradientAngle;
            set { if (m_GradientAngle != value) { m_GradientAngle = value; SetVerticesDirty(); } }
        }

        public float borderWidth
        {
            get => m_BorderWidth;
            set { m_BorderWidth = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public Color borderColor
        {
            get => m_BorderColor;
            set { m_BorderColor = value; SetVerticesDirty(); }
        }

        public float dropShadowSoftness
        {
            get => m_DropShadowSoftness;
            set { m_DropShadowSoftness = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public float glowSoftness
        {
            get => dropShadowSoftness;
            set => dropShadowSoftness = value;
        }

        public Vector2 dropShadowOffset
        {
            get => m_DropShadowOffset;
            set { m_DropShadowOffset = value; SetVerticesDirty(); }
        }

        public Color dropShadowColor
        {
            get => m_DropShadowColor;
            set { m_DropShadowColor = value; SetVerticesDirty(); }
        }

        public Color glowColor
        {
            get => dropShadowColor;
            set => dropShadowColor = value;
        }

        public float innerShadowSoftness
        {
            get => m_InnerShadowSoftness;
            set { m_InnerShadowSoftness = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public Color innerShadowColor
        {
            get => m_InnerShadowColor;
            set { m_InnerShadowColor = value; SetVerticesDirty(); }
        }

        public ProgressType progressType
        {
            get => m_ProgressType;
            set { if (m_ProgressType != value) { m_ProgressType = value; SetVerticesDirty(); } }
        }

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
