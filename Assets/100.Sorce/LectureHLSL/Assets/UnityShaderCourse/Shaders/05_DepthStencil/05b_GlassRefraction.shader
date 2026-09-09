// =============================================================================
// 5차시 (2) 유리 굴절 (_CameraOpaqueTexture)
//
// 학습 목표
//  - 씬 컬러를 다시 읽어 화면 UV를 왜곡시키는 원리
//  - Grab-pass 없이 URP가 제공하는 Opaque Texture를 쓰는 방법
//  - 왜 투명 오브젝트끼리는 서로 굴절시킬 수 없는가 (렌더 순서)
//
// [필수] URP Asset > Opaque Texture : ON
//
// 실습
//  1) 이 오브젝트 두 개를 겹쳐 보고 뒤쪽 유리가 안 보이는 이유 토론
//  2) _Blur를 켜서 프로스티드 글래스 만들기 (밉맵 LOD 활용)
// =============================================================================
Shader "Course/05b_GlassRefraction"
{
    Properties
    {
        _Tint("Tint", Color) = (0.85, 0.95, 1, 1)
        [Normal] _NormalMap("Distortion Normal", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0,3)) = 1
        _Distortion("Distortion Amount", Range(0, 0.2)) = 0.04
        _BlurLod("Blur (mip level)", Range(0, 6)) = 0
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 4
        _SpecGloss("Specular Gloss", Range(1,512)) = 250
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
            Name "Glass"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            Cull Back
            Blend Off       // 씬 컬러를 직접 읽어 합성하므로 블렌딩 불필요

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _NormalMap_ST;
                half4  _Tint;
                half   _NormalScale;
                half   _Distortion;
                half   _BlurLod;
                half   _FresnelPower;
                half   _SpecGloss;
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
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 N0 = normalize(IN.normalWS);
                half3 T  = normalize(IN.tangentWS.xyz);
                half3 B  = normalize(cross(N0, T) * IN.tangentWS.w);
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv), _NormalScale);
                half3 N = normalize(TransformTangentToWorld(normalTS, half3x3(T, B, N0)));

                half3 V = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));

                // 화면 UV를 노멀로 밀어 굴절 효과
                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float2 offset   = normalTS.xy * _Distortion;
                // 원근에 따라 왜곡량 보정 (멀리 있으면 덜 왜곡)
                offset /= max(1.0, IN.positionCS.w * 0.2);

                // _CameraOpaqueTexture는 밉맵이 있으므로 LOD로 흐림 효과 가능
                half3 refracted = SAMPLE_TEXTURE2D_X_LOD(
                    _CameraOpaqueTexture, sampler_CameraOpaqueTexture,
                    screenUV + offset, _BlurLod).rgb;

                // 프레넬 + 스페큘러
                half fresnel = pow(saturate(1.0h - saturate(dot(N, V))), _FresnelPower);
                Light mainLight = GetMainLight();
                half3 H = SafeNormalize(mainLight.direction + V);
                half  spec = pow(saturate(dot(N, H)), _SpecGloss) * 2.0h;

                half3 color = refracted * _Tint.rgb;
                color = lerp(color, color * 1.6h + 0.05h, fresnel);   // 스침각에서 밝아짐
                color += spec * mainLight.color;

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
