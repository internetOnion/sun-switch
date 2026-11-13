// Inactive-phase "ghost": a faint pencil sketch of the platform.
// Transparent fill, brighter edges facing away from the camera, and diagonal hatch
// lines in world space. Casts no shadows and writes no depth, so it never looks solid.
Shader "SunSwitch/StorybookGhost"
{
    Properties
    {
        _BaseColor ("Fill Colour (alpha = opacity)", Color) = (0.4, 0.44, 0.72, 0.12)
        _EdgeColor ("Edge Colour", Color) = (0.7, 0.75, 1, 1)
        _EdgeStrength ("Edge Strength", Range(0, 1)) = 0.6
        _EdgePower ("Edge Falloff", Range(0.5, 8)) = 2.5
        _HatchDensity ("Hatch Lines (per metre)", Float) = 4
        _HatchWidth ("Hatch Line Width", Range(0, 1)) = 0.12
        _HatchAlpha ("Hatch Opacity", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                half _EdgeStrength;
                half _EdgePower;
                float _HatchDensity;
                half _HatchWidth;
                half _HatchAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half edge = pow(1.0 - saturate(dot(n, v)), _EdgePower) * _EdgeStrength;

                float stripe = frac((i.positionWS.x + i.positionWS.y + i.positionWS.z) * _HatchDensity);
                half hatch = step(stripe, _HatchWidth) * _HatchAlpha;

                half3 color = lerp(_BaseColor.rgb, _EdgeColor.rgb, saturate(edge + hatch));
                half alpha = saturate(_BaseColor.a + edge + hatch);
                return half4(MixFog(color, i.fogFactor), alpha);
            }
            ENDHLSL
        }
    }
}
