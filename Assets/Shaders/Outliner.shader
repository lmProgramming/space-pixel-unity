Shader "Custom/OutlinedSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.001, 0.05)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "RenderType"="Transparent"
        }
        Blend SrcAlpha OneMinusSrcAlpha

        // Pass 1: Render the Outline (behind the sprite)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2_f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            v2_f vert(appdata_t IN)
            {
                v2_f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return OUT;
            }

            fixed4 frag(v2_f IN) : SV_Target
            {
                // Offsets for outline extrusion
                float2 offsets[8] = {
                    float2(_OutlineThickness, 0),
                    float2(-_OutlineThickness, 0),
                    float2(0, _OutlineThickness),
                    float2(0, -_OutlineThickness),
                    float2(_OutlineThickness, _OutlineThickness),
                    float2(-_OutlineThickness, -_OutlineThickness),
                    float2(_OutlineThickness, -_OutlineThickness),
                    float2(-_OutlineThickness, _OutlineThickness)
                };

                // Check if any surrounding pixel is opaque
                for (int i = 0; i < 8; i++)
                {
                    if (tex2D(_MainTex, IN.uv + offsets[i]).a > 0.1)
                    {
                        return _OutlineColor; // Render outline
                    }
                }

                return fixed4(0, 0, 0, 0); // Fully transparent
            }
            ENDCG
        }

        // Pass 2: Render the sprite normally
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2_f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2_f vert(appdata_t IN)
            {
                v2_f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return OUT;
            }

            fixed4 frag(v2_f IN) : SV_Target
            {
                return tex2D(_MainTex, IN.uv); // Correctly samples the sprite texture
            }
            ENDCG
        }
    }
}