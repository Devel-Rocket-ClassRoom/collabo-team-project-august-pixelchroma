// =============================================================================
// 1차시 (1) 최소 URP Unlit 셰이더
//
// 학습 목표
//  - ShaderLab의 계층 구조 (Shader > SubShader > Pass > HLSLPROGRAM)
//  - Attributes / Varyings 구조체와 시맨틱
//  - CBUFFER_START(UnityPerMaterial)과 SRP Batcher 호환
//
// 실습: _BaseColor를 CBUFFER 밖으로 빼고 인스펙터의 SRP Batcher 상태가
//       어떻게 바뀌는지 확인해 볼 것. (셰이더 인스펙터 상단)
// =============================================================================
Shader "Course/01a_UnlitSolid"
{
    Properties
    {
        BaseColor("Base Color", Color) = (1, 0.4, 0.2, 1)
    }

    SubShader
    {
        // RenderPipeline 태그가 없으면 URP가 이 SubShader를 선택하지 않습니다.
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP의 핵심 헤더. 여기에 변환 함수와 매크로가 들어 있음.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 정점 입력. 시맨틱(POSITION 등)으로 메시의 어느 채널을 받을지 지정.
            struct Attributes
            {
                float4 positionOS : POSITION;   // OS = Object Space
            };

            // 정점 -> 프래그먼트로 넘길 데이터. 래스터라이저가 보간함.
            struct Varyings
            {
                float4 positionCS : SV_POSITION; // CS = Clip Space (필수)
            };

            // SRP Batcher 호환을 위해 머티리얼 프로퍼티는 반드시 이 cbuffer 안에.
            CBUFFER_START(UnityPerMaterial)
                half4 BaseColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Object Space -> Clip Space (MVP 변환을 한 번에)
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return BaseColor;
            }
            ENDHLSL
        }
    }
}
