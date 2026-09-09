// =============================================================================
// 1차시 (2) 텍스처 샘플링 + UV 스크롤
//
// 학습 목표
//  - TEXTURE2D / SAMPLER / SAMPLE_TEXTURE2D 매크로 (플랫폼 추상화)
//  - TRANSFORM_TEX와 _XXX_ST (Tiling/Offset)
//  - _Time 내장 변수
//
// 실습: SAMPLE_TEXTURE2D 대신 tex2D를 써 보고 왜 매크로를 쓰는지 확인
// =============================================================================
Shader "Course/01b_UnlitTexture"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _ScrollSpeed("Scroll Speed (XY)", Vector) = (0.1, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

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

            // 텍스처와 샘플러는 cbuffer 밖에 선언합니다 (cbuffer에는 들어갈 수 없음).
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;     // Tiling(xy) / Offset(zw). 이름 규칙 고정.
                half4  _BaseColor;
                float4 _ScrollSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // uv * Tiling + Offset
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // _Time = (t/20, t, t*2, t*3). 보통 _Time.y를 씁니다.
                float2 uv = IN.uv + _ScrollSpeed.xy * _Time.y;
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                return tex * _BaseColor;
            }
            ENDHLSL
        }
    }
}
