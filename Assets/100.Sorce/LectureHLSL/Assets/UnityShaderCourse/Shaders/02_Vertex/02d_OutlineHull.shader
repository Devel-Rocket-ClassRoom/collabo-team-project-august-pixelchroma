// =============================================================================
// 2차시 (4) Inverted Hull 아웃라인 (2-Pass)
//
// 학습 목표
//  - Pass가 여러 개일 때의 실행 순서와 Cull 방향
//  - 클립 공간에서 오프셋을 주어 화면 기준 굵기를 일정하게 유지하는 방법
//  - 종횡비 보정이 필요한 이유
//
// 실습:
//  1) positionCS.w 곱셈을 제거하면 어떻게 되는지 (거리에 따라 굵기가 변함)
//  2) 종횡비 보정을 제거하고 창을 가로로 늘려 보기
//  3) 큐브에 적용해 보고 왜 모서리가 갈라지는지 토론 (노멀이 분리된 하드 엣지)
// =============================================================================
Shader "Course/02d_OutlineHull"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.9, 0.9, 0.9, 1)
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width (px-ish)", Range(0, 20)) = 3
        [Toggle] _ScreenSpaceWidth("Constant Screen Width", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // ---------------------------------------------------------------------
        // Pass 1: 아웃라인. 뒷면만 그리면서 노멀 방향으로 부풀림.
        //         앞면 오브젝트가 나중에 덮어쓰므로 테두리만 남게됨.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _ScreenSpaceWidth;
            CBUFFER_END

            /*
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                float4 positionCS = TransformWorldToHClip(positionWS);

                // 노멀을 클립 공간으로 보내 화면상의 밀어낼 방향을 구함
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);
                float2 offset   = normalize(normalCS.xy + 1e-6);

                // 종횡비 보정 (없으면 가로로 넓은 화면에서 테두리가 타원형이 됨)
                offset.x *= _ScreenParams.y / _ScreenParams.x;

                // w를 곱하면 NDC 기준 고정 크기 -> 거리와 무관하게 화면 굵기 일정
                float scale = _OutlineWidth * 0.002;
                if (_ScreenSpaceWidth > 0.5)
                    positionCS.xy += offset * scale * positionCS.w;
                else
                    positionCS.xy += offset * scale;

                OUT.positionCS = positionCS;
                return OUT;
            }
            */

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // [해결책 1] 노멀 대신 로컬 위치 기반 방사형 방향(Smooth Dir) 추출
                float3 smoothDirOS = normalize(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(smoothDirOS);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                // 노멀을 클립 공간으로 변환하여 오프셋 방향 구하기
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);
                float2 offset   = normalize(normalCS.xy + 1e-6);

                // 종횡비 보정
                offset.x *= _ScreenParams.y / _ScreenParams.x;

                // 화면 고정 굵기 처리 (w 곱셈)
                float scale = _OutlineWidth * 0.002;
                if (_ScreenSpaceWidth > 0.5)
                    positionCS.xy += offset * scale * positionCS.w;
                else
                    positionCS.xy += offset * scale;

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // Pass 2: 본체
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            // SRP Batcher: 모든 Pass의 UnityPerMaterial 레이아웃이 동일해야 합니다.
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _ScreenSpaceWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);
                half3  N = normalize(IN.normalWS);
                half   ndotl = saturate(dot(N, mainLight.direction));
                half3  col = _BaseColor.rgb * mainLight.color * ndotl * mainLight.shadowAttenuation;
                col += _BaseColor.rgb * SampleSH(N) * 0.5;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
