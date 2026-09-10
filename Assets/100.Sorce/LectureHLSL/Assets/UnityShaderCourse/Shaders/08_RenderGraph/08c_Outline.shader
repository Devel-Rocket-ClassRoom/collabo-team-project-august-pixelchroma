// =============================================================================
// 8차시 (3) 깊이 + 노멀 엣지 검출 아웃라인  ★ 이 차시의 메인 산출물
//
// 학습 목표
//  - Roberts Cross 연산자로 엣지 검출
//  - 깊이만 쓰면 놓치는 엣지 / 노멀만 쓰면 놓치는 엣지 (둘을 합치는 이유)
//  - 깊이 값을 그대로 쓰면 안 되고 선형화해야 하는 이유
//  - 거리에 따라 엣지 임계값을 보정해야 하는 이유
//
// [필수 설정]
//  URP Asset > Depth Texture : ON
//  이 셰이더를 쓰는 Renderer Feature가 Normal 버퍼를 요청해야 합니다.
//  (커스텀 Feature는 ConfigureInput, 내장 Full Screen Pass Renderer Feature는
//   Requirements 드롭다운에서 Depth + Normal 체크)
//
//  ★ 씬 오브젝트의 셰이더에 DepthNormals Pass가 없으면 그 오브젝트에는
//    테두리가 생기지 않습니다. 3차시 ToonLit에 DepthNormals Pass를 넣은 이유입니다.
//
// 실습
//  1) _DepthWeight를 0으로 -> 같은 깊이의 면 경계만 검출됨
//  2) _NormalWeight를 0으로 -> 평평한 벽 앞의 평평한 물체 경계를 놓침
//  3) _DistanceFade를 끄고 카메라를 멀리 -> 테두리가 뭉개지는 현상 관찰
// =============================================================================
Shader "Course/08c_Outline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _Thickness("Thickness (px)", Range(1, 6)) = 1
        _DepthThreshold("Depth Threshold", Range(0.0001, 0.5)) = 0.02
        _DepthWeight("Depth Weight", Range(0, 5)) = 1
        _NormalThreshold("Normal Threshold", Range(0.01, 2)) = 0.4
        _NormalWeight("Normal Weight", Range(0, 5)) = 1
        [Toggle] _DistanceFade("Normalize By Distance", Float) = 1
        _EdgeSharpness("Edge Sharpness", Range(0.5, 8)) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float  _Thickness;
            float  _DepthThreshold;
            float  _DepthWeight;
            float  _NormalThreshold;
            float  _NormalWeight;
            float  _DistanceFade;
            float  _EdgeSharpness;

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4  sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // 1픽셀 크기 * 두께
                float2 texel = _BlitTexture_TexelSize.xy * _Thickness;

                // ---- Roberts Cross: 대각선 두 쌍의 차이를 이용 ----
                //  bl ── tr        엣지 강도 = |tr - bl| + |tl - br|
                //    ╳
                //  tl ── br
                float2 uvBL = uv + float2(-texel.x, -texel.y);
                float2 uvTR = uv + float2( texel.x,  texel.y);
                float2 uvTL = uv + float2(-texel.x,  texel.y);
                float2 uvBR = uv + float2( texel.x, -texel.y);

                // ---- 깊이 엣지 ----
                // 원시 깊이는 비선형이므로 반드시 선형(eye) 깊이로 변환
                float dBL = LinearEyeDepth(SampleSceneDepth(uvBL), _ZBufferParams);
                float dTR = LinearEyeDepth(SampleSceneDepth(uvTR), _ZBufferParams);
                float dTL = LinearEyeDepth(SampleSceneDepth(uvTL), _ZBufferParams);
                float dBR = LinearEyeDepth(SampleSceneDepth(uvBR), _ZBufferParams);

                float depthCenter = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);

                float d1 = dTR - dBL;
                float d2 = dTL - dBR;
                float depthEdge = sqrt(d1 * d1 + d2 * d2);

                // 멀리 있는 물체는 깊이 차이가 커지므로 거리로 정규화
                if (_DistanceFade > 0.5)
                    depthEdge /= max(0.001, depthCenter);

                depthEdge = smoothstep(_DepthThreshold, _DepthThreshold * 2.0, depthEdge) * _DepthWeight;

                // ---- 노멀 엣지 ----
                float3 nBL = SampleSceneNormals(uvBL);
                float3 nTR = SampleSceneNormals(uvTR);
                float3 nTL = SampleSceneNormals(uvTL);
                float3 nBR = SampleSceneNormals(uvBR);

                float3 n1 = nTR - nBL;
                float3 n2 = nTL - nBR;
                float normalEdge = sqrt(dot(n1, n1) + dot(n2, n2));
                normalEdge = smoothstep(_NormalThreshold, _NormalThreshold * 2.0, normalEdge) * _NormalWeight;

                // ---- 합성 ----
                float edge = saturate(max(depthEdge, normalEdge));
                edge = pow(edge, _EdgeSharpness);

                half3 outColor = lerp(sceneColor.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
                return half4(outColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
