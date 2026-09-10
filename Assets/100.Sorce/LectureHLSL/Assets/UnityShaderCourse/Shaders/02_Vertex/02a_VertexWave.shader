// ================================================================================================
// 2차시 (1) 정점 변형 + 노멀 재계산
//
// 학습 목표
//  - GetVertexPositionInputs / GetVertexNormalInputs
//  - 정점을 움직였을 때 노멀도 함께 고쳐야 하는 이유
//  - 오브젝트/월드 공간 선택이 결과를 바꾸는 지점
//
// 실습: _RecalcNormal을 0으로 두고 라이팅이 어떻게 어색해지는지 확인
// 권장 메시: Plane (ProBuilder 또는 분할이 많은 Plane 에셋 사용)
//           ex) ProBuilder로 Plane 제작 후 Height/Width Cuts 값을 plane 크기의 2배 이상으로 지정.
// ================================================================================================
Shader "Course/02a_VertexWave"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.2, 0.6, 1, 1)
        _Amplitude("Wave Amplitude", Float) = 0.3
        _Frequency("Wave Frequency", Float) = 2.0
        _Speed("Wave Speed", Float) = 1.5
        [Toggle] _RecalcNormal("Recalculate Normal", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardUnlitLambert"
            Tags { "LightMode" = "UniversalForward" }

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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Amplitude;
                float _Frequency;
                float _Speed;
                float _RecalcNormal;
            CBUFFER_END

            // 높이 함수. 노멀 재계산 시에도 같은 함수를 써야 일관성이 유지됩니다.
            float WaveHeight(float2 p)
            {
                return sin(p.x * _Frequency + _Time.y * _Speed) *
                       cos(p.y * _Frequency * 0.7 + _Time.y * _Speed * 0.8) *
                       _Amplitude;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = IN.positionOS.xyz;
                posOS.y += WaveHeight(posOS.xz);

                float3 normalOS = IN.normalOS;

                if (_RecalcNormal > 0.5)
                {
                    // 유한 차분으로 접선 두 개를 구해 외적 -> 새 노멀
                    const float e = 0.01;
                    float3 tx = float3(e, WaveHeight(posOS.xz + float2(e, 0)) - WaveHeight(posOS.xz), 0);
                    float3 tz = float3(0, WaveHeight(posOS.xz + float2(0, e)) - WaveHeight(posOS.xz), e);
                    normalOS = normalize(cross(tz, tx));
                }

                VertexPositionInputs vp = GetVertexPositionInputs(posOS);
                VertexNormalInputs   vn = GetVertexNormalInputs(normalOS);

                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                half3 N = normalize(IN.normalWS);
                half  ndotl = saturate(dot(N, mainLight.direction));
                half  atten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                half3 lit = _BaseColor.rgb * mainLight.color * ndotl * atten;
                lit += _BaseColor.rgb * SampleSH(N) * 0.5;   // 간단한 앰비언트

                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
}
