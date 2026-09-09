// =============================================================================
// 8차시 (1) 최소 전체화면 셰이더 — 그레이스케일
//
// 학습 목표
//  - 전체화면(fullscreen) 셰이더의 전용 버텍스 함수
//  - _BlitTexture / sampler_LinearClamp / _BlitScaleBias
//  - Blit.hlsl이 제공하는 Vert 함수를 그대로 쓰는 이유 (XR, 스케일 바이어스 처리)
//
// [중요] 이 셰이더는 씬 오브젝트에 붙이는 게 아니라
//        08a_GrayscaleRendererFeature.cs가 사용합니다.
//
// [경로 문제 대응]
//  Blit.hlsl의 경로가 URP 버전에 따라 다를 수 있습니다.
//  컴파일 에러가 나면 Project 창의 Packages 폴더에서 "Blit.hlsl"을 검색해
//  실제 경로로 바꾸세요. (우클릭 > Show in Explorer로 확인 가능)
// =============================================================================
Shader "Course/08a_Grayscale"
{
    Properties
    {
        _Intensity("Intensity", Range(0, 1)) = 1
        _Vignette("Vignette", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "Grayscale"

            HLSLPROGRAM
            #pragma vertex   Vert          // Blit.hlsl이 제공
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _Vignette;

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Rec.709 휘도 가중치. 단순 평균((r+g+b)/3)과 비교해 보세요.
                half luma = dot(col.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                col.rgb = lerp(col.rgb, luma.rrr, _Intensity);

                if (_Vignette > 0.001)
                {
                    float2 d = uv - 0.5;
                    half v = 1.0h - saturate(dot(d, d) * 2.0h * _Vignette);
                    col.rgb *= v;
                }

                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
