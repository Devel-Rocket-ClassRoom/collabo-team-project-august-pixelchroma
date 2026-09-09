// =============================================================================
// 5차시 (1) 깊이 기반 교차선 발광 (실드 / 홀로그램)
//
// 학습 목표
//  - _CameraDepthTexture 샘플링과 LinearEyeDepth
//  - "화면 UV"를 구하는 방법 (GetNormalizedScreenSpaceUV)
//  - 소프트 파티클과 동일한 원리 (교차 부분 페이드)
//
// [필수] URP Asset > Depth Texture : ON
//
// 실습
//  1) 이 셰이더를 Sphere에 적용하고 바닥을 관통시켜 보세요
//  2) ZWrite를 On으로 바꿔 어떤 문제가 생기는지 확인
//  3) _IntersectionThickness를 키우며 소프트 파티클 원리 설명
// =============================================================================
Shader "Course/05a_IntersectionGlow"
{
    Properties
    {
        _BaseColor("Body Color", Color) = (0.2, 0.7, 1, 0.15)
        _GlowColor("Intersection Glow", Color) = (0.5, 0.9, 1, 1)
        _IntersectionThickness("Intersection Thickness", Float) = 0.3
        _RimPower("Rim Power", Range(0.5, 8)) = 2.5
        _RimIntensity("Rim Intensity", Range(0, 4)) = 1.5
        _ScanSpeed("Scan Line Speed", Float) = 1.0
        _ScanDensity("Scan Line Density", Float) = 40.0
        _ScanIntensity("Scan Line Intensity", Range(0,1)) = 0.25
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
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One      // Additive-ish: 발광체에 적합
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlowColor;
                float _IntersectionThickness;
                half  _RimPower;
                half  _RimIntensity;
                float _ScanSpeed;
                float _ScanDensity;
                half  _ScanIntensity;
            CBUFFER_END

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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN, half facing : VFACE) : SV_Target
            {
                // --- 씬 깊이와 내 깊이 비교 ---
                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float  sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float  myEye    = IN.positionCS.w;

                // 두 깊이가 가까울수록(=교차선) 1에 가까움
                float  diff = abs(sceneEye - myEye);
                half   intersect = 1.0h - saturate(diff / max(0.0001, _IntersectionThickness));
                intersect = pow(intersect, 2.0h);

                // --- 림 라이트 (실루엣 발광) ---
                half3 N = normalize(IN.normalWS) * (facing >= 0 ? 1 : -1);
                half3 V = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                half  rim = pow(saturate(1.0h - saturate(dot(N, V))), _RimPower) * _RimIntensity;

                // --- 스캔 라인 (월드 Y 기준) ---
                half scan = frac(IN.positionWS.y * _ScanDensity * 0.05 - _Time.y * _ScanSpeed);
                scan = smoothstep(0.45h, 0.5h, scan) * _ScanIntensity;

                half3 color = _BaseColor.rgb
                            + _GlowColor.rgb * intersect * 2.0h
                            + _GlowColor.rgb * rim
                            + _GlowColor.rgb * scan;

                half alpha = saturate(_BaseColor.a + intersect + rim * 0.5h + scan);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
