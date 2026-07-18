using System;
using Core.Pixelation;
using UnityEngine;

namespace Core.Services.Dummies
{
    public class DummyDebrisSpawner : IDebrisSpawner
    {
        public void SpawnDebris(Vector2 position, Quaternion rotation, Color32[,] colors,
            IPixelatedRigidbody parentBody)
        {
            throw new NotSupportedException();
        }
    }
}