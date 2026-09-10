// =============================================================================
// 8차시 (2) 2패스 분리형 가우시안 블러
//
// 학습 목표
//  - 2D 가우시안을 1D 두 번으로 분리하면 왜 연산량이 O(N^2) -> O(2N)이 되는가
//  - _BlitTexture_TexelSize로 1픽셀 크기 얻기
//  - 다운샘플링이 블러 품질/성능에 미치는 영향
//
// Pass 0: 수평 블러
// Pass 1: 수직 블러
// Pass 2: 단순 복사 (다시 카메라 컬러로 되돌릴 때)
// =============================================================================
Shader "Course/08b_Blur"
{
    Properties
    {
        _BlurRadius("Blur Radius (px)", Range(0, 8)) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurRadius;

        // 9탭 가우시안 커널 (sigma ~ 2.0). 대칭이므로 절반만 저장.
        static const half kWeights[5] =
        {
            0.2270270270h, 0.1945945946h, 0.1216216216h, 0.0540540541h, 0.0162162162h
        };

        half4 BlurDirection(float2 uv, float2 dir)
        {
            // _BlitTexture_TexelSize = (1/width, 1/height, width, height)
            float2 texel = _BlitTexture_TexelSize.xy * dir * _BlurRadius;

            half4 sum = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * kWeights[0];
            [unroll]
            for (int i = 1; i < 5; i++)
            {
                float2 offset = texel * i;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset) * kWeights[i];
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offset) * kWeights[i];
            }
            return sum;
        }
        ENDHLSL

        Pass
        {
            Name "BlurHorizontal"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return BlurDirection(input.texcoord, float2(1, 0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "BlurVertical"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return BlurDirection(input.texcoord, float2(0, 1));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Copy"
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
