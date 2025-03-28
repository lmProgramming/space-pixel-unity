using UnityEngine;

namespace LM
{
    public static class EasyImage
    {
        public static Color32[] ReorientTexture(Color32[] pixels, int width, int height, bool flipX,
            bool flipY)
        {
            if (!flipX && !flipY) return pixels;

            if (flipX)
                for (var x = 0; x < width / 2; x++)
                for (var y = 0; y < height; y++)
                    (pixels[x + y * width], pixels[width - 1 - x + y * width]) =
                        (pixels[width - 1 - x + y * width], pixels[x + y * width]);

            if (!flipY) return pixels;

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height / 2; y++)
                (pixels[x + y * width], pixels[x + (height - 1 - y) * width]) = (
                    pixels[x + (height - 1 - y) * width], pixels[x + y * width]);

            return pixels;
        }

        public static (Color32[], int, int) RotateTexture(Color32[] pixels, int width, int height, int rotation)
        {
            rotation = (4 - rotation % 4 + 4) % 4;

            if (rotation == 0) return (pixels, width, height);

            var rotatedPixels = pixels;
            int newWidth = width, newHeight = height;

            for (var r = 0; r < rotation; r++)
            {
                var tempPixels = new Color32[newWidth * newHeight];
                var index = 0;

                for (var x = 0; x < newWidth; x++)
                for (var y = newHeight - 1; y >= 0; y--)
                    tempPixels[index++] = rotatedPixels[y * newWidth + x];

                rotatedPixels = tempPixels;
                (newWidth, newHeight) = (newHeight, newWidth);
            }

            return (rotatedPixels, newWidth, newHeight);
        }
    }
}