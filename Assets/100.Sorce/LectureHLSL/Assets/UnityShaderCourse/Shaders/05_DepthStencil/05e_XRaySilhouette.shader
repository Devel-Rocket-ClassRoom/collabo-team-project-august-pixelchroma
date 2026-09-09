// =============================================================================
// 5차시 (5) 벽 뒤 캐릭터 투시 (X-Ray) — ZTest 활용
//
// 학습 목표
//  - ZTest Greater: "다른 물체에 가려진 부분에만" 그리는 트릭
//  - Pass 순서와 렌더 큐의 관계
//
// 실습
//  1) 캐릭터를 벽 뒤로 이동 -> 실루엣이 나타남
//  2) ZTest를 LEqual로 바꾸면 어떻게 되는지 확인
//  3) Queue를 Geometry로 되돌리면 벽이 나중에 그려져 실루엣이 사라질 수 있음
//     -> 렌더 순서 문제. 실무에서는 별도 Renderer Feature로 처리합니다.
// =============================================================================
Shader "Course/05e_XRaySilhouette"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.8, 0.8, 0.85, 1)
        _XRayColor("X-Ray Color", Color) = (0.2, 1, 0.6, 0.6)
        _XRayRimPower("X-Ray Rim Power", Range(0.5, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry+100"   // 대부분의 불투명 물체 뒤에 그림
        }

        // Pass 1: 가려진 부분의 실루엣
        Pass
        {
            Name "XRay"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZTest Greater       // 깊이 테스트를 "실패해야" 그림 = 가려진 곳
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

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
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _XRayColor;
                half  _XRayRimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 N = normalize(IN.normalWS);
                half3 V = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                half rim = pow(saturate(1.0h - saturate(dot(N, V))), _XRayRimPower);
                half alpha = _XRayColor.a * saturate(0.35h + rim);
                return half4(_XRayColor.rgb, alpha);
            }
            ENDHLSL
        }

        // Pass 2: 정상 렌더
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZTest LEqual
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
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
                half3  normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _XRayColor;
                half  _XRayRimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 N = normalize(IN.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half ndotl = saturate(dot(N, mainLight.direction));
                half3 col = _BaseColor.rgb * (mainLight.color * ndotl * mainLight.shadowAttenuation + SampleSH(N));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
