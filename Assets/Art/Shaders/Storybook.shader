// Hand-painted "celestial storybook" look for gameplay geometry.
// - Banded light with a wobbly, painted shadow edge and tinted shadows
// - Paper grain + paint blotches from world-space noise (no UVs or textures needed,
//   so a 2 m and an 8 m platform get the same grain size)
// - Hand-drawn ink on sharp edges, from per-triangle edge distances baked by MeshInk
//   (UV1, UV2.x); UV2.y flags: 0 = no data, 1 = baked, 2 = baked + outline hull
// - Ink outline (inverted hull) for curved silhouettes, along smoothed normals in UV3
//   (skinned meshes keep them in the tangent so they bend with the bones; see MeshInk)
// - Vertex colour tints parts of a model (paving, grout, rock) under the phase colour
Shader "SunSwitch/Storybook"
{
    Properties
    {
        _BaseColor ("Base Colour", Color) = (0.6, 0.59, 0.55, 1)
        _ShadowTint ("Shadow Tint", Color) = (0.6, 0.58, 0.72, 1)
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = 0.05
        _ShadowSoftness ("Shadow Softness", Range(0.001, 0.5)) = 0.05
        _ShadowWobble ("Shadow Edge Wobble", Range(0, 1)) = 0.3
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.35

        _NoiseScale ("Noise Scale (per metre)", Float) = 1.2
        _GrainStrength ("Paper Grain", Range(0, 1)) = 0.1
        _BlotchStrength ("Paint Blotches", Range(0, 1)) = 0.18

        _RimColor ("Rim Colour", Color) = (1, 0.96, 0.87, 1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.2
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)

        _OutlineColor ("Ink Colour", Color) = (0.157, 0.137, 0.129, 1)
        _OutlineWidth ("Ink Width (metres)", Range(0, 0.1)) = 0.02
        _OutlinePixels ("Ink Min Width (pixels)", Range(0, 6)) = 1.2
        _EdgeInkWidth ("Edge Ink Width (metres)", Range(0, 0.1)) = 0.02
        _EdgeInkPixels ("Edge Ink Min Width (pixels)", Range(0, 4)) = 1.0
        _EdgeInkStrength ("Edge Ink Strength", Range(0, 1)) = 0.85
        _EdgeInkWobble ("Edge Ink Wobble", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadowTint;
            half _ShadowThreshold;
            half _ShadowSoftness;
            half _ShadowWobble;
            half _AmbientStrength;
            float _NoiseScale;
            half _GrainStrength;
            half _BlotchStrength;
            half4 _RimColor;
            half _RimStrength;
            half4 _EmissionColor;
            half4 _OutlineColor;
            float _OutlineWidth;
            float _OutlinePixels;
            float _EdgeInkWidth;
            float _EdgeInkPixels;
            half _EdgeInkStrength;
            half _EdgeInkWobble;
        CBUFFER_END

        float Hash13(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.zyx + 31.32);
            return frac((p.x + p.y) * p.z);
        }

        float ValueNoise(float3 p)
        {
            float3 i = floor(p);
            float3 f = frac(p);
            // Quintic fade: smooth across cell borders, so no visible creases or lines.
            f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
            float a = lerp(Hash13(i + float3(0, 0, 0)), Hash13(i + float3(1, 0, 0)), f.x);
            float b = lerp(Hash13(i + float3(0, 1, 0)), Hash13(i + float3(1, 1, 0)), f.x);
            float c = lerp(Hash13(i + float3(0, 0, 1)), Hash13(i + float3(1, 0, 1)), f.x);
            float d = lerp(Hash13(i + float3(0, 1, 1)), Hash13(i + float3(1, 1, 1)), f.x);
            return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
        }

        // Roughly 0..1, centred near 0.5.
        float Fbm(float3 p)
        {
            float sum = 0.0;
            float amp = 0.5;
            [unroll] for (int k = 0; k < 4; k++)
            {
                sum += amp * ValueNoise(p);
                p *= 2.03;
                amp *= 0.5;
            }
            return sum / 0.9375;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            // WebGL (GLES3) rejects the draw when the additional-light shadow map is a shadow sampler
            // without a depth-compare texture bound, which the web build has none of. This shader only
            // uses the lights for painted pools of light, so skip their shadows there.
            #if defined(SHADER_API_GLES3)
                #undef _ADDITIONAL_LIGHT_SHADOWS
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float2 inkAB      : TEXCOORD1; // metres to the edges opposite corners 0 and 1
                float2 inkC       : TEXCOORD2; // x: metres to the edge opposite corner 2, y: flags
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
                float4 ink        : TEXCOORD3; // xyz = edge distances, w = flags
                half3  tint       : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                o.ink = float4(input.inkAB, input.inkC);
                // Meshes without baked data get no edge ink and a white tint.
                o.tint = input.inkC.y > 0.5 ? input.color.rgb : half3(1, 1, 1);
                return o;
            }

            // 0..1 ink coverage near the inked edges of the current triangle, with a hand-drawn wobble.
            half EdgeInk(float4 ink, float3 positionWS)
            {
                float d = min(ink.x, min(ink.y, ink.z));
                float metresPerPixel = max(fwidth(d), 1e-4);
                float width = max(_EdgeInkWidth, _EdgeInkPixels * metresPerPixel);
                float wobble = (ValueNoise(positionWS * 7.0) - 0.5) * _EdgeInkWobble * width;
                half coverage = 1.0 - smoothstep(width - metresPerPixel, width + metresPerPixel, d + wobble);
                return coverage * _EdgeInkStrength * step(0.5, ink.w);   // no baked data = no ink
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // Painted surface: low-frequency blotches + fine paper grain.
                float blotch = Fbm(i.positionWS * _NoiseScale * 0.35);
                // Two grain sizes, offset from each other, so the grain doesn't show a grid of cells.
                float3 grainP = i.positionWS * _NoiseScale * 14.0;
                float grain = (ValueNoise(grainP) + ValueNoise(grainP * 2.17 + 17.3)) * 0.5;
                half3 albedo = _BaseColor.rgb * i.tint;
                albedo *= 1.0 + (blotch - 0.5) * 2.0 * _BlotchStrength;
                albedo *= 1.0 + (grain - 0.5) * 2.0 * _GrainStrength;

                // Main light: two bands with a hand-painted, wobbly edge.
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndl = dot(n, mainLight.direction) + (blotch - 0.5) * _ShadowWobble;
                float band = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, ndl);
                float castShadow = smoothstep(0.35, 0.65, mainLight.shadowAttenuation);
                float lit = band * castShadow;

                half3 color = albedo * lerp(_ShadowTint.rgb, 1.0, lit) * mainLight.color;
                half ao = 1.0;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                    ao = aoFactor.indirectAmbientOcclusion;
                #endif
                color += albedo * SampleSH(n) * _AmbientStrength * ao;

                // Additional lights, stepped so they read as painted pools of light.
                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = i.positionWS;
                    inputData.normalWS = n;
                    inputData.viewDirectionWS = v;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1));
                        half amount = smoothstep(0.0, 0.08, saturate(dot(n, light.direction)) * light.distanceAttenuation * light.shadowAttenuation);
                        color += albedo * light.color * amount * saturate(light.distanceAttenuation * 4.0);
                    LIGHT_LOOP_END
                #endif

                // Cream rim on the lit side, like a highlight brushed on paper.
                half rim = pow(1.0 - saturate(dot(n, v)), 4.0) * saturate(ndl + 0.3) * _RimStrength;
                color += _RimColor.rgb * rim * mainLight.color;
                color += _EmissionColor.rgb;
                color = lerp(color, _OutlineColor.rgb, EdgeInk(i.ink, i.positionWS));

                color = MixFog(color, i.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "InkOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;   // skinned meshes: smoothed normal (bent by the bones)
                float2 inkC         : TEXCOORD2; // y: 0 = no data, 1 = no hull, 2 = hull
                float4 smoothNormal : TEXCOORD3; // xyz = smoothed normal; w = 1: use the tangent instead
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  fogFactor  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Flat regions already ink their edges (silhouette included), so the hull is only
                // drawn where MeshInk flagged a curved surface, or on meshes without baked data.
                // Skipped triangles are moved outside the clip volume.
                if (input.inkC.y > 0.5 && input.inkC.y < 1.5)
                {
                    o.positionCS = float4(2.0, 2.0, 2.0, 1.0);
                    return o;
                }

                float3 dirOS = input.smoothNormal.w > 0.5 ? input.tangentOS.xyz : input.smoothNormal.xyz;
                if (dot(dirOS, dirOS) < 0.01) dirOS = input.normalOS;
                float3 dirWS = normalize(TransformObjectToWorldNormal(dirOS));
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // World width up close, but never thinner than _OutlinePixels on screen far away.
                float width = _OutlineWidth;
                if (unity_OrthoParams.w < 0.5)
                {
                    float distance = length(_WorldSpaceCameraPos - positionWS);
                    float metresPerPixel = 2.0 * distance / (UNITY_MATRIX_P[1][1] * _ScreenParams.y);
                    width = max(width, _OutlinePixels * metresPerPixel);
                }
                positionWS += dirWS * width;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                return half4(MixFog(_OutlineColor.rgb, i.fogFactor), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 Vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half Frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormal = PackNormalOctQuadEncode(n);
                    float2 remapped = saturate(octNormal * 0.5 + 0.5);
                    return half4(PackFloat2To888(remapped), 0.0);
                #else
                    return half4(n, 0.0);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
