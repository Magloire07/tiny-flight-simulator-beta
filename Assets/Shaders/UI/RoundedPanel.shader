Shader "UI/RoundedPanel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        // Couleurs du dégradé vertical
        _ColorTop    ("Couleur haut",  Color) = (0.10, 0.11, 0.16, 0.97)
        _ColorBottom ("Couleur bas",   Color) = (0.05, 0.06, 0.10, 0.97)

        // Bordure intérieure lumineuse
        _BorderColor ("Couleur bordure", Color) = (0.25, 0.50, 0.90, 0.60)
        _BorderWidth ("Epaisseur bordure (px)", Float) = 1.5

        // Rayon des coins en UV (0 = carré, 0.5 = cercle)
        _Radius ("Rayon coins", Range(0.0, 0.5)) = 0.06

        // Lueur intérieure subtile (vignette inversée)
        _GlowStrength ("Force lueur centre", Range(0.0, 1.0)) = 0.12

        // Stencil (obligatoire pour UI)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil     ("Stencil ID",         Float) = 0
        _StencilOp   ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask   ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Stencil
        {
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
            ReadMask  [_StencilReadMask]
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _ColorTop;
            fixed4    _ColorBottom;
            fixed4    _BorderColor;
            float     _BorderWidth;
            float     _Radius;
            float     _GlowStrength;
            float4    _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.uv       = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color    = v.color;
                o.worldPos = v.vertex;
                return o;
            }

            // Signed-distance function d'un rectangle à coins arrondis
            // p  : coordonnée UV centrée (−0.5..0.5)
            // b  : demi-dimensions (0.5, 0.5) moins le rayon
            // r  : rayon
            float sdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV centrées
                float2 uv = i.uv - 0.5;

                // Aspect ratio : on adapte le rayon pour qu'il soit visuellement
                // identique en X et Y (suppose 16:9 ; ajustez si besoin)
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uvScaled = float2(uv.x, uv.y);

                float r = _Radius;
                float dist = sdRoundBox(uvScaled, float2(0.5 - r, 0.5 - r), r);

                // Anti-aliasing doux sur le bord
                float alpha = 1.0 - smoothstep(-0.004, 0.004, dist);

                // Dégradé vertical
                float t = i.uv.y;                        // 0 = bas, 1 = haut
                fixed4 col = lerp(_ColorBottom, _ColorTop, t);

                // Lueur centrale subtile (vignette inversée)
                float vignette = 1.0 - dot(uv * 1.6, uv * 1.6);
                vignette = saturate(vignette);
                col.rgb += vignette * _GlowStrength * 0.15;

                // Bordure intérieure lumineuse
                // On calcule un SDF légèrement rentré pour la bordure
                float bw = _BorderWidth * 0.001; // conversion approximative UV
                float distInner = sdRoundBox(uvScaled, float2(0.5 - r - bw, 0.5 - r - bw), r);
                float border = smoothstep(-0.003, 0.003, distInner)
                             - smoothstep(-0.003, 0.003, dist);
                border = saturate(border);
                col.rgb = lerp(col.rgb, _BorderColor.rgb, border * _BorderColor.a);

                // Stencil / clipping Unity UI
                col.a *= alpha * UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                col.a *= i.color.a;

                return col;
            }
            ENDCG
        }
    }
}
