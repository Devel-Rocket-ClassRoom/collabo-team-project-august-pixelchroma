// =============================================================================
// 4차시 (1) 노멀맵 + URP PBR 라이팅 직접 호출
//
// 학습 목표
//  - 탄젠트 공간(TBN)과 UnpackNormalScale
//  - SurfaceData / InputData 구조체를 채워 URP의 UniversalFragmentPBR 사용
//    -> "URP Lit 셰이더를 내가 만들 수 있다"는 감각
//  - 마스크 텍스처 채널 패킹 (R=Metallic, G=AO, A=Smoothness)
//
// 실습
//  1) _NormalScale을 음수로 -> 요철이 반전됨 (노멀맵 Y축 규약 문제 체험)
//  2) 노멀맵 텍스처 임포트 설정에서 Texture Type을 Normal Map으로 바꾸지 않으면
//     어떻게 되는지 확인 (DXT5nm 인코딩 차이)
// =============================================================================
Shader "Course/04a_CustomLitNormalMap"
{
    Properties
    {
        _BaseMap("Base Map (albedo)", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(-2, 2)) = 1

        // R: Metallic, G: Occlusion, A: Smoothness  (B는 여유 채널)
        _MaskMap("Mask (R:Metal G:AO A:Smooth)", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1

        _EmissionColor("Emission", Color) = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "04a_CustomLitInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;   // w = handedness (±1)
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                half3  normalWS    : TEXCOORD2;
                half4  tangentWS   : TEXCOORD3;  // xyz = tangent, w = sign
                half   fogFactor   : TEXCOORD4;
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord : TEXCOORD5;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                // 비트탄젠트는 프래그먼트에서 외적으로 복원 (보간 데이터 절약)
                OUT.tangentWS  = half4(vn.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(vp.positionCS.z);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    OUT.shadowCoord = GetShadowCoord(vp);
                #endif
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                half4 mask   = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, IN.uv);
                half4 nrmTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv);

                // --- TBN 구성 ---
                half3 N = normalize(IN.normalWS);
                half3 T = normalize(IN.tangentWS.xyz);
                half3 B = normalize(cross(N, T) * IN.tangentWS.w);
                half3x3 tangentToWorld = half3x3(T, B, N);

                half3 normalTS = UnpackNormalScale(nrmTex, _NormalScale);
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));

                // --- SurfaceData: "표면이 어떤 재질인가" ---
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo              = albedo.rgb;
                surfaceData.alpha               = albedo.a;
                surfaceData.metallic            = _Metallic * mask.r;
                surfaceData.smoothness          = _Smoothness * mask.a;
                surfaceData.occlusion           = lerp(1.0h, mask.g, _OcclusionStrength);
                surfaceData.normalTS            = normalTS;
                surfaceData.emission            = _EmissionColor.rgb;
                surfaceData.specular            = 0;
                surfaceData.clearCoatMask       = 0;
                surfaceData.clearCoatSmoothness = 0;

                // --- InputData: "이 픽셀이 어디에 있고 무엇을 보는가" ---
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord = IN.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS      = IN.positionWS;
                inputData.normalWS        = normalWS;
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                inputData.shadowCoord     = shadowCoord;
                inputData.fogCoord        = IN.fogFactor;
                inputData.vertexLighting  = half3(0, 0, 0);
                inputData.bakedGI         = SampleSH(normalWS);
                inputData.positionCS      = IN.positionCS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask      = half4(1, 1, 1, 1);

                // URP의 PBR 라이팅을 그대로 사용 (GGX + 반사 프로브 + 모든 광원)
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------------
        // 아래 세 Pass는 3차시에서 직접 작성한 코드를 공용 include로 재사용합니다.
        //
        // 왜 UsePass를 안 쓰는가?
        //   UsePass "다른셰이더/SHADOWCASTER" 로 가져오면 그 셰이더의
        //   UnityPerMaterial cbuffer 레이아웃이 함께 딸려와 이 셰이더와
        //   어긋납니다. SRP Batcher가 깨지고 프로퍼티 값이 뒤섞입니다.
        //   Pass를 재사용하지 말고 "코드"를 include하세요.
        // ---------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On  ZTest LEqual  ColorMask 0  Cull Back

            HLSLPROGRAM
            #pragma vertex   CourseShadowVert
            #pragma fragment CourseShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "04a_CustomLitInput.hlsl"
            #include "../../Common/CourseDepthShadowPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On  ColorMask R  Cull Back

            HLSLPROGRAM
            #pragma vertex   CourseDepthVert
            #pragma fragment CourseDepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "04a_CustomLitInput.hlsl"
            #include "../../Common/CourseDepthShadowPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On  Cull Back

            HLSLPROGRAM
            #pragma vertex   CourseDepthNormalsVert
            #pragma fragment CourseDepthNormalsFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "04a_CustomLitInput.hlsl"
            #include "../../Common/CourseDepthShadowPasses.hlsl"
            ENDHLSL
        }
    }
}
