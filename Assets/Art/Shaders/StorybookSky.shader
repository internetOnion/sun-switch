// Painted storybook sky for Sun Switch, blending between a Day and a Night look.
// - Three-colour gradient (top, horizon, below) per phase from the art palette
// - Soft cloud wisps above the horizon and slow paint blotches, so the sky isn't a flat fill
// - Stars that only show at night
// The Day/Night blend comes from the global _SunSwitchNight (0 = Day, 1 = Night), set by
// DayNightLook, so switching never modifies the material asset.
Shader "SunSwitch/StorybookSky"
{
    Properties
    {
        [Header(Day)]
        _DayTop ("Top", Color) = (0.42, 0.62, 0.80, 1)
        _DayHorizon ("Horizon", Color) = (1.0, 0.95, 0.85, 1)
        _DayBottom ("Below Horizon", Color) = (0.72, 0.83, 0.91, 1)
        _DayCloud ("Clouds", Color) = (1.0, 0.97, 0.90, 1)

        [Header(Night)]
        _NightTop ("Top", Color) = (0.08, 0.09, 0.22, 1)
        _NightHorizon ("Horizon", Color) = (0.40, 0.45, 0.72, 1)
        _NightBottom ("Below Horizon", Color) = (0.13, 0.15, 0.32, 1)
        _NightCloud ("Clouds", Color) = (0.30, 0.34, 0.56, 1)

        [Header(Shape)]
        _HorizonHeight ("Horizon Glow Height", Range(0.02, 1)) = 0.35
        _BelowFade ("Below Horizon Fade", Range(0.02, 1)) = 0.3
        _CloudAmount ("Cloud Amount", Range(0, 1)) = 0.45
        _CloudScale ("Cloud Scale", Float) = 1.6
        _BlotchStrength ("Paint Blotches", Range(0, 0.3)) = 0.06
        _StarDensity ("Star Density", Range(0, 1)) = 0.5
        _StarBrightness ("Star Brightness", Range(0, 3)) = 1.4
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DayTop, _DayHorizon, _DayBottom, _DayCloud;
                half4 _NightTop, _NightHorizon, _NightBottom, _NightCloud;
                half _HorizonHeight, _BelowFade, _CloudAmount;
                float _CloudScale;
                half _BlotchStrength, _StarDensity, _StarBrightness;
            CBUFFER_END

            float _SunSwitchNight;   // global, 0 = Day, 1 = Night

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

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
                f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float a = lerp(Hash13(i + float3(0, 0, 0)), Hash13(i + float3(1, 0, 0)), f.x);
                float b = lerp(Hash13(i + float3(0, 1, 0)), Hash13(i + float3(1, 1, 0)), f.x);
                float c = lerp(Hash13(i + float3(0, 0, 1)), Hash13(i + float3(1, 0, 1)), f.x);
                float d = lerp(Hash13(i + float3(0, 1, 1)), Hash13(i + float3(1, 1, 1)), f.x);
                return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float sum = 0.0, amp = 0.5;
                [unroll] for (int k = 0; k < 4; k++) { sum += amp * ValueNoise(p); p *= 2.03; amp *= 0.5; }
                return sum / 0.9375;
            }

            half3 Gradient(float y, half3 top, half3 horizon, half3 bottom)
            {
                half up = smoothstep(0.0, _HorizonHeight, y);
                half down = smoothstep(0.0, _BelowFade, -y);
                half3 sky = lerp(horizon, top, sqrt(up));
                return lerp(sky, bottom, down);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half night = saturate(_SunSwitchNight);

                half3 col = lerp(Gradient(d.y, _DayTop.rgb, _DayHorizon.rgb, _DayBottom.rgb),
                                 Gradient(d.y, _NightTop.rgb, _NightHorizon.rgb, _NightBottom.rgb), night);

                // Slow paint blotches over the whole dome.
                col *= 1.0 + (Fbm(d * 2.5) - 0.5) * 2.0 * _BlotchStrength;

                // Cloud wisps: noise stretched along the horizon, strongest just above it.
                float3 cloudP = float3(d.x * 1.0, d.y * 3.5, d.z * 1.0) * _CloudScale;
                float n = Fbm(cloudP + float3(0, 0, 7.1));
                half band = smoothstep(-0.05, 0.08, d.y) * (1.0 - smoothstep(0.25, 0.6, d.y));
                half cloud = smoothstep(1.0 - _CloudAmount, 1.0 - _CloudAmount + 0.12, n) * band;
                col = lerp(col, lerp(_DayCloud.rgb, _NightCloud.rgb, night), cloud * 0.85);

                // Stars: one candidate per direction cell, only at night and above the horizon.
                float3 cell = floor(d * 90.0);
                float h = Hash13(cell);
                float3 centre = (cell + 0.5 + (float3(Hash13(cell + 1.3), Hash13(cell + 2.7), Hash13(cell + 4.1)) - 0.5) * 0.6) / 90.0;
                float dist = length(d - normalize(centre)) * 90.0;
                half star = step(1.0 - _StarDensity * 0.08, h) * (1.0 - smoothstep(0.05, 0.2, dist));
                star *= night * smoothstep(0.05, 0.3, d.y) * (1.0 - cloud);
                col += star * _StarBrightness * (0.6 + 0.4 * Hash13(cell + 9.2));

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
