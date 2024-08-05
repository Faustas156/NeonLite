Shader "NeonLite/Hue Shift"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Shift ("Hue Shift", Range(0, 1)) = 0 
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float4 vertex : SV_POSITION;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Shift;
            float4 _ClipRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed3 hueShift(fixed3 color, float hueAdjust) {
                const fixed3  kRGBToYPrime = fixed3(0.299, 0.587, 0.114);
                const fixed3  kRGBToI      = fixed3(0.596, -0.275, -0.321);
                const fixed3  kRGBToQ      = fixed3(0.212, -0.523, 0.311);

                const fixed3  kYIQToR     = fixed3(1.0, 0.956, 0.621);
                const fixed3  kYIQToG     = fixed3(1.0, -0.272, -0.647);
                const fixed3  kYIQToB     = fixed3(1.0, -1.107, 1.704);

                float   YPrime  = dot(color, kRGBToYPrime);
                float   I       = dot(color, kRGBToI);
                float   Q       = dot(color, kRGBToQ);
                float   hue     = atan2(Q, I);
                float   chroma  = sqrt(I * I + Q * Q);

                hue += hueAdjust;

                Q = chroma * sin(hue);
                I = chroma * cos(hue);

                fixed3 yIQ = fixed3(YPrime, I, Q);

                return fixed3(dot(yIQ, kYIQToR), dot(yIQ, kYIQToG), dot(yIQ, kYIQToB));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                const float PI = 3.14159265;

                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                col.rgb = hueShift(col, _Shift * 360 * (PI/180));

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                return saturate(col);
            }
            ENDHLSL
        }
    }
}
