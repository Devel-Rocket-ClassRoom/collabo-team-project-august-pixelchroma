// =============================================================================
// 7차시 GPU 파티클 렌더링 셰이더 (절차적 지오메트리)
//
// 학습 목표
//  - 메시 없이 SV_VertexID만으로 쿼드를 만들어 내는 방법
//  - 버텍스 셰이더에서 StructuredBuffer 읽기 (SM 4.5 이상 필요)
//  - 카메라를 향하는 빌보드를 뷰 행렬에서 구성
//
// 실습
//  1) Blend One One(가산) <-> SrcAlpha OneMinusSrcAlpha(알파) 비교
//  2) 파티클 수를 10만 -> 100만으로 올리며 GPU ms 측정
// =============================================================================
Shader "Course/07_ParticleRender"
{
    Properties
    {
        _ColorStart("Color (life=1)", Color) = (1, 0.9, 0.4, 1)
        _ColorEnd("Color (life=0)", Color) = (1, 0.1, 0.05, 1)
        _Size("Particle Size", Float) = 0.06
        _SoftEdge("Soft Edge", Range(0.01, 1)) = 0.5
        _Intensity("Intensity", Range(0, 5)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "ParticleUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One        // 가산 합성 (발광 파티클)
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            // StructuredBuffer를 버텍스 셰이더에서 읽으려면 SM 4.5 이상
            #pragma target 4.5
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // 컴퓨트 셰이더의 struct와 반드시 동일한 레이아웃
            struct Particle
            {
                float3 position;
                float  life;
                float3 velocity;
                float  seed;
            };

            StructuredBuffer<Particle> _Particles;

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorStart;
                half4 _ColorEnd;
                float _Size;
                half  _SoftEdge;
                half  _Intensity;
            CBUFFER_END

            // 쿼드 하나 = 삼각형 2개 = 정점 6개
            static const float2 kCorners[6] =
            {
                float2(-1, -1), float2(-1,  1), float2( 1,  1),
                float2(-1, -1), float2( 1,  1), float2( 1, -1)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 localUV    : TEXCOORD0;   // -1..1
                half3  color      : TEXCOORD1;
                half   life       : TEXCOORD2;
            };

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings OUT;

                uint particleIndex = vertexID / 6u;
                uint cornerIndex   = vertexID % 6u;

                Particle p = _Particles[particleIndex];
                float2 corner = kCorners[cornerIndex];

                // 뷰 행렬의 행에서 카메라의 right / up 축을 얻어 빌보드 구성
                float3 camRight = float3(UNITY_MATRIX_V[0][0], UNITY_MATRIX_V[0][1], UNITY_MATRIX_V[0][2]);
                float3 camUp    = float3(UNITY_MATRIX_V[1][0], UNITY_MATRIX_V[1][1], UNITY_MATRIX_V[1][2]);

                // 수명이 줄면 작아지도록
                float size = _Size * saturate(p.life * 1.5);
                float3 positionWS = p.position + (camRight * corner.x + camUp * corner.y) * size;

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.localUV    = corner;
                OUT.life       = (half)saturate(p.life);
                OUT.color      = lerp(_ColorEnd.rgb, _ColorStart.rgb, OUT.life);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 원형 마스크 (텍스처 없이)
                half r = length(IN.localUV);
                half alpha = saturate(1.0h - r);
                alpha = pow(alpha, 1.0h / max(0.01h, _SoftEdge));

                if (alpha <= 0.001h) discard;

                half3 color = IN.color * alpha * _Intensity * IN.life;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
