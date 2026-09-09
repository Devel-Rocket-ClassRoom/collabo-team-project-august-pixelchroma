#ifndef TOONLIT_INPUT_INCLUDED
#define TOONLIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_RampMap);        SAMPLER(sampler_RampMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;

    half   _Flatten;
    half   _ShadowThreshold;
    half   _ShadowFeather;
    half4  _ShadowTint;
    half   _ReceiveShadowStrength;
    half   _UseRampMap;

    half4  _SpecularColor;
    half   _SpecGloss;
    half   _SpecThreshold;
    half   _SpecSmooth;

    half4  _RimColor;
    half   _RimPower;
    half   _RimIntensity;

    half4  _OutlineColor;
    float  _OutlineWidth;
    float  _ScreenSpaceWidth;

    half   _AmbientStrength;
    half   _AmbientFlatten;
    half   _EnvironmentInfluence;

    half   _Brightness;
    half   _AdditionalLightIntensity;
    float  _Cull;
CBUFFER_END

#endif
