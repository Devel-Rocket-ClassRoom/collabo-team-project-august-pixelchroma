// =============================================================================
// 5차시 (3) 스텐실 마스크 작성기 — "포탈 창문"
//
// 사용법
//  1) 이 셰이더로 만든 머티리얼을 Quad에 적용 (포탈 창문 역할)
//  2) 포탈 안에서만 보일 오브젝트에는 05d_StencilMasked를 적용
//  3) 둘의 _StencilRef 값을 같게 맞춥니다
//
// 원리
//  - 이 오브젝트는 색을 쓰지 않고(ColorMask 0) 스텐실 버퍼에만 값을 기록
//  - Queue를 앞당겨(Geometry-1) 마스크를 먼저 기록
// =============================================================================
Shader "Course/05c_StencilMaskWriter"
{
    Properties
    {
        [IntRange] _StencilRef("Stencil Ref", Range(0, 255)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry-1"     // 마스크를 먼저 기록
        }

        Pass
        {
            Name "StencilWrite"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Stencil
            {
                Ref  [_StencilRef]
                Comp Always
                Pass Replace
            }

            ColorMask 0     // 색상 버퍼에 아무것도 쓰지 않음
            ZWrite Off      // 깊이도 남기지 않음 (뒤 오브젝트가 가려지지 않게)
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float _StencilRef;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
