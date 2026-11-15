// Full-screen ink iris for the death / respawn wipe (InkIris on the HUD).
// Ink everywhere outside a circle around _Center; the circle's edge wobbles a little like a
// brush line, with a thin darker rim. _Radius is in screen heights (0 = fully closed).
Shader "SunSwitch/UI/InkIris"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Ink", Color) = (0.157, 0.137, 0.129, 1)
        _Center ("Centre (viewport)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius (screen heights)", Float) = 2
        _Aspect ("Width / height", Float) = 1.7778
        _Wobble ("Edge wobble", Range(0, 0.2)) = 0.05
        _Softness ("Edge softness", Range(0.001, 0.05)) = 0.006

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            float4 _Center;
            float _Radius, _Aspect, _Wobble, _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Distance from the centre in screen heights, so the iris stays round.
                float2 d = (i.uv - _Center.xy) * float2(_Aspect, 1.0);
                float r = length(d);
                float a = atan2(d.y, d.x);
                float t = _Time.y;
                float edge = _Radius * (1.0 + _Wobble * (0.6 * sin(a * 7.0 + t * 2.3) + 0.4 * sin(a * 13.0 - t * 3.1)));
                float ink = smoothstep(edge - _Softness, edge + _Softness, r);
                // A thin darker rim just inside the ink, like the edge of a brush stroke.
                float rim = 1.0 - smoothstep(0.0, 0.012, abs(r - edge - 0.01));
                fixed3 col = _Color.rgb * (1.0 - 0.35 * rim);
                return fixed4(col, saturate(ink) * _Color.a * i.color.a);
            }
            ENDCG
        }
    }
}
