Shader "Custom/StarBackgroundWithNoiseGradient"
{
    Properties
    {
        _MainTex ("Star Texture", 2D) = "white" {}
        _StarColor1 ("Primary Star Color", Color) = (1,1,1,1)
        _StarColor2 ("Secondary Star Color", Color) = (0.7,0.8,1,1)
        _StarColor3 ("Tertiary Star Color", Color) = (1,0.8,0.6,1)
        _BackgroundColor ("Background Base Color", Color) = (0,0,0,1)
        _GradientColor1 ("Gradient Color 1", Color) = (0.05,0,0.1,1)
        _GradientColor2 ("Gradient Color 2", Color) = (0,0.05,0.1,1)
        _GradientColor3 ("Gradient Color 3", Color) = (0.02,0.02,0.05,1)
        _NoiseScale ("Noise Scale", Range(0.001, 0.1)) = 0.01
        _NoiseStrength ("Noise Strength", Range(0.0, 1.0)) = 0.5
        _NoiseSpeed ("Noise Movement Speed", Range(0.0, 0.1)) = 0.01
        _StarDensity ("Star Density", Range(0.1, 10.0)) = 2.0
        _ParallaxStrength ("Parallax Strength", Range(0.001, 0.1)) = 0.02
        _StarSizeMin ("Star Size Min", Range(0.001, 0.1)) = 0.005
        _StarSizeMax ("Star Size Max", Range(0.001, 0.1)) = 0.02
        _TwinkleSpeed ("Twinkle Speed", Range(0, 2.0)) = 0.5
        _TwinkleVariation ("Twinkle Variation", Range(0, 1.0)) = 0.7
        _Seed ("Random Seed", Range(0, 1000)) = 42
        _MinZoom ("Min Zoom Level", Range(0.1, 100.0)) = 0.5
        _MaxZoom ("Max Zoom Level", Range(0.1, 100.0)) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _StarColor1;
            fixed4 _StarColor2;
            fixed4 _StarColor3;
            fixed4 _BackgroundColor;
            fixed4 _GradientColor1;
            fixed4 _GradientColor2;
            fixed4 _GradientColor3;
            float _NoiseScale;
            float _NoiseStrength;
            float _NoiseSpeed;
            float _StarDensity;
            float _ParallaxStrength;
            float _StarSizeMin;
            float _StarSizeMax;
            float _TwinkleSpeed;
            float _TwinkleVariation;
            float _Seed;
            float _MinZoom;
            float _MaxZoom;

            // Global variables passed from script
            float4 _PlayerPosition;
            float _CurrentZoom;

            // Hash function for pseudo-random number generation
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y + _Seed);
            }

            // Hash function for color determination
            float hashColor(float2 p)
            {
                p = frac(p * float2(273.31, 781.57));
                p += dot(p, p + 92.78);
                return frac(p.x * p.y + _Seed * 1.23);
            }

            // Hash for star size
            float hashSize(float2 p)
            {
                p = frac(p * float2(187.37, 639.19));
                p += dot(p, p + 135.33);
                return frac(p.x * p.y + _Seed * 0.87);
            }

            // Hash for twinkle phase
            float hashTwinklePhase(float2 p)
            {
                p = frac(p * float2(398.47, 519.29));
                p += dot(p, p + 217.53);
                return frac(p.x * p.y + _Seed * 1.87);
            }

            // Hash for twinkle group
            float hashTwinkleGroup(float2 p)
            {
                p = frac(p * float2(527.63, 291.41));
                p += dot(p, p + 159.27);
                return frac(p.x * p.y + _Seed * 0.53);
            }

            // 2D Perlin noise implementation for background gradient
            float2 unity_gradientNoise_dir(float2 p)
            {
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float unity_gradientNoise(float2 p)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(unity_gradientNoise_dir(ip), fp);
                float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }

            // Fractional Brownian Motion (multi-layered noise)
            float fbm(float2 p, int octaves)
            {
                float sum = 0;
                float amp = 0.5;
                float freq = 1.0;

                for (int i = 0; i < octaves; i++)
                {
                    sum += unity_gradientNoise(p * freq) * amp;
                    amp *= 0.5;
                    freq *= 2.0;
                }

                return sum;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Apply zoom to compensate for camera zoom levels
                float zoomFactor = clamp(_CurrentZoom, _MinZoom, _MaxZoom);

                // Get world position for resolution independence
                float2 worldUV = i.worldPos.xy * 0.1; // Scale factor controls how "world-based" the coordinates are

                // Calculate parallax offset based on player position
                float2 parallaxOffset = _PlayerPosition.xy * _ParallaxStrength;

                // Apply offset to UVs - different layers move at different speeds
                float2 uv1 = worldUV + parallaxOffset * 0.5; // Far stars (slow)
                float2 uv2 = worldUV + parallaxOffset * 0.7; // Mid stars
                float2 uv3 = worldUV + parallaxOffset; // Near stars (fast)

                // Multi-layered noise for background - very slow movement with player position
                float2 noiseUV = worldUV * _NoiseScale + parallaxOffset * 0.15;
                noiseUV += _Time.y * _NoiseSpeed; // Additional slow movement over time

                // Use Fractional Brownian Motion for more complex noise pattern
                float noise1 = fbm(noiseUV, 3);
                float noise2 = fbm(noiseUV + float2(7.89, 3.25), 3);

                // First blend between color1 and color2
                fixed4 gradColor1 = lerp(_BackgroundColor, _GradientColor1, noise1 * _NoiseStrength);

                // Then blend with color2 using second noise sample
                fixed4 gradColor2 = lerp(gradColor1, _GradientColor2, noise2 * _NoiseStrength);

                // Mix colors based on noise values
                // Add a subtle third color variation using combined noise
                fixed4 backgroundColor = lerp(gradColor2, _GradientColor3, (noise1 * noise2) * _NoiseStrength * 0.5);

                // Create star layers with different densities for depth effect
                // Scale density with zoom for consistent look
                float scaledDensity = _StarDensity * (1.0 / zoomFactor);

                float2 gridUV1 = frac(uv1 * scaledDensity * 8.0);
                float2 cellID1 = floor(uv1 * scaledDensity * 8.0);

                float2 gridUV2 = frac(uv2 * scaledDensity * 12.0);
                float2 cellID2 = floor(uv2 * scaledDensity * 12.0);

                float2 gridUV3 = frac(uv3 * scaledDensity * 16.0);
                float2 cellID3 = floor(uv3 * scaledDensity * 16.0);

                // Generate stars based on random hash for each layer
                float random1 = hash(cellID1);
                float random2 = hash(cellID2);
                float random3 = hash(cellID3);

                // Separate hash for color determination to ensure color variety
                float colorRandom1 = hashColor(cellID1);
                float colorRandom2 = hashColor(cellID2);
                float colorRandom3 = hashColor(cellID3);

                // Hash for individual star sizes
                float sizeRandom1 = hashSize(cellID1);
                float sizeRandom2 = hashSize(cellID2);
                float sizeRandom3 = hashSize(cellID3);

                // Determine individual star sizes between min and max
                float starSize1 = lerp(_StarSizeMin, _StarSizeMax, sizeRandom1) / zoomFactor;
                float starSize2 = lerp(_StarSizeMin, _StarSizeMax, sizeRandom2) / zoomFactor;
                float starSize3 = lerp(_StarSizeMin, _StarSizeMax, sizeRandom3) / zoomFactor;

                // Create stars using individual sizes
                float brightness1 = smoothstep(starSize1, 0.0, length(gridUV1 - 0.5));
                float brightness2 = smoothstep(starSize2, 0.0, length(gridUV2 - 0.5));
                float brightness3 = smoothstep(starSize3, 0.0, length(gridUV3 - 0.5));

                // Only show some cells as stars based on the random value
                brightness1 *= step(0.85, random1); // Only ~15% of cells have stars
                brightness2 *= step(0.9, random2); // Only ~10% of cells have stars
                brightness3 *= step(0.93, random3); // Only ~7% of cells have stars

                // Twinkle group assignment (0-4)
                float twinkleGroup1 = floor(hashTwinkleGroup(cellID1) * 5);
                float twinkleGroup2 = floor(hashTwinkleGroup(cellID2) * 5);
                float twinkleGroup3 = floor(hashTwinkleGroup(cellID3) * 5);

                // Twinkle phase offset per star
                float phaseOffset1 = hashTwinklePhase(cellID1) * 6.28; // 0 to 2π
                float phaseOffset2 = hashTwinklePhase(cellID2) * 6.28;
                float phaseOffset3 = hashTwinklePhase(cellID3) * 6.28;

                // Group-based twinkling
                // Base time offset for each group - they twinkle independently
                float groupTime1 = _Time.y * _TwinkleSpeed + twinkleGroup1 * 1.618;
                float groupTime2 = _Time.y * _TwinkleSpeed + twinkleGroup2 * 1.618;
                float groupTime3 = _Time.y * _TwinkleSpeed + twinkleGroup3 * 1.618;

                // Calculate twinkling - add individualized phase offset to group timing
                float twinkle1 = sin(groupTime1 + phaseOffset1 * _TwinkleVariation) * 0.5 + 0.5;
                float twinkle2 = sin(groupTime2 + phaseOffset2 * _TwinkleVariation) * 0.5 + 0.5;
                float twinkle3 = sin(groupTime3 + phaseOffset3 * _TwinkleVariation) * 0.5 + 0.5;

                // Apply twinkling to brightness
                brightness1 *= lerp(0.6, 1.0, twinkle1);
                brightness2 *= lerp(0.5, 1.0, twinkle2);
                brightness3 *= lerp(0.4, 1.0, twinkle3);

                // Choose star colors - truly separate for each star
                fixed4 col1 = lerp(lerp(_StarColor1, _StarColor2, step(0.33, colorRandom1)), _StarColor3,
                                   step(0.66, colorRandom1));
                fixed4 col2 = lerp(lerp(_StarColor1, _StarColor2, step(0.33, colorRandom2)), _StarColor3,
                   step(0.66, colorRandom2));
                fixed4 col3 = lerp(lerp(_StarColor1, _StarColor2, step(0.33, colorRandom3)), _StarColor3,
                  step(0.66, colorRandom3));

                // Blend each star layer with the background
                fixed4 finalColor = backgroundColor;
                finalColor = lerp(finalColor, col1, brightness1);
                finalColor = lerp(finalColor, col2, brightness2);
                finalColor = lerp(finalColor, col3, brightness3);

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}