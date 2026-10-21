Shader "UI/ProceduralUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite/Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Required for UGUI Stencil Mask
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;     // Gradient Start Color (with CanvasGroup alpha)
                float4 texcoord : TEXCOORD0; // xy = UV, z = TextureOpacity, w = TextureBlendMode
                float4 uv1      : TEXCOORD1; // xy = halfDimensions, zw = localCenterOffset
                float4 uv2      : TEXCOORD2; // Corner radii: x=TL, y=TR, z=BR, w=BL
                float4 uv3      : TEXCOORD3; // Packed RGBA colors: x=Border, y=GradEnd, z=DropShadow, w=InnerShadow
                float4 tangent  : TANGENT;   // x=BorderWidth, y=DropShadowSoftness, z=GradientAngle, w=ProgressAmount
                float3 normal   : NORMAL;    // x=ShadowOffsetX, y=ShadowOffsetY, z=Flags(CornerStyle, ProgType, InnerSoftness)
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float4 texcoord      : TEXCOORD0; // xy = UV, z = TextureOpacity, w = TextureBlendMode
                float2 localPos      : TEXCOORD1;
                float4 halfDimCenter : TEXCOORD2;
                float4 cornerRadii   : TEXCOORD3;
                float4 packedColors  : TEXCOORD4;
                float4 params        : TEXCOORD5; // x=BorderWidth, y=DropShadowSoftness, z=GradientAngle, w=ProgressAmount
                float3 shadowAndFlags: TEXCOORD6; // x=ShadowOffsetX, y=ShadowOffsetY, z=Flags
                float4 worldPosition : TEXCOORD7;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = v.texcoord;

                // Local position relative to the base card center
                o.localPos = v.vertex.xy - v.uv1.zw;
                o.halfDimCenter = v.uv1;
                o.cornerRadii = v.uv2;
                o.packedColors = v.uv3;
                o.params = v.tangent;
                o.shadowAndFlags = v.normal;
                return o;
            }

            // Unpacks 32-bit packed color into standard fixed4 RGBA
            fixed4 UnpackColor(float f)
            {
                uint u = asuint(f);
                return fixed4(
                    ((u >> 24) & 0xFF) / 255.0,
                    ((u >> 16) & 0xFF) / 255.0,
                    ((u >> 8)  & 0xFF) / 255.0,
                    ( u        & 0xFF) / 255.0
                );
            }

            // Evaluates SDF distance for given point, halfDimensions, radii, and style
            float sdProceduralShape(float2 p, float2 halfDim, float4 radii, int style)
            {
                // Quadrant selection:
                // x >= 0, y >= 0 -> Top-Right (radii.y)
                // x <  0, y >= 0 -> Top-Left  (radii.x)
                // x >= 0, y <  0 -> Bottom-Right (radii.z)
                // x <  0, y <  0 -> Bottom-Left  (radii.w)
                float2 rHalf;
                rHalf.x = (p.x > 0.0) ? radii.y : radii.x;
                rHalf.y = (p.x > 0.0) ? radii.z : radii.w;
                float r = (p.y >= 0.0) ? rHalf.x : rHalf.y;

                r = min(r, min(halfDim.x, halfDim.y));

                if (r <= 0.001)
                {
                    float2 q = abs(p) - halfDim;
                    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0));
                }

                if (style == 1) // Octagonal / Chamfer (45-degree cut)
                {
                    float2 q = abs(p) - halfDim;
                    return max(max(q.x, q.y), (q.x + q.y + r) * 0.70710678);
                }
                else if (style == 2) // Poly / Scooped / Inverted Round
                {
                    float2 q = abs(p) - halfDim;
                    if (q.x > -r && q.y > -r)
                    {
                        return r - length(q);
                    }
                    return max(q.x, q.y);
                }
                else // Round
                {
                    float2 q = abs(p) - halfDim + r;
                    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
                }
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 halfDim = IN.halfDimCenter.xy;
                float borderWidth = max(0.0, IN.params.x);
                float shadowSoftness = max(0.0, IN.params.y);
                float gradAngle = IN.params.z;
                float progressAmount = saturate(IN.params.w);

                float2 shadowOffset = IN.shadowAndFlags.xy;
                float rawFlags = IN.shadowAndFlags.z;

                int cornerStyle = (int)(rawFlags / 1000.0);
                float remainder = rawFlags - cornerStyle * 1000.0;
                int progressType = (int)(remainder / 100.0);
                float innerShadowSoftness = remainder - progressType * 100.0;

                // Unpack colors
                fixed4 borderColor = UnpackColor(IN.packedColors.x);
                fixed4 gradEndColor = UnpackColor(IN.packedColors.y);
                fixed4 dropShadowColor = UnpackColor(IN.packedColors.z);
                fixed4 innerShadowColor = UnpackColor(IN.packedColors.w);

                // Inherit canvas group alpha
                borderColor.a *= IN.color.a;
                gradEndColor.a *= IN.color.a;
                dropShadowColor.a *= IN.color.a;
                innerShadowColor.a *= IN.color.a;

                float eps = 0.5; // Antialiasing standard half-pixel
                float dist = sdProceduralShape(IN.localPos, halfDim, IN.cornerRadii, cornerStyle);

                // 1. Antialiased Body Mask
                float bodyAlpha = smoothstep(eps, -eps, dist);

                // 2. Drop Shadow (rendered behind the body)
                fixed4 shadowResult = fixed4(0, 0, 0, 0);
                if (shadowSoftness > 0.01 && dropShadowColor.a > 0.001)
                {
                    float shadowDist = sdProceduralShape(IN.localPos - shadowOffset, halfDim, IN.cornerRadii, cornerStyle);
                    if (shadowDist > -eps)
                    {
                        float normDist = saturate(shadowDist / shadowSoftness);
                        float shadowFactor = (1.0 - normDist) * (1.0 - normDist) * dropShadowColor.a;
                        shadowResult = fixed4(dropShadowColor.rgb, shadowFactor * (1.0 - bodyAlpha));
                    }
                }

                // 3. Fill calculation: Solid or Linear Gradient
                fixed4 fillColor;
                if (gradEndColor.a > 0.001)
                {
                    float2 dir = float2(cos(gradAngle), sin(gradAngle));
                    float proj = dot(IN.localPos, dir);
                    float maxProj = dot(halfDim, abs(dir));
                    float t = saturate((proj / max(maxProj, 0.001)) * 0.5 + 0.5);
                    fillColor = lerp(IN.color, gradEndColor, t);
                }
                else
                {
                    fillColor = IN.color;
                }

                // 4. Texture / Image sample and blending
                float texOpacity = IN.texcoord.z;
                float blendMode = IN.texcoord.w;
                if (texOpacity > 0.001)
                {
                    fixed4 texColor = (tex2D(_MainTex, IN.texcoord.xy) + _TextureSampleAdd);
                    if (blendMode > 0.5) // AlphaBlend / Overlay
                    {
                        fixed4 overlayTex = fixed4(texColor.rgb, texColor.a * texOpacity);
                        fillColor.rgb = lerp(fillColor.rgb, overlayTex.rgb, overlayTex.a);
                    }
                    else // Multiply (Standard tint)
                    {
                        fixed4 tintedTex = lerp(fixed4(1,1,1,1), texColor, texOpacity);
                        fillColor *= tintedTex;
                    }
                }

                // 5. Inner Shadow (inside the perimeter: dist < 0)
                if (innerShadowSoftness > 0.1 && innerShadowColor.a > 0.001 && dist < 0.0)
                {
                    float insetDist = -dist;
                    float innerFactor = saturate(insetDist / innerShadowSoftness);
                    float innerAlpha = (1.0 - innerFactor) * (1.0 - innerFactor) * innerShadowColor.a;
                    fillColor.rgb = lerp(fillColor.rgb, innerShadowColor.rgb, innerAlpha);
                }

                // 6. Progression / Fill Amount
                float progressMask = 1.0;
                if (progressType != 0)
                {
                    float p = 1.0;
                    if (progressType == 1) // Horizontal: left to right
                    {
                        p = saturate((IN.localPos.x + halfDim.x) / (2.0 * halfDim.x));
                    }
                    else if (progressType == 2) // Vertical: bottom to top
                    {
                        p = saturate((IN.localPos.y + halfDim.y) / (2.0 * halfDim.y));
                    }
                    else if (progressType == 3) // Radial 360 clockwise
                    {
                        float angle = atan2(IN.localPos.x, IN.localPos.y);
                        p = frac(angle / 6.283185307 + 1.0);
                    }

                    progressMask = smoothstep(0.005, -0.005, p - progressAmount);
                }

                // Apply progress mask to fill
                fillColor.a *= progressMask;

                // 7. Border / Outline (Inner stroke)
                float outlineAlpha = 0.0;
                if (borderWidth > 0.0)
                {
                    float innerEdge = smoothstep(-borderWidth - eps, -borderWidth + eps, dist);
                    float outerEdge = smoothstep(eps, -eps, dist);
                    outlineAlpha = innerEdge * outerEdge;
                }

                // Composite: Drop Shadow -> Body Fill -> Border
                fixed4 finalCol = shadowResult;
                finalCol = lerp(finalCol, fillColor, bodyAlpha * fillColor.a);

                if (borderWidth > 0.0)
                {
                    finalCol = lerp(finalCol, borderColor, outlineAlpha * borderColor.a);
                }

                #ifdef UNITY_UI_CLIP_RECT
                finalCol.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalCol.a - 0.001);
                #endif

                return finalCol;
            }
            ENDHLSL
        }
    }
}
