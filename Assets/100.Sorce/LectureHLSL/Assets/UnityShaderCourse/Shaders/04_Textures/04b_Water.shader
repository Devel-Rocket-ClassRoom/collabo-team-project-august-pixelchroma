// =============================================================================
// 4차시 (2) 물 표면 셰이더  (4차시와 5차시를 잇는 예제)
//
// 학습 목표
//  - 노멀맵 두 장을 다른 속도로 스크롤해 반복감 없애기
//  - 프레넬로 반사/투과 비율 결정
//  - _CameraOpaqueTexture로 굴절, _CameraDepthTexture로 접촉선 포말
//
// [필수 설정] URP Asset 인스펙터에서
//   - Depth Texture  : ON
//   - Opaque Texture : ON
//  두 개를 켜지 않으면 굴절과 포말이 동작하지 X.
//
// 실습
//  1) _RefractStrength를 크게 -> 화면 경계에서 아티팩트 확인, 원인 찾아보기
//  2) _FoamDistance를 조절하며 얕은 물가 표현
// =============================================================================
Shader "Course/04b_Water"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor("Shallow Color", Color) = (0.2, 0.7, 0.75, 1)
        _DeepColor("Deep Color", Color) = (0.02, 0.15, 0.3, 1)
        _DepthFade("Depth Fade Distance", Float) = 4.0

        [Header(Normals)]
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0, 3)) = 0.6
        _Speed1("Layer1 Speed (XY)", Vector) = (0.03, 0.02, 0, 0)
        _Speed2("Layer2 Speed (XY)", Vector) = (-0.02, 0.035, 0, 0)
        _Tiling2("Layer2 Tiling Multiplier", Float) = 1.7

        [Header(Refraction)]
        _RefractStrength("Refraction Strength", Range(0, 0.1)) = 0.02

        [Header(Foam)]
        _FoamColor("Foam Color", Color) = (1,1,1,1)
        _FoamDistance("Foam Distance", Float) = 0.4
        _FoamSharpness("Foam Sharpness", Range(0.5, 8)) = 2

        [Header(Specular)]
        _SpecGloss("Specular Gloss", Range(1, 512)) = 200
        _SpecIntensity("Specular Intensity", Range(0, 5)) = 1.5

        [Header(Fresnel)]
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 4
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
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // 씬 깊이 / 씬 컬러 접근용 선언
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _NormalMap_ST;
                half4  _ShallowColor;
                half4  _DeepColor;
                half4  _FoamColor;
                float4 _Speed1;
                float4 _Speed2;
                float  _Tiling2;
                float  _DepthFade;
                half   _NormalScale;
                half   _RefractStrength;
                float  _FoamDistance;
                half   _FoamSharpness;
                half   _SpecGloss;
                half   _SpecIntensity;
                half   _FresnelPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                half4  tangentWS  : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                OUT.tangentWS  = half4(vn.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv         = TRANSFORM_TEX(IN.uv, _NormalMap);
                OUT.fogFactor  = ComputeFogFactor(vp.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // --- 1) 노멀 두 층 블렌드 ---
                float2 uv1 = IN.uv + _Speed1.xy * _Time.y;
                float2 uv2 = IN.uv * _Tiling2 + _Speed2.xy * _Time.y;

                half3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1), _NormalScale);
                half3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2), _NormalScale);
                // 단순 덧셈 후 정규화 (Whiteout blend보다 저렴)
                half3 normalTS = normalize(half3(n1.xy + n2.xy, n1.z * n2.z));

                half3 N0 = normalize(IN.normalWS);
                half3 T  = normalize(IN.tangentWS.xyz);
                half3 B  = normalize(cross(N0, T) * IN.tangentWS.w);
                half3 N  = normalize(TransformTangentToWorld(normalTS, half3x3(T, B, N0)));

                half3 V = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                // --- 2) 화면 UV와 씬 깊이 ---
                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                float rawDepth  = SampleSceneDepth(screenUV);
                float sceneEye  = LinearEyeDepth(rawDepth, _ZBufferParams);
                // 원근 투영에서 positionCS.w == 뷰 공간 깊이
                float surfaceEye = IN.positionCS.w;
                float waterDepth = max(0, sceneEye - surfaceEye);

                // --- 3) 굴절: 노멀로 화면 UV를 밀어서 씬 컬러 샘플 ---
                // 물 표면보다 앞에 있는 물체를 잘못 끌어오지 않도록 깊이로 가중
                float2 refractOffset = N.xz * _RefractStrength * saturate(waterDepth);
                float2 refractUV = screenUV + refractOffset;

                float rawDepthR = SampleSceneDepth(refractUV);
                float sceneEyeR = LinearEyeDepth(rawDepthR, _ZBufferParams);
                // 굴절된 위치가 물보다 앞이면 원래 UV로 되돌림 (경계 아티팩트 방지)
                refractUV = (sceneEyeR < surfaceEye) ? screenUV : refractUV;

                half3 sceneColor = SampleSceneColor(refractUV);

                // --- 4) 깊이에 따른 물 색 ---
                half  depthT = saturate(waterDepth / max(0.001, _DepthFade));
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthT);

                // 얕을수록 투명 -> 씬 컬러가 많이 보임
                half3 body = lerp(sceneColor, waterColor, saturate(depthT * 0.85 + 0.15));

                // --- 5) 프레넬 반사 ---
                half fresnel = pow(saturate(1.0h - saturate(dot(N, V))), _FresnelPower);

                // --- 6) 라이팅 ---
                Light mainLight = GetMainLight();
                half3 H = SafeNormalize(mainLight.direction + V);
                half  spec = pow(saturate(dot(N, H)), _SpecGloss) * _SpecIntensity;

                // 반사 색: 리플렉션 프로브(스카이박스)를 직접 샘플
                // GlossyEnvironmentReflection()은 URP 버전마다 인자가 달라서
                // 여기에서는 큐브맵을 직접 읽는 편이 안전 (수업용 예제이므로)
                half3 reflectDir = reflect(-V, N);
                half4 encodedRefl = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectDir, 0);
                half3 reflection  = DecodeHDREnvironment(encodedRefl, unity_SpecCube0_HDR);

                half3 color = lerp(body, reflection, fresnel * 0.7h);
                color += spec * mainLight.color;

                // --- 7) 접촉선 포말 ---
                half foam = 1.0h - saturate(waterDepth / max(0.001, _FoamDistance));
                foam = pow(foam, _FoamSharpness);
                // 노이즈 대신 노멀을 이용해 포말 경계를 흔들어 줌
                foam *= saturate(0.6h + 0.4h * normalTS.x * 4.0h);
                color = lerp(color, _FoamColor.rgb, saturate(foam));

                color = MixFog(color, IN.fogFactor);

                // 물 자체는 거의 불투명하게 합성 (씬 컬러를 이미 섞었으므로)
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
