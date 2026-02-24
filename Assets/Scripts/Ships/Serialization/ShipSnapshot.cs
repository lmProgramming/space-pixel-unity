using System;
using System.Collections.Generic;
using Core.Ship;
using UnityEngine;

namespace Ships.Serialization
{
    [Serializable]
    public class ShipSnapshot
    {
        public string shipName;
        public List<ModuleSnapshot> modules = new();
        public List<ModuleConnection> connections = new();
        public int commandModuleIndex;

        public ShipSnapshot()
        {
        }

        public ShipSnapshot(string name)
        {
            shipName = name;
        }
    }

    [Serializable]
    public class ModuleSnapshot
    {
        public string moduleName;
        public ModuleType moduleType;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public PixelGridSnapshot pixelGrid;

        public ModuleSnapshot()
        {
        }

        public ModuleSnapshot(string name, ModuleType type)
        {
            moduleName = name;
            moduleType = type;
        }
    }

    [Serializable]
    public class PixelGridSnapshot
    {
        public int width;
        public int height;

        /// <summary>
        ///     Flattened array of pixel colors. Empty pixels are transparent (alpha = 0).
        ///     Row-major order: index = y * width + x
        /// </summary>
        public Color32[] pixels;

        public PixelGridSnapshot()
        {
        }

        public PixelGridSnapshot(int width, int height)
        {
            this.width = width;
            this.height = height;
            pixels = new Color32[width * height];
        }

        public Color32 GetPixel(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return default;
            return pixels[y * width + x];
        }

        public void SetPixel(int x, int y, Color32 color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;
            if (pixels == null || pixels.Length != width * height)
                return;
            pixels[y * width + x] = color;
        }

        public bool IsPixel(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;
            if (pixels == null || pixels.Length != width * height)
                return false;
            return pixels[y * width + x].a > 0;
        }

        public void RemovePixel(int x, int y)
        {
            SetPixel(x, y, new Color32(0, 0, 0, 0));
        }

        public List<Vector2Int> GetAllNonTransparentPixelPositions()
        {
            var positions = new List<Vector2Int>();
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (IsPixel(x, y))
                    positions.Add(new Vector2Int(x, y));

            return positions;
        }
    }

    [Serializable]
    public class ModuleConnection
    {
        public int moduleIndexA;
        public int moduleIndexB;

        public List<Vector2Int> connectionPointsA = new();

        public List<Vector2Int> connectionPointsB = new();

        public ModuleConnection()
        {
        }

        public ModuleConnection(int indexA, int indexB)
        {
            moduleIndexA = indexA;
            moduleIndexB = indexB;
        }
    }
}