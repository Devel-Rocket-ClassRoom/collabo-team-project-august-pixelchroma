// =============================================================================
// 2차시 (2) 바람에 흔들리는 풀
//
// 학습 목표
//  - UV.y 또는 버텍스 컬러를 "흔들림 마스크"로 쓰는 실무 패턴
//  - 오브젝트 위치를 위상(phase) 오프셋으로 써서 개체마다 다르게 흔들기
//  - 양면 렌더링(Cull Off)과 노멀 뒤집기
//
// 실습: _WindMaskSource를 바꿔 UV 기반 / 버텍스컬러 기반을 비교.
// =============================================================================
Shader "Course/02b_GrassWind"
{
    Properties
    {
        _BaseMap("Grass Texture (Alpha Clip)", 2D) = "white" {}
        _TopColor("Top Color", Color) = (0.45, 0.8, 0.25, 1)
        _BottomColor("Bottom Color", Color) = (0.1, 0.3, 0.08, 1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        _WindStrength("Wind Strength", Float) = 0.25
        _WindSpeed("Wind Speed", Float) = 2.0
        _WindFrequency("Wind Frequency (world)", Float) = 0.6
        _WindDirection("Wind Direction (XZ)", Vector) = (1, 0, 0.3, 0)
        [KeywordEnum(UV, VertexColor)] _WindMask("Wind Mask Source", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "AlphaTest"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off        // 풀 카드는 양면으로 보여야 함

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma shader_feature_local _WINDMASK_UV _WINDMASK_VERTEXCOLOR
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                half   heightMask : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _TopColor;
                half4  _BottomColor;
                half   _Cutoff;
                float  _WindStrength;
                float  _WindSpeed;
                float  _WindFrequency;
                float4 _WindDirection;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 1) 흔들림 마스크 (뿌리=0, 끝=1)
                #ifdef _WINDMASK_VERTEXCOLOR
                    half mask = IN.color.r;
                #else
                    half mask = saturate(IN.uv.y);
                #endif
                // 끝이 더 많이 휘도록 제곱
                half bend = mask * mask;

                // 2) 오브젝트의 월드 위치를 위상 오프셋으로 -> 개체마다 다른 타이밍
                float3 pivotWS = TransformObjectToWorld(float3(0, 0, 0));
                float  phase   = dot(pivotWS.xz, float2(1.0, 0.7)) * _WindFrequency;

                float wave = sin(_Time.y * _WindSpeed + phase)
                           + 0.4 * sin(_Time.y * _WindSpeed * 2.3 + phase * 1.7); // 배음

                float3 windDir = normalize(float3(_WindDirection.x, 0, _WindDirection.z));

                // 3) 월드 공간에서 밀어줌 (오브젝트 회전과 무관하게 바람 방향 유지)
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                positionWS += windDir * wave * bend * _WindStrength;

                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                OUT.heightMask = mask;
                return OUT;
            }

            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(tex.a - _Cutoff);      // 알파 컷아웃

                // VFACE: 앞면 +1 / 뒷면 -1. 뒷면은 노멀을 뒤집어야 라이팅이 맞게됨.
                half3 N = normalize(IN.normalWS) * (facing >= 0 ? 1 : -1);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                // 풀은 반투명하므로 Half-Lambert가 자연스러움.
                half ndotl = dot(N, mainLight.direction) * 0.5 + 0.5;
                half atten = mainLight.shadowAttenuation;

                half3 albedo = lerp(_BottomColor.rgb, _TopColor.rgb, IN.heightMask);
                half3 col = albedo * mainLight.color * ndotl * lerp(0.5, 1.0, atten);
                col += albedo * SampleSH(N) * 0.4;

                col = MixFog(col, IN.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
