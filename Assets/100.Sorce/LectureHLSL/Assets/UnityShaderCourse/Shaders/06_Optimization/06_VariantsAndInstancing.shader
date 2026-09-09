// =============================================================================
// 6차시 셰이더 변형(Variant) · 인스턴싱 · 정밀도 실습용 셰이더
//
// 학습 목표
//  - shader_feature / shader_feature_local / multi_compile 차이
//  - 키워드 개수와 변형 폭발 (2^N)
//  - GPU Instancing per-instance 프로퍼티와 SRP Batcher의 관계
//  - half vs float 정밀도
//
// [실습 A] 변형 개수 세기
//   Project 창에서 이 셰이더 선택 -> 인스펙터의 "Variant Count" 확인.
//   그 다음 아래 pragma를 하나씩 주석 처리하며 숫자가 어떻게 줄어드는지 기록.
//   표로 정리: 키워드 수 / 변형 수 / 컴파일 시간
//
// [실습 B] shader_feature vs multi_compile
//   _EMISSION_ON은 shader_feature_local -> 머티리얼에서 안 쓰면 빌드에서 제외됨
//   _DETAIL_ON은 multi_compile_local     -> 안 써도 항상 빌드에 포함됨
//   빌드 후 Shader Variant 수를 비교해 볼 것.
//
// [실습 C] 정밀도
//   USE_HALF_PRECISION을 켜고/끄며 모바일 기기에서 GPU ms 비교.
//   PC에서는 차이가 거의 없음 (PC 버전은 자동으로 float으로 변환, 모바일 버전에서 차이가 남).
//
// [실습 D] 인스턴싱
//   06_InstancingDemo.cs를 붙인 오브젝트로 큐브 500개 생성.
//   Frame Debugger에서 드로우콜을 확인하고 아래 3가지 상태를 비교:
//     1) 모두 같은 머티리얼            -> SRP Batcher
//     2) MaterialPropertyBlock로 색 변경 -> SRP Batcher 깨짐 + GPU Instancing
//     3) 머티리얼 Enable GPU Instancing 끄기 -> 개별 드로우콜
// =============================================================================
Shader "Course/06_VariantsAndInstancing"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        [Toggle(_EMISSION_ON)] _UseEmission("Enable Emission", Float) = 0
        _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionPulse("Emission Pulse Speed", Float) = 2

        [Toggle(_DETAIL_ON)] _UseDetail("Enable Detail Map", Float) = 0
        _DetailMap("Detail Map", 2D) = "gray" {}
        _DetailStrength("Detail Strength", Range(0,2)) = 1

        [Toggle(USE_HALF_PRECISION)] _UseHalf("Use half precision", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // ---- 이 셰이더의 로컬 키워드 ----
            // shader_feature_local : 머티리얼이 실제로 켠 조합만 빌드에 포함 (권장)
            #pragma shader_feature_local_fragment _EMISSION_ON
            // multi_compile_local : 항상 모든 조합이 빌드에 포함됨
            //   런타임에 Shader.EnableKeyword로 전역 제어할 때만 필요
            #pragma multi_compile_local _ _DETAIL_ON
            #pragma shader_feature_local_fragment USE_HALF_PRECISION

            // ---- URP 시스템 키워드 (여기가 변형 폭발의 주범) ----
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog

            // ---- GPU Instancing ----
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #ifdef USE_HALF_PRECISION
                #define FLT  half
                #define FLT3 half3
                #define FLT4 half4
            #else
                #define FLT  float
                #define FLT3 float3
                #define FLT4 float4
            #endif

            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _DetailMap_ST;
                half4  _BaseColor;
                half4  _EmissionColor;
                half   _EmissionPulse;
                half   _DetailStrength;
            CBUFFER_END

            // ---------------------------------------------------------------------
            // per-instance 프로퍼티.
            // 주의: 이걸 쓰면 SRP Batcher와 함께 쓸 수 없음.
            //   SRP Batcher = 머티리얼 프로퍼티를 큰 버퍼에 모아 두고 드로우콜만 반복
            //   GPU Instancing = 인스턴스별 데이터를 배열로 넘겨 한 번에 그림
            // Unity는 SRP Batcher를 우선하며, per-instance 프로퍼티가 실제로
            // 세팅되면 인스턴싱 경로로 내려감.
            // ---------------------------------------------------------------------
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(vp.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                FLT4 albedo = (FLT4)SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                albedo *= (FLT4)_BaseColor;

                // per-instance 색 곱하기
                albedo *= (FLT4)UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);

                #ifdef _DETAIL_ON
                    FLT detail = (FLT)SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap,
                                     TRANSFORM_TEX(IN.uv, _DetailMap)).r;
                    // 0.5를 중심으로 밝기 변조 (Overlay 유사)
                    albedo.rgb *= lerp((FLT)1.0, detail * (FLT)2.0, (FLT)_DetailStrength);
                #endif

                FLT3 N = normalize((FLT3)IN.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                FLT ndotl = saturate(dot(N, (FLT3)mainLight.direction));
                FLT3 color = albedo.rgb * ((FLT3)mainLight.color * ndotl * (FLT)mainLight.shadowAttenuation
                                          + (FLT3)SampleSH(N));

                #ifdef _EMISSION_ON
                    FLT pulse = (FLT)(0.5 + 0.5 * sin(_Time.y * _EmissionPulse));
                    color += (FLT3)_EmissionColor.rgb * pulse;
                #endif

                half3 outColor = (half3)color;//MixFog((half3)color, IN.fogFactor);
                return half4(outColor, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On  ZTest LEqual  ColorMask 0  Cull Back

            HLSLPROGRAM
            #pragma vertex   CourseShadowVert
            #pragma fragment CourseShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _DetailMap_ST;
                half4  _BaseColor;
                half4  _EmissionColor;
                half   _EmissionPulse;
                half   _DetailStrength;
            CBUFFER_END

            #include "../../Common/CourseDepthShadowPasses.hlsl"
            ENDHLSL
        }
    }
}
