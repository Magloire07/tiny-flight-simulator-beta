Shader "Custom/VolumetricClouds"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CloudDensity ("Cloud Density", Range(0, 2)) = 0.5
        _CloudScale ("Cloud Scale", Range(0.1, 10)) = 1.0
        _CloudSpeed ("Cloud Speed", Range(0, 5)) = 0.5
        _CloudHeight ("Cloud Height", Range(100, 5000)) = 1000
        _CloudThickness ("Cloud Thickness", Range(100, 2000)) = 500
        _RaySteps ("Ray Steps", Range(10, 100)) = 50
        _LightAbsorption ("Light Absorption", Range(0, 1)) = 0.3
        _SunColor ("Sun Color", Color) = (1, 0.95, 0.8, 1)
        _CloudColor ("Cloud Color", Color) = (0.9, 0.9, 0.95, 1)
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewVector : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _CloudDensity;
            float _CloudScale;
            float _CloudSpeed;
            float _CloudHeight;
            float _CloudThickness;
            float _RaySteps;
            float _LightAbsorption;
            float4 _SunColor;
            float4 _CloudColor;
            float3 _CameraPos;
            float3 _SunDirection;
            
            // Noise 3D simple
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                         lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                         lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y),
                    f.z);
            }
            
            // FBM (Fractional Brownian Motion) pour des nuages plus réalistes
            float fbm(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise3D(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            // Fonction de densité des nuages
            float cloudDensity(float3 p)
            {
                // Mouvement des nuages avec le temps
                p.xz += _Time.y * _CloudSpeed * 10.0;
                
                // Limitation verticale (couche de nuages)
                float heightFactor = 1.0 - abs(p.y - _CloudHeight) / (_CloudThickness * 0.5);
                heightFactor = saturate(heightFactor);
                
                if(heightFactor <= 0.0) return 0.0;
                
                // Génération des nuages avec FBM
                float density = fbm(p * _CloudScale * 0.001);
                density = smoothstep(0.3, 0.7, density);
                
                return density * heightFactor * _CloudDensity;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Calculer le vecteur de vue pour le ray marching
                float4 viewPos = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
                o.viewVector = mul(unity_CameraToWorld, float4(viewPos.xyz, 0)).xyz;
                
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                
                // Direction du rayon
                float3 rayDir = normalize(i.viewVector);
                float3 rayOrigin = _CameraPos;
                
                // Ray Marching
                float stepSize = _CloudThickness / _RaySteps;
                float totalDensity = 0.0;
                float transmittance = 1.0;
                float3 cloudCol = float3(0, 0, 0);
                
                // Trouver l'intersection avec la couche de nuages
                float startHeight = _CloudHeight - _CloudThickness * 0.5;
                float endHeight = _CloudHeight + _CloudThickness * 0.5;
                
                // Calculer le point de départ du ray marching
                float t = 0.0;
                if(rayOrigin.y < startHeight && rayDir.y > 0)
                {
                    t = (startHeight - rayOrigin.y) / rayDir.y;
                }
                else if(rayOrigin.y > endHeight && rayDir.y < 0)
                {
                    t = (endHeight - rayOrigin.y) / rayDir.y;
                }
                else if(rayOrigin.y >= startHeight && rayOrigin.y <= endHeight)
                {
                    t = 0.0;
                }
                else
                {
                    // Pas d'intersection
                    return col;
                }
                
                float3 currentPos = rayOrigin + rayDir * t;
                
                // Marcher à travers les nuages
                for(int step = 0; step < (int)_RaySteps; step++)
                {
                    float density = cloudDensity(currentPos);
                    
                    if(density > 0.01)
                    {
                        // Calculer l'éclairage
                        float lightSample = cloudDensity(currentPos + _SunDirection * 50.0);
                        float lighting = exp(-lightSample * _LightAbsorption);
                        
                        // Accumulation
                        float3 sampleColor = lerp(_CloudColor.rgb, _SunColor.rgb, lighting);
                        cloudCol += sampleColor * density * transmittance * stepSize;
                        transmittance *= exp(-density * stepSize * _LightAbsorption);
                        
                        if(transmittance < 0.01) break;
                    }
                    
                    currentPos += rayDir * stepSize;
                    
                    // Sortir si on dépasse la couche de nuages
                    if(currentPos.y < startHeight || currentPos.y > endHeight)
                        break;
                }
                
                // Mélanger avec le ciel
                float alpha = 1.0 - transmittance;
                col.rgb = lerp(col.rgb, cloudCol, alpha);
                
                return col;
            }
            ENDCG
        }
    }
}
