Shader "Custom/GridFloor"
{
    Properties
    {
        _GridColor    ("Couleur grille",       Color)  = (0.20, 0.50, 1.00, 1.0)
        _BgColor      ("Couleur fond sol",     Color)  = (0.03, 0.04, 0.07, 1.0)
        _GridScale    ("Echelle grille",       Float)  = 12.0
        _LineWidth    ("Epaisseur lignes",     Range(0.01, 0.2)) = 0.04
        _GlowRadius   ("Rayon lueur centre",  Float)  = 6.0
        _GlowStrength ("Force lueur",         Range(0, 3)) = 1.2
        _FadeRadius   ("Rayon de fondu bord", Float)  = 10.0
        _EmissionMult ("Multiplicateur émission", Float) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float3 world : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _GridColor, _BgColor;
            float  _GridScale, _LineWidth, _GlowRadius, _GlowStrength, _FadeRadius, _EmissionMult;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.world.xz * _GridScale;

                // Lignes de grille via fract
                float2 f = abs(frac(uv) - 0.5);
                float  gridLine = 1.0 - smoothstep(_LineWidth - 0.01, _LineWidth + 0.01,
                                                   min(f.x, f.y));

                // Atténuation radiale depuis l'origine
                float dist   = length(i.world.xz);
                float fade   = 1.0 - smoothstep(_FadeRadius * 0.5, _FadeRadius, dist);

                // Lueur centre
                float glow   = exp(-dist * dist / (_GlowRadius * _GlowRadius));

                fixed4 col = lerp(_BgColor, _GridColor * _EmissionMult, gridLine * fade);
                col.rgb   += _GridColor.rgb * glow * _GlowStrength * 0.3;

                return col;
            }
            ENDCG
        }
    }
}
