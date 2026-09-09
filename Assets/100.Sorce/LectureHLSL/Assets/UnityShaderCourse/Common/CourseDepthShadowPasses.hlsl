#ifndef COURSE_DEPTH_SHADOW_PASSES_INCLUDED
#define COURSE_DEPTH_SHADOW_PASSES_INCLUDED

// -----------------------------------------------------------------------------
// ShadowCaster / DepthOnly / DepthNormals Pass의 공용 구현
//
// 3차시에서는 이 코드를 셰이더 안에 직접 써 보고(원리 이해),
// 4차시 이후에는 이 파일을 include해서 재사용합니다.
//
// 사용법 (각 Pass 안에서):
//   #include ".../Core.hlsl"
//   #include "내셰이더Input.hlsl"     // UnityPerMaterial cbuffer
//   #include "CourseDepthShadowPasses.hlsl"   // Shadows.hlsl은 이 파일이 알아서 include
//   #pragma vertex CourseShadowVert / #pragma fragment CourseShadowFrag  등
//
// 주의: UsePass 구문으로 다른 셰이더의 Pass를 가져오면 UnityPerMaterial
//       레이아웃이 어긋나 SRP Batcher가 깨지거나 값이 잘못 읽힙니다.
//       그래서 "Pass를 재사용"하지 않고 "코드를 include"합니다.
// -----------------------------------------------------------------------------

// CourseShadowVert가 ApplyShadowBias를 참조하므로, 이 파일을 include하는 Pass가
// DepthOnly/DepthNormals라서 Shadows.hlsl을 안 넣었더라도 컴파일이 되어야 합니다.
// (HLSL은 실제로 쓰이지 않는 함수라도 같은 파일 안에 있으면 심볼을 확인하기 때문에,
//  "ShadowCaster Pass에만 필요"라는 이유로 include를 생략하면 undeclared identifier가 납니다.)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// ============================ ShadowCaster ===================================
// URP가 그림자 드로우마다 전역으로 설정하는 값 (cbuffer에 넣으면 안 됨)
float3 _LightDirection;
float3 _LightPosition;

struct CourseShadowAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct CourseShadowVaryings
{
    float4 positionCS : SV_POSITION;
};

CourseShadowVaryings CourseShadowVert(CourseShadowAttributes IN)
{
    CourseShadowVaryings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);

    float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

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

    OUT.positionCS = positionCS;
    return OUT;
}

half4 CourseShadowFrag(CourseShadowVaryings IN) : SV_Target
{
    return 0;
}

// ============================== DepthOnly ====================================
struct CourseDepthAttributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct CourseDepthVaryings
{
    float4 positionCS : SV_POSITION;
};

CourseDepthVaryings CourseDepthVert(CourseDepthAttributes IN)
{
    CourseDepthVaryings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
    return OUT;
}

half4 CourseDepthFrag(CourseDepthVaryings IN) : SV_Target
{
    return 0;
}

// ============================ DepthNormals ===================================
struct CourseDepthNormalsAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct CourseDepthNormalsVaryings
{
    float4 positionCS : SV_POSITION;
    half3  normalWS   : TEXCOORD0;
};

CourseDepthNormalsVaryings CourseDepthNormalsVert(CourseDepthNormalsAttributes IN)
{
    CourseDepthNormalsVaryings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
    return OUT;
}

half4 CourseDepthNormalsFrag(CourseDepthNormalsVaryings IN) : SV_Target
{
    // URP의 _CameraNormalsTexture는 월드 공간 노멀을 담습니다.
    return half4(normalize(IN.normalWS), 0);
}

#endif // COURSE_DEPTH_SHADOW_PASSES_INCLUDED
