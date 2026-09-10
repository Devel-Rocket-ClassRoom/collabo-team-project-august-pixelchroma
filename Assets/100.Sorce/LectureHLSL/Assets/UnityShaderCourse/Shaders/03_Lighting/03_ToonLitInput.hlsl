#ifndef COURSE_TOONLIT_INPUT_INCLUDED
#define COURSE_TOONLIT_INPUT_INCLUDED

// -----------------------------------------------------------------------------
// 머티리얼 프로퍼티 선언을 별도 파일로 분리하는 이유
//
// SRP Batcher는 "한 셰이더의 모든 Pass가 동일한 UnityPerMaterial 레이아웃"을
// 가질 것을 요구합니다. Pass마다 손으로 복사하면 반드시 어긋나므로
// include 파일 하나로 관리하는 것이 실무 표준입니다.
// URP 내부 셰이더도 LitInput.hlsl / SimpleLitInput.hlsl 방식으로 되어 있습니다.
// -----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
TEXTURE2D(_RampMap);        SAMPLER(sampler_RampMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half4  _ShadowTint;
    half4  _SpecularColor;
    half4  _RimColor;
    half   _RampThreshold;
    half   _RampSmooth;
    half   _UseRampMap;
    half   _SpecGloss;
    half   _SpecThreshold;
    half   _SpecSmooth;
    half   _RimPower;
    half   _RimIntensity;
    half   _AdditionalLightScale;
    half   _Cutoff;
CBUFFER_END

#endif // COURSE_TOONLIT_INPUT_INCLUDED
