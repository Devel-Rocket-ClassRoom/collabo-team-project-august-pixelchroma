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
        _ShadowTint("Shadow Tint (albedo multiplier)", Color) = (0.66,0.71,0.86,1)
        _ReceiveShadowStrength("Receive Shadow Strength", Range(0,1)) = 0.0

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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half4  _ShadowTint;
            half   _Flatten;
            half   _ShadowThreshold;
            half   _ShadowFeather;
            half   _ReceiveShadowStrength;
            half   _AmbientStrength;
            half   _AmbientFlatten;
            half   _EnvironmentInfluence;
            half   _Brightness;
            half   _AdditionalLightIntensity;
            float  _Cull;
        CBUFFER_END

        TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);

        half ToonStep(half value, half threshold, half feather)
        {
            return smoothstep(threshold - feather, threshold + feather, value);
        }
        ENDHLSL

        // ── Main Toon Lighting ──
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

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
                float3 normalWS   : TEXCOORD2;
                float  fogCoord   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

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
                o.fogCoord   = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 ToonFrag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 albedo = baseTex.rgb;

                float3 N = normalize(i.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

                // ── Main Light ──
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.shadowCoord = shadowCoord;
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = dot(N, mainLight.direction);
                half lambert = NdotL * 0.5h + 0.5h;
                lambert = lerp(lambert, 1.0h, _Flatten);

                half castShadow = lerp(1.0h, mainLight.shadowAttenuation, _ReceiveShadowStrength);
                half lightTerm = lambert * castShadow;

                half ramp = ToonStep(lightTerm, _ShadowThreshold, _ShadowFeather);

                half3 shadowCol = albedo * _ShadowTint.rgb;
                half3 diffuse = lerp(shadowCol, albedo, ramp) * mainLight.color * mainLight.distanceAttenuation;

                // ── Ambient (SH) ──
                half3 shDirectional = SampleSH(N);
                half3 shFlat = SampleSH(float3(0, 1, 0));
                half3 envSH = lerp(shDirectional, shFlat, _AmbientFlatten);
                envSH = lerp(half3(1, 1, 1) * Luminance(envSH), envSH, _EnvironmentInfluence);
                half3 ambient = envSH * albedo * _AmbientStrength * lerp(0.35h, 1.0h, _EnvironmentInfluence);

                // ── Additional Lights ──
                half3 additional = 0;
            #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light l = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));

                    half lNdotL = dot(N, l.direction) * 0.5h + 0.5h;
                    lNdotL = lerp(lNdotL, 1.0h, _Flatten);
                    half lRamp = ToonStep(lNdotL, _ShadowThreshold, _ShadowFeather);
                    half atten = l.distanceAttenuation *
                                 lerp(1.0h, l.shadowAttenuation, _ReceiveShadowStrength);

                    additional += albedo * l.color * lRamp * atten;
                LIGHT_LOOP_END
                additional *= _AdditionalLightIntensity;
            #endif

                // ── Final ──
                half3 color = (diffuse + ambient + additional) * _Brightness;
                color = MixFog(color, i.fogCoord);

                return half4(color, baseTex.a);
            }
            ENDHLSL
        }

        // ── Shadow Caster ──
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

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

        // ── Depth Only ──
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

        // ── Depth Normals ──
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD1;
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
                o.normalWS = normalize(TransformObjectToWorldNormal(v.normalOS));
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
