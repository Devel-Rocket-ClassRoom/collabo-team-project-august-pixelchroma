#ifndef COURSE_CUSTOMLIT_INPUT_INCLUDED
#define COURSE_CUSTOMLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);
TEXTURE2D(_MaskMap);    SAMPLER(sampler_MaskMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half4  _EmissionColor;
    half   _NormalScale;
    half   _Metallic;
    half   _Smoothness;
    half   _OcclusionStrength;
CBUFFER_END

#endif
