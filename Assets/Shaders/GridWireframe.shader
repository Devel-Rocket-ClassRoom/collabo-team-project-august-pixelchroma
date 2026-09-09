Shader "Custom/GridWireframe"
{
    Properties
    {
        _BorderColor("Border Color", Color) = (0.4, 0.6, 0.8, 0.6)
        _BorderWidth("Border Width", Range(0.01, 0.2)) = 0.05
        _FillColor("Fill Color", Color) = (0.1, 0.15, 0.2, 0.03)
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
            Name "GridWire"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BorderColor;
                half4 _FillColor;
                half  _BorderWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float dx = min(uv.x, 1.0 - uv.x);
                float dy = min(uv.y, 1.0 - uv.y);
                float d = min(dx, dy);

                float edge = smoothstep(0.0, _BorderWidth, d);
                half4 col = lerp(_BorderColor, _FillColor, edge);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
