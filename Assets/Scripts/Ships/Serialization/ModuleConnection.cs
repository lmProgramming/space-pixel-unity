using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ships.Serialization
{
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