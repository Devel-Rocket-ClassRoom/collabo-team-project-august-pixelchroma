Shader "Custom/HologramArc"
{
    Properties
    {
        _BaseColor("Arc Color", Color) = (0.580, 0.106, 0.204, 1)
        _EdgeColor("Edge Color", Color) = (0.580, 0.106, 0.204, 1)
        _EdgeWidth("Edge Width", Range(0.02, 0.5)) = 0.38
        _CoreAlpha("Core Alpha", Range(0, 1)) = 0.05
        _EdgeIntensity("Edge Intensity", Range(1, 6)) = 1.2
        _BaseAlpha("Idle Line Alpha", Range(0, 1)) = 1
        _PulseAlpha("Pulse Alpha", Range(0, 1)) = 0.5
        _PulseSpeed("Pulse Speed", Range(0.1, 5)) = 0.9
        _PulseLength("Pulse Length", Range(0.05, 1)) = 0.35
        _PulseFalloff("Pulse Falloff", Range(1, 12)) = 3
        _Phase("Pulse Phase", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HologramArc"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                half  _EdgeWidth;
                half  _CoreAlpha;
                half  _EdgeIntensity;
                half  _BaseAlpha;
                half  _PulseAlpha;
                half  _PulseSpeed;
                half  _PulseLength;
                half  _PulseFalloff;
                half  _Phase;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // LineRenderer는 uv.y가 선의 폭을 가로지릅니다. 0.5가 한가운데입니다.
                half across = abs(i.uv.y - 0.5h) * 2.0h;

                // 가장자리는 끊김 없는 실선으로 채우고, 안쪽만 비웁니다.
                // 바깥 끝까지 1로 유지되도록 안쪽 경계에서만 부드럽게 올립니다.
                half edge = smoothstep(1.0h - _EdgeWidth, 1.0h - _EdgeWidth * 0.35h, across);
                half core = _CoreAlpha * (1.0h - smoothstep(0.0h, 1.0h - _EdgeWidth, across));

                // uv.x는 적(0)에서 아군(1) 방향입니다. 머리가 아군 쪽으로 다가오고 꼬리가 따라옵니다.
                half head = frac(_Time.y * _PulseSpeed + _Phase);
                half behind = frac(head - i.uv.x);
                half pulse = pow(saturate(1.0h - behind / _PulseLength), _PulseFalloff);

                half idle = edge * _BaseAlpha + core;
                half alpha = saturate(idle + pulse * _PulseAlpha);
                half3 rgb = lerp(_BaseColor.rgb, _EdgeColor.rgb, max(edge, pulse)) *
                            (1.0h + max(edge, pulse) * (_EdgeIntensity - 1.0h));

                return half4(rgb, alpha * _BaseColor.a * i.color.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
