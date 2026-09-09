// =============================================================================
// 2차시 (3) 빌보드
//
// 학습 목표
//  - 뷰 행렬에서 카메라의 right / up 축 추출
//  - 정점을 "월드 공간에서 재구성"하는 사고방식
//
// 실습: _LockY를 켜면 Y축 고정 빌보드(나무용), 끄면 완전 빌보드(파티클용).
//       Quad 메시에 적용할 것.
// =============================================================================
Shader "Course/02c_Billboard"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _Size("Size", Float) = 1.0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.3
        [Toggle] _LockY("Lock Y Axis", Float) = 0
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
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float  _Size;
                half   _Cutoff;
                float  _LockY;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 오브젝트 원점의 월드 좌표 = 빌보드가 서 있을 위치
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));

                // 뷰 행렬의 각 행이 카메라의 월드 공간 기저 벡터.
                float3 camRight = float3(UNITY_MATRIX_V[0][0], UNITY_MATRIX_V[0][1], UNITY_MATRIX_V[0][2]);
                float3 camUp    = float3(UNITY_MATRIX_V[1][0], UNITY_MATRIX_V[1][1], UNITY_MATRIX_V[1][2]);

                if (_LockY > 0.5)
                {
                    // Y축 고정: right는 수평 성분만, up은 월드 up
                    camRight = normalize(float3(camRight.x, 0, camRight.z));
                    camUp    = float3(0, 1, 0);
                }

                // Quad의 로컬 XY를 카메라 평면에 투사
                float3 posWS = centerWS
                             + camRight * IN.positionOS.x * _Size
                             + camUp    * IN.positionOS.y * _Size;

                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                clip(c.a - _Cutoff);
                return c;
            }
            ENDHLSL
        }
    }
}
