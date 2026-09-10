// =============================================================================
// 3차시 커스텀 Toon Lit 셰이더 (이 과정의 첫 "완성형" 셰이더)
//
// 학습 목표
//  - URP 라이팅 API: GetMainLight / GetAdditionalLight / SampleSH
//  - 그림자 수신에 필요한 키워드와 shadowCoord 처리 (버전 함정 포함)
//  - ShadowCaster / DepthOnly / DepthNormals Pass를 왜 직접 만들어야 하는가
//  - Fog 적용
//
// Pass 구성
//  1) ForwardLit    : 실제 셰이딩
//  2) ShadowCaster  : 이 오브젝트가 그림자를 "드리우게" 하려면 필수
//  3) DepthOnly     : Depth Texture 프리패스용
//  4) DepthNormals  : Normal Texture(8차시 아웃라인)용
//
// 실습 (반드시 해볼 내용)
//  - ShadowCaster Pass를 주석 처리 -> 그림자를 못 드리움
//  - _MAIN_LIGHT_SHADOWS 계열 pragma 하나 삭제 -> 그림자를 못 받음
//  - DepthNormals Pass 삭제 -> 8차시 아웃라인에서 이 오브젝트만 테두리 없음
// =============================================================================
Shader "Course/03_ToonLit"
{
    Properties
    {
        [Header(Base)]
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        [Header(Diffuse Ramp)]
        [Toggle] _UseRampMap("Use Ramp Texture", Float) = 0
        _RampMap("Ramp Map (1D gradient)", 2D) = "white" {}
        _RampThreshold("Ramp Threshold", Range(0,1)) = 0.5
        _RampSmooth("Ramp Smoothness", Range(0.001, 0.5)) = 0.03
        _ShadowTint("Shadow Tint", Color) = (0.35, 0.4, 0.6, 1)

        [Header(Specular)]
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecGloss("Specular Gloss", Range(1, 256)) = 48
        _SpecThreshold("Specular Threshold", Range(0,1)) = 0.5
        _SpecSmooth("Specular Smoothness", Range(0.001, 0.3)) = 0.02

        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.5, 16)) = 4
        _RimIntensity("Rim Intensity", Range(0, 2)) = 0.6

        [Header(Misc)]
        _AdditionalLightScale("Additional Light Scale", Range(0,2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
            "UniversalMaterialType" = "SimpleLit"
        }

        // =====================================================================
        // Pass 1 : ForwardLit
        // =====================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            // --- 그림자 수신 ---
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            // --- 추가 광원 ---
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES

            // Forward+ (클러스터 라이트 루프).
            // URP 버전에 따라 키워드 이름이 _FORWARD_PLUS 또는 _CLUSTER_LIGHT_LOOP 임.
            // 둘 다 선언해 두면 어느 버전에서도 추가 광원이 정상 동작함.

            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP

            // --- 기타 ---
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "03_ToonLitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                // -------------------------------------------------------------
                // shadowCoord를 정점에서 넘길지 프래그먼트에서 계산할지는 URP가 REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR로 결정.
                // (스크린스페이스 섀도우일 때는 반드시 정점에서 넘겨야 함) 인터넷 예제가 이 처리를 빼먹어서 "그림자가 이상하다"는 문제가 자주 생김.
                // -------------------------------------------------------------
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(vp.positionCS.z);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    OUT.shadowCoord = GetShadowCoord(vp);
                #endif
                return OUT;
            }

            // 확산광을 툰 스타일로 계단화
            half3 ToonDiffuse(half ndotl, half atten, half3 lightColor)
            {
                half lambert = ndotl * atten;

                half ramp;
                if (_UseRampMap > 0.5h)
                {
                    // 램프 텍스처: 아티스트가 그라데이션을 직접 그려 넣는 방식
                    ramp = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(saturate(lambert), 0.5)).r;
                }
                else
                {
                    ramp = smoothstep(_RampThreshold - _RampSmooth,
                                      _RampThreshold + _RampSmooth,
                                      lambert);
                }
                // 그림자 영역에 색조를 넣어 단조로움을 피함
                half3 shadowed = _ShadowTint.rgb;
                return lerp(shadowed, lightColor, ramp);
            }

            half3 ToonSpecular(half3 N, half3 L, half3 V, half atten, half3 lightColor)
            {
                half3 H = SafeNormalize(L + V);
                half  ndoth = saturate(dot(N, H));
                half  spec  = pow(ndoth, _SpecGloss);
                spec = smoothstep(_SpecThreshold - _SpecSmooth,
                                  _SpecThreshold + _SpecSmooth, spec);
                return spec * atten * lightColor * _SpecularColor.rgb;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                half3 N = normalize(IN.normalWS);
                half3 V = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                // --- shadowCoord 결정 ---
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord = IN.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                // --- 메인 광원 ---
                Light mainLight = GetMainLight(shadowCoord);
                half  mainAtten = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half  ndotl     = saturate(dot(N, mainLight.direction));

                half3 diffuse  = ToonDiffuse(ndotl, mainAtten, mainLight.color);
                half3 specular = ToonSpecular(N, mainLight.direction, V, mainAtten, mainLight.color);

                // --- 추가 광원 ---
                // LIGHT_LOOP_BEGIN 매크로는 inputData라는 이름의 변수를 참조하므로 이름을 바꾸면 컴파일이 깨짐.
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS   = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = shadowCoord;
                inputData.positionCS = IN.positionCS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                half3 additional = 0;
                #ifdef _ADDITIONAL_LIGHTS
                    uint lightCount = GetAdditionalLightsCount();
                    #if defined(LIGHT_LOOP_BEGIN)
                        LIGHT_LOOP_BEGIN(lightCount)
                            Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                            half a  = light.distanceAttenuation * light.shadowAttenuation;
                            half nl = saturate(dot(N, light.direction));
                            additional += ToonDiffuse(nl, a, light.color) * nl;
                            additional += ToonSpecular(N, light.direction, V, a, light.color);
                        LIGHT_LOOP_END
                    #else
                        for (uint li = 0u; li < lightCount; ++li)
                        {
                            Light light = GetAdditionalLight(li, inputData.positionWS);
                            half a  = light.distanceAttenuation * light.shadowAttenuation;
                            half nl = saturate(dot(N, light.direction));
                            additional += ToonDiffuse(nl, a, light.color) * nl;
                            additional += ToonSpecular(N, light.direction, V, a, light.color);
                        }
                    #endif
                #endif
                additional *= _AdditionalLightScale;

                // --- 앰비언트 (SH 프로브) ---
                half3 ambient = SampleSH(N);

                // --- 림 라이트 (프레넬) ---
                half rim = pow(saturate(1.0h - saturate(dot(N, V))), _RimPower);
                // 광원 반대쪽에만 림을 넣으면 더 자연스러움.
                rim *= smoothstep(0.0h, 0.4h, ndotl);
                half3 rimColor = rim * _RimIntensity * _RimColor.rgb;

                half3 color = albedo.rgb * (diffuse + additional + ambient) + specular + rimColor;
                color = MixFog(color, IN.fogFactor);

                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        // =====================================================================
        // Pass 2 : ShadowCaster  (그림자를 드리우기 위해 필수)
        // =====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "03_ToonLitInput.hlsl"

            // URP가 매 그림자 드로우마다 전역으로 설정하는 값들. UnityPerMaterial 안에 넣으면 안됨.
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirWS = _LightDirection;
                #endif

                // 섀도우 아크네 / 피터패닝을 막기 위한 노멀·깊이 바이어스 적용
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirWS));

                // 근평면 클리핑으로 그림자가 잘리는 것을 방지
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // =====================================================================
        // Pass 3 : DepthOnly (URP Asset의 Depth Texture 활성화 시 사용)
        // =====================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "03_ToonLitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthFrag(Varyings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // =====================================================================
        // Pass 4 : DepthNormals (_CameraNormalsTexture 생성용)
        //          8차시 아웃라인 포스트 프로세스에 필요.
        // =====================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "03_ToonLitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  normalWS   : TEXCOORD0;
            };

            Varyings DepthNormalsVert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 DepthNormalsFrag(Varyings IN) : SV_Target
            {
                // URP의 _CameraNormalsTexture는 월드 공간 노멀을 저장.
                return half4(normalize(IN.normalWS), 0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
