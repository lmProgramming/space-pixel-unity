using System;
using UnityEngine;

namespace Core.Ships
{
    [Serializable]
    public class ArmorGridSnapshot
    {
        public int width;
        public int height;
        public byte[] values;

        public ArmorGridSnapshot()
        {
        }

        public ArmorGridSnapshot(int widthValue, int heightValue)
        {
            width = widthValue;
            height = heightValue;
            values = new byte[width * height];
        }

        public byte GetValue(int x, int y)
        {
            if (!InBounds(x, y) || values == null || values.Length != width * height)
                return 0;
            return values[y * width + x];
        }

        public void SetValue(int x, int y, byte value)
        {
            if (!InBounds(x, y) || values == null || values.Length != width * height)
                return;
            values[y * width + x] = value;
        }

        public Color32 GetColor(int x, int y)
        {
            var v = GetValue(x, y);
            return new Color32(v, v, v, 255);
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}