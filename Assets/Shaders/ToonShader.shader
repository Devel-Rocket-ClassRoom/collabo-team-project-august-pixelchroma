Shader "Custom/ToonLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)

        [Header(Toon Ramp)][Space(4)]
        _Flatten("Flatten (contrast reduce)", Range(0,1)) = 0.425
        _ShadowThreshold("Shadow Threshold", Range(0,1)) = 0.68
        _ShadowFeather("Shadow Feather", Range(0.001,0.4)) = 0.10
        _ShadowTint("Shadow Tint", Color) = (0.66,0.71,0.86,1)
        _ReceiveShadowStrength("Receive Shadow Strength", Range(0,1)) = 0.0
        [Toggle] _UseRampMap("Use Ramp Texture", Float) = 0
        _RampMap("Ramp Map (1D gradient)", 2D) = "white" {}

        [Header(Specular)][Space(4)]
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
        _SpecGloss("Specular Gloss", Range(1, 256)) = 48
        _SpecThreshold("Specular Threshold", Range(0,1)) = 0.5
        _SpecSmooth("Specular Smoothness", Range(0.001, 0.3)) = 0.02

        [Header(Rim Light)][Space(4)]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimPower("Rim Power", Range(0.5, 16)) = 4
        _RimIntensity("Rim Intensity", Range(0, 2)) = 0.6

        [Header(Outline)][Space(4)]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(0, 20)) = 3
        [Toggle] _ScreenSpaceWidth("Constant Screen Width", Float) = 1

        [Header(Ambient)][Space(4)]
        _AmbientStrength("Ambient Strength", Range(0,2)) = 1.0
        _AmbientFlatten("Ambient Flatten", Range(0,1)) = 0.6
        _EnvironmentInfluence("Environment Influence", Range(0,1)) = 0.583

        [Header(Brightness)][Space(4)]
        _Brightness("Brightness", Range(0.5,3)) = 1.0

        [Header(Additional Lights)][Space(4)]
        _AdditionalLightIntensity("Additional Light Intensity", Range(0,2)) = 0.5

        [Header(Rendering)][Space(4)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        // =================================================================
        // Pass 1 : Outline (Inverted Hull)
        // =================================================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing

            #include "ToonLitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 smoothDirOS = normalize(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(smoothDirOS);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float4 positionCS = TransformWorldToHClip(positionWS);

                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);
                float2 offset   = normalize(normalCS.xy + 1e-6);
                offset.x *= _ScreenParams.y / _ScreenParams.x;

                float scale = _OutlineWidth * 0.002;
                if (_ScreenSpaceWidth > 0.5)
                    positionCS.xy += offset * scale * positionCS.w;
                else
                    positionCS.xy += offset * scale;

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // =================================================================
        // Pass 2 : ForwardLit
        // =================================================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonVert
            #pragma fragment ToonFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "ToonLitInput.hlsl"

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
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half ToonStep(half value, half threshold, half feather)
            {
                return smoothstep(threshold - feather, threshold + feather, value);
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

            Varyings ToonVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(v.normalOS);

                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS   = nrm.normalWS;
                o.uv         = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor  = ComputeFogFactor(pos.positionCS.z);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    o.shadowCoord = GetShadowCoord(pos);
                #endif
                return o;
            }

            half4 ToonFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 albedo = baseTex.rgb;

                half3 N = normalize(i.normalWS);
                half3 V = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));

                // ── Shadow Coord ──
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord = i.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS   = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = shadowCoord;
                inputData.positionCS = i.positionCS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                // ── Main Light ──
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = dot(N, mainLight.direction);
                half lambert = NdotL * 0.5h + 0.5h;
                lambert = lerp(lambert, 1.0h, _Flatten);

                half castShadow = lerp(1.0h, mainLight.shadowAttenuation, _ReceiveShadowStrength);
                half lightTerm = lambert * castShadow;

                half ramp;
                if (_UseRampMap > 0.5h)
                    ramp = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(saturate(lightTerm), 0.5)).r;
                else
                    ramp = ToonStep(lightTerm, _ShadowThreshold, _ShadowFeather);

                half3 shadowCol = albedo * _ShadowTint.rgb;
                half mainAtten = mainLight.distanceAttenuation * castShadow;
                half3 diffuse = lerp(shadowCol, albedo, ramp) * mainLight.color * mainLight.distanceAttenuation;

                // ── Specular ──
                half3 specular = ToonSpecular(N, mainLight.direction, V, mainAtten, mainLight.color);

                // ── Additional Lights ──
                half3 additional = 0;
                #ifdef _ADDITIONAL_LIGHTS
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if defined(LIGHT_LOOP_BEGIN)
                        LIGHT_LOOP_BEGIN(pixelLightCount)
                            Light l = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                    #else
                        for (uint li = 0u; li < pixelLightCount; ++li)
                        {
                            Light l = GetAdditionalLight(li, inputData.positionWS);
                    #endif
                            half lNdotL = dot(N, l.direction) * 0.5h + 0.5h;
                            lNdotL = lerp(lNdotL, 1.0h, _Flatten);
                            half lRamp = ToonStep(lNdotL, _ShadowThreshold, _ShadowFeather);
                            half atten = l.distanceAttenuation *
                                         lerp(1.0h, l.shadowAttenuation, _ReceiveShadowStrength);

                            additional += albedo * l.color * lRamp * atten;
                            additional += ToonSpecular(N, l.direction, V, atten, l.color);
                    #if defined(LIGHT_LOOP_BEGIN)
                        LIGHT_LOOP_END
                    #else
                        }
                    #endif
                    additional *= _AdditionalLightIntensity;
                #endif

                // ── Ambient (SH) ──
                half3 shDirectional = SampleSH(N);
                half3 shFlat = SampleSH(float3(0, 1, 0));
                half3 envSH = lerp(shDirectional, shFlat, _AmbientFlatten);
                envSH = lerp(half3(1,1,1) * Luminance(envSH), envSH, _EnvironmentInfluence);
                half3 ambient = envSH * albedo * _AmbientStrength * lerp(0.35h, 1.0h, _EnvironmentInfluence);

                // ── Rim Light ──
                half rim = pow(saturate(1.0h - saturate(dot(N, V))), _RimPower);
                rim *= smoothstep(0.0h, 0.4h, saturate(NdotL));
                half3 rimColor = rim * _RimIntensity * _RimColor.rgb;

                // ── Final ──
                half3 color = (diffuse + ambient + additional + specular + rimColor) * _Brightness;
                color = MixFog(color, i.fogFactor);

                return half4(color, baseTex.a);
            }
            ENDHLSL
        }

        // =================================================================
        // Pass 3 : ShadowCaster
        // =================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "ToonLitInput.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(v.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                o.positionCS = positionCS;
                return o;
            }

            half4 ShadowFrag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // =================================================================
        // Pass 4 : DepthOnly
        // =================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "ToonLitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 DepthFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return 0;
            }
            ENDHLSL
        }

        // =================================================================
        // Pass 5 : DepthNormals
        // =================================================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing

            #include "ToonLitInput.hlsl"

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS   = normalize(TransformObjectToWorldNormal(v.normalOS));
                return o;
            }

            half4 DepthNormalsFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return half4(normalize(i.normalWS) * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
