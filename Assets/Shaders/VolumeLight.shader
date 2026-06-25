Shader "Custom/FakeVolumetricLight"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Range(0, 10)) = 2
        _FresnelPower("Fresnel Power", Range(0.1, 5)) = 1
        _AlphaStart("Alpha Start", Range(0, 1)) = 0.0
        _AlphaEnd("Alpha End", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float2 uv          : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _FresnelPower;
                half _AlphaStart;
                half _AlphaEnd;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
{
    float3 N = normalize(IN.normalWS);
    float3 V = normalize(IN.viewDirWS);

    float rim = 1.0 - saturate(dot(N, V));
    float fresnel = 1.0 - pow(rim, _FresnelPower);

    float gradient = lerp(_AlphaStart, _AlphaEnd, clamp(IN.uv.y, 0, 1));
    float alpha = fresnel * gradient;

    half3 emission = _Color.rgb * _Intensity * alpha;

    return half4(emission, alpha);
}
            ENDHLSL
        }
    }
}