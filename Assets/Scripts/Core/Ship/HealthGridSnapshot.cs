using System;
using UnityEngine;

namespace Core.Ship
{
    [Serializable]
    public class HealthGridSnapshot
    {
        public int width;
        public int height;
        public float[] values;

        public HealthGridSnapshot()
        {
        }

        public HealthGridSnapshot(int widthValue, int heightValue)
        {
            width = widthValue;
            height = heightValue;
            values = new float[width * height];
        }

        public float GetValue(int x, int y)
        {
            if (!InBounds(x, y) || values == null || values.Length != width * height)
                return 0f;
            return values[y * width + x];
        }

        public void SetValue(int x, int y, float value)
        {
            if (!InBounds(x, y) || values == null || values.Length != width * height)
                return;
            values[y * width + x] = Mathf.Max(0f, value);
        }

        private bool InBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
