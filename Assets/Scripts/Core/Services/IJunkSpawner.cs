using Core.Pixelation;
using UnityEngine;

namespace Core.Services
{
    public interface IJunkSpawner
    {
        void SpawnJunk(Vector2 position, Quaternion rotation, Color32[,] colors, IPixelatedRigidbody parentBody);
    }
}