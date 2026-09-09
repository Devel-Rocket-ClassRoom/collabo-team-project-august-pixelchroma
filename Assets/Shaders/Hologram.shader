Shader "Custom/Hologram"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _RimColor("Rim Color", Color) = (0.25, 0.45, 0.85, 1)
        _RimPower("Rim Power", Range(1, 10)) = 5
        _RimBlinkPower("Blink Speed", Range(1, 10)) = 5
        _RimBlinkRange("Blink Min Alpha", Range(0.5, 1.0)) = 0.5
        _LineCount("Scan Line Count", Range(1, 30)) = 5
        _LinePower("Scan Line Sharpness", Range(1, 50)) = 30
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Hologram"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _RimColor;
                half   _RimPower;
                half   _RimBlinkPower;
                half   _RimBlinkRange;
                half   _LineCount;
                half   _LinePower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float3 positionWS  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(v.normalOS);

                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS   = nrm.normalWS;
                o.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float edgeX = min(uv.x, 1.0 - uv.x);
                float edgeY = min(uv.y, 1.0 - uv.y);
                float edgeDist = min(edgeX, edgeY);
                float edge = 1.0 - smoothstep(0.0, 0.06, edgeDist);

                float scan = frac((i.positionWS.x + i.positionWS.z) * _LineCount * 0.5 - _Time.y);
                scan = pow(scan, _LinePower);

                half3 N = normalize(i.normalWS);
                half3 V = normalize(i.viewDirWS);
                half rim = pow(1.0h - saturate(dot(N, V)), _RimPower);

                half alpha = edge * 0.85 + scan * 0.3 + rim * 0.5 + 0.06;

                half blink = sin(_Time.y * _RimBlinkPower) * 0.5 + _RimBlinkRange;
                alpha = saturate(alpha * blink);

                return half4(_RimColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
