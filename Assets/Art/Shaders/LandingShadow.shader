// Landing shadow: a soft ink spot on the ground straight below the player (LandingShadow).
// Drawn procedurally on a quad (no texture): a soft disc with a faint inked rim, so it reads as
// part of the storybook look. Transparent, no depth write, pulled toward the camera a little so
// it never fights with the ground it lies on.
Shader "SunSwitch/LandingShadow"
{
    Properties
    {
        _BaseColor ("Colour (alpha = opacity)", Color) = (0.157, 0.137, 0.129, 0.45)
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.45
        _RimStrength ("Inked Rim", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-10" "IgnoreProjector" = "True" }

        Pass
        {
            Name "LandingShadow"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Softness;
                half _RimStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fog : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * 2.0 - 1.0;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half r = length(input.uv);
                half disc = 1.0 - smoothstep(1.0 - _Softness, 1.0, r);
                half rim = smoothstep(0.62, 0.8, r) * (1.0 - smoothstep(0.8, 0.95, r));   // a thin pencil ring
                half alpha = saturate(disc * (1.0 - _RimStrength * 0.5) + rim * _RimStrength) * _BaseColor.a;
                half3 color = MixFog(_BaseColor.rgb, input.fog);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
