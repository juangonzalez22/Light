Shader "Custom/BurnShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)

        _DissolveMap("Dissolve Map", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0
        _DissolveColor("Dissolve Color", Color) = (1, 0.3, 0, 1)
        _DissolveEmission("Dissolve Emission", Range(0,10)) = 3
        _DissolveWidth("Dissolve Width", Range(0,0.1)) = 0.05
    }

    SubShader
    {
        Tags { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvDissolve : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            TEXTURE2D(_DissolveMap);
            SAMPLER(sampler_DissolveMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _DissolveMap_ST;
                half4 _EmissionColor;
                half _DissolveAmount;
                half4 _DissolveColor;
                half _DissolveEmission;
                half _DissolveWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.uvDissolve = TRANSFORM_TEX(IN.uv, _DissolveMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                half mask = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, IN.uvDissolve).r;

                clip(mask - _DissolveAmount);

                half edge = smoothstep(_DissolveAmount, _DissolveAmount + _DissolveWidth, mask);

                half borderVisibility = step(0.0001, _DissolveAmount);
                edge = lerp(1.0, edge, borderVisibility);

                color.rgb = lerp(_DissolveColor.rgb, color.rgb, edge);

                half3 dissolveEmission = _DissolveColor.rgb * _DissolveEmission * (1.0 - edge);
                color.rgb += dissolveEmission;

                half emissionMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).r;
                color.rgb += _EmissionColor.rgb * emissionMask;

                return color;
            }
            ENDHLSL
        }
    }
}