Shader "Custom/PixelatedStarBackgroundWithNoiseGradient"
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

        // Pixelation properties
        _PixelSize ("Pixel Size", Range(1, 64)) = 8
        _BackgroundPixelSize ("Background Pixel Size", Range(1, 32)) = 4
        _StarPixelSize ("Star Pixel Size", Range(1, 16)) = 4
        _StarSharpness ("Star Pixel Sharpness", Range(0.1, 10.0)) = 1.0
        _DitherStrength ("Dither Strength", Range(0.0, 1.0)) = 0.1
        _ColorBanding ("Color Banding", Range(1, 32)) = 8
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

            struct v2_f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
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

            // Pixelation parameters
            float _PixelSize;
            float _BackgroundPixelSize;
            float _StarPixelSize;
            float _StarSharpness;
            float _DitherStrength;
            float _ColorBanding;

            // Global variables passed from script
            float4 _PlayerPosition;
            float _CurrentZoom;

            float hash_iq(float2 p)
            {
                // Combine components with large prime numbers
                float h = dot(p, float2(127.1, 311.7));

                // Use sine function for pseudo-randomness, large multiplier for chaos
                // Add _Seed for variation based on the property
                return frac(sin(h + _Seed) * 43758.5453123);
            }

            // Version returning float2 (useful if you need two random numbers)
            float2 hash_iq2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p + _Seed) * 43758.5453123);
            }

            // 2D Perlin noise implementation for background gradient
            float2 unity_gradient_noise_dir(float2 p)
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
                float d00 = dot(unity_gradient_noise_dir(ip), fp);
                float d01 = dot(unity_gradient_noise_dir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(unity_gradient_noise_dir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(unity_gradient_noise_dir(ip + float2(1, 1)), fp - float2(1, 1));
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

            // Ordered dithering function
            float dither8x8(float2 position, float brightness)
            {
                int x = int(fmod(position.x, 8));
                int y = int(fmod(position.y, 8));

                // 8x8 Bayer matrix for ordered dithering
                const float dither[64] = {
                    0, 32, 8, 40, 2, 34, 10, 42,
                    48, 16, 56, 24, 50, 18, 58, 26,
                    12, 44, 4, 36, 14, 46, 6, 38,
                    60, 28, 52, 20, 62, 30, 54, 22,
                    3, 35, 11, 43, 1, 33, 9, 41,
                    51, 19, 59, 27, 49, 17, 57, 25,
                    15, 47, 7, 39, 13, 45, 5, 37,
                    63, 31, 55, 23, 61, 29, 53, 21
                };

                int index = y * 8 + x;
                float limit = dither[index] / 64.0;

                return step(limit, brightness);
            }

            // Pixelate the UV coordinates
            float2 pixelateUV(float2 uv, float pixelSize)
            {
                float2 pixelatedUV = floor(uv * pixelSize) / pixelSize;
                return pixelatedUV;
            }

            // Quantize colors to create banding effect
            float4 quantizeColor(float4 color, float levels)
            {
                return floor(color * levels) / levels;
            }

            v2_f vert(appdata v)
            {
                v2_f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2_f i) : SV_Target
            {
                // Apply zoom to compensate for camera zoom levels
                float zoomFactor = clamp(_CurrentZoom, _MinZoom, _MaxZoom);

                // Get world position for resolution independence
                float2 worldUV = i.worldPos.xy * 0.1; // Scale factor controls how "world-based" the coordinates are

                // Calculate parallax offset based on player position
                float2 parallaxOffset = _PlayerPosition.xy * _ParallaxStrength;

                // Apply pixelation to the world UVs with a smaller pixel size for the background
                // This makes the background less pixelated while stars remain more pixelated
                float2 bgPixelatedUV = pixelateUV(worldUV, _BackgroundPixelSize * 0.5);

                // Apply offset to UVs - different layers move at different speeds
                float2 uv1 = worldUV + parallaxOffset * 0.5; // Far stars (slow)
                float2 uv2 = worldUV + parallaxOffset * 0.7; // Mid stars
                float2 uv3 = worldUV + parallaxOffset; // Near stars (fast)

                // Multi-layered noise for background - apply less pixelation to the noise
                float2 noiseUV = pixelateUV(worldUV * _NoiseScale + parallaxOffset * 0.15, _BackgroundPixelSize * 0.25);

                // CHANGED: Use smooth time instead of pixelated time for noise movement
                float noiseTime = _Time.y * _NoiseSpeed;
                noiseUV += noiseTime;

                // Use Fractional Brownian Motion with more octaves for smoother noise pattern
                float noise1 = fbm(noiseUV, 3);
                float noise2 = fbm(noiseUV + float2(7.89, 3.25), 3);

                // First blend between color1 and color2
                fixed4 gradColor1 = lerp(_BackgroundColor, _GradientColor1, noise1 * _NoiseStrength);

                // Then blend with color2 using second noise sample
                fixed4 gradColor2 = lerp(gradColor1, _GradientColor2, noise2 * _NoiseStrength);

                // Mix colors based on noise values
                fixed4 backgroundColor = lerp(gradColor2, _GradientColor3, (noise1 * noise2) * _NoiseStrength * 0.5);

                // Apply subtle color quantization to create slight banding in background
                backgroundColor = quantizeColor(backgroundColor, _ColorBanding * 0.5);

                // Create star layers with different densities for depth effect
                float scaledDensity = _StarDensity * (1.0 / zoomFactor);

                // Create pixelated UVs specifically for stars
                float2 starUV1 = pixelateUV(uv1, _StarPixelSize);
                float2 starUV2 = pixelateUV(uv2, _StarPixelSize);
                float2 starUV3 = pixelateUV(uv3, _StarPixelSize);

                // Use pixelated UVs for grid cell identification
                float2 gridUV1 = frac(starUV1 * scaledDensity * 8.0);
                float2 cellID1 = floor(starUV1 * scaledDensity * 8.0);

                float2 gridUV2 = frac(starUV2 * scaledDensity * 12.0);
                float2 cellID2 = floor(starUV2 * scaledDensity * 12.0);

                float2 gridUV3 = frac(starUV3 * scaledDensity * 16.0);
                float2 cellID3 = floor(starUV3 * scaledDensity * 16.0);

                // Generate stars based on random hash for each layer
                float random1 = hash_iq(cellID1);
                float random2 = hash_iq(cellID2);
                float random3 = hash_iq(cellID3);

                // Separate hash for color determination to ensure color variety
                float colorRandom1 = hash_iq(cellID1 + float2(0.1, -0.1));
                float colorRandom2 = hash_iq(cellID2 + float2(0.1, -0.1));
                float colorRandom3 = hash_iq(cellID3 + float2(0.1, -0.1));

                // Hash for individual star sizes
                float sizeRandom1 = hash_iq(cellID1 + float2(-0.1, 0.1));
                float sizeRandom2 = hash_iq(cellID2 + float2(-0.1, 0.1));
                float sizeRandom3 = hash_iq(cellID3 + float2(-0.1, 0.1));

                // Determine individual star sizes between min and max
                float starSize1 = lerp(_StarSizeMin, _StarSizeMax, sizeRandom1) / zoomFactor;
                float starSize2 = lerp(_StarSizeMin, _StarSizeMax, sizeRandom2) / zoomFactor;
                float starSize3 = lerp(_StarSizeMin, _StarSizeMax, sizeRandom3) / zoomFactor;

                // Create pixelated stars with hard edges
                float dist1 = length(gridUV1 - 0.5);
                float dist2 = length(gridUV2 - 0.5);
                float dist3 = length(gridUV3 - 0.5);

                // Create pixelated stars of different sizes
                float brightness1 = step(dist1, starSize1 * _StarSharpness);
                float brightness2 = step(dist2, starSize2 * _StarSharpness);
                float brightness3 = step(dist3, starSize3 * _StarSharpness);

                // Only show some cells as stars based on the random value
                brightness1 *= step(0.85, random1); // Only ~15% of cells have stars
                brightness2 *= step(0.9, random2); // Only ~10% of cells have stars
                brightness3 *= step(0.93, random3); // Only ~7% of cells have stars

                // Twinkle group assignment (0-4)
                float twinkleGroup1 = floor(hash_iq(cellID1 + float2(0.2, 0.2)) * 5.0);
                float twinkleGroup2 = floor(hash_iq(cellID2 + float2(0.2, 0.2)) * 5.0);
                float twinkleGroup3 = floor(hash_iq(cellID3 + float2(0.2, 0.2)) * 5.0);

                // Twinkle phase offset per star
                float phaseOffset1 = hash_iq(cellID1 + float2(-0.2, -0.2)) * 6.28318; // 0 to 2pi
                float phaseOffset2 = hash_iq(cellID2 + float2(-0.2, -0.2)) * 6.28318;
                float phaseOffset3 = hash_iq(cellID3 + float2(-0.2, -0.2)) * 6.28318;

                // Keep pixelated time values for twinkling - 8 frames per second for star twinkling
                float pixelatedTime = floor(_Time.y * _TwinkleSpeed * 8) / 8;

                // Group-based twinkling with pixelated time
                float groupTime1 = pixelatedTime + twinkleGroup1 * 1.618;
                float groupTime2 = pixelatedTime + twinkleGroup2 * 1.618;
                float groupTime3 = pixelatedTime + twinkleGroup3 * 1.618;

                // Calculate twinkling with more steps (0, 0.2, 0.4, 0.6, 0.8, 1.0)
                float twinkle1 = floor(sin(groupTime1 + phaseOffset1 * _TwinkleVariation) * 2.5 + 2.5) / 5;
                float twinkle2 = floor(sin(groupTime2 + phaseOffset2 * _TwinkleVariation) * 2.5 + 2.5) / 5;
                float twinkle3 = floor(sin(groupTime3 + phaseOffset3 * _TwinkleVariation) * 2.5 + 2.5) / 5;

                // Apply twinkling to brightness
                brightness1 *= lerp(0.6, 1.0, twinkle1);
                brightness2 *= lerp(0.5, 1.0, twinkle2);
                brightness3 *= lerp(0.4, 1.0, twinkle3);

                // Choose star colors from a limited palette (pixel art style)
                fixed4 col1 = quantizeColor(lerp(lerp(_StarColor1, _StarColor2, step(0.33, colorRandom1)), _StarColor3,
                                                 step(0.66, colorRandom1)), _ColorBanding * 0.5);
                fixed4 col2 = quantizeColor(lerp(lerp(_StarColor1, _StarColor2, step(0.33, colorRandom2)), _StarColor3,
                                                step(0.66, colorRandom2)), _ColorBanding * 0.5);
                fixed4 col3 = quantizeColor(lerp(lerp(_StarColor1, _StarColor2, step(0.33, colorRandom3)), _StarColor3,
                             step(0.66, colorRandom3)),
                        _ColorBanding * 0.5);

                // Apply ordered dithering for a retro look
                float2 screenPos = i.screenPos.xy / i.screenPos.w * _ScreenParams.xy;
                float dither1 = dither8x8(screenPos, brightness1 * _DitherStrength);
                float dither2 = dither8x8(screenPos, brightness2 * _DitherStrength);
                float dither3 = dither8x8(screenPos, brightness3 * _DitherStrength);

                // Blend dithering with brightness for a better pixel art effect
                brightness1 = lerp(brightness1, dither1, _DitherStrength * 0.5);
                brightness2 = lerp(brightness2, dither2, _DitherStrength * 0.5);
                brightness3 = lerp(brightness3, dither3, _DitherStrength * 0.5);

                // Blend each star layer with the background
                fixed4 finalColor = backgroundColor;
                finalColor = lerp(finalColor, col1, brightness1);
                finalColor = lerp(finalColor, col2, brightness2);
                finalColor = lerp(finalColor, col3, brightness3);

                // Apply subtle quantization to final color for the pixelated effect
                finalColor = quantizeColor(finalColor, _ColorBanding);

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}