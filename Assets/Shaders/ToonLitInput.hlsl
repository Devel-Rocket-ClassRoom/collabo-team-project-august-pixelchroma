#ifndef TOONLIT_INPUT_INCLUDED
#define TOONLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_RampMap);        SAMPLER(sampler_RampMap);
TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _EmissionMap_ST;
    half4  _BaseColor;
    half4  _ShadowTint;

    half4  _SpecularColor;
    half4  _RimColor;
    half4  _OutlineColor;
    half4  _EmissionColor;

    half   _Cutoff;
    half   _Flatten;
    half   _ShadowThreshold;
    half   _ShadowFeather;
    half   _ReceiveShadowStrength;
    half   _UseRampMap;

    half   _SpecGloss;
    half   _SpecThreshold;
    half   _SpecSmooth;

    half   _RimPower;
    half   _RimIntensity;

    float  _OutlineWidth;
    half   _OutlineMaxWidth;
    half   _OutlineTintByAlbedo;

    half   _AmbientStrength;
    half   _AmbientFlatten;
    half   _EnvironmentInfluence;

    half   _Brightness;
    half   _AdditionalLightIntensity;

    float  _ZOffset;
    float  _OutlineZOffset;

    float  _AlphaClip;
    float  _OutlineEnabled;
    float  _Surface;
    float  _SrcBlend;
    float  _DstBlend;
    float  _Cull;
    float  _ZWrite;
CBUFFER_END

half ToonStep(half value, half threshold, half feather)
{
    return smoothstep(threshold - feather, threshold + feather, value);
}

float4 ApplyZOffset(float4 positionCS, float offset)
{
    if (offset == 0.0) return positionCS;

    if (unity_OrthoParams.w == 0.0)
    {
        float viewZ = -positionCS.w;
        float modifiedVS_Z = min(viewZ + offset, -1e-4);
        float modifiedCS_Z = modifiedVS_Z * UNITY_MATRIX_P._m22 + UNITY_MATRIX_P._m23;
        positionCS.z = modifiedCS_Z * positionCS.w / (-modifiedVS_Z);
    }
    else
    {
    #if UNITY_REVERSED_Z
        positionCS.z += offset / _ProjectionParams.z;
    #else
        positionCS.z -= offset / _ProjectionParams.z;
    #endif
    }
    return positionCS;
}

#endif
