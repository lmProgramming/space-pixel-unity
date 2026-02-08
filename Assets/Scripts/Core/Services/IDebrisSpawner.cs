using Core.Pixelation;
using UnityEngine;

namespace Core.Services
{
    public interface IDebrisSpawner
    {
        void SpawnDebris(Vector2 position, Quaternion rotation, Color32[,] colors, IPixelatedRigidbody parentBody);
    }
}