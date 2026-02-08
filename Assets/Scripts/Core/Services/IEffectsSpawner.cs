using UnityEngine;

namespace Core.Services
{
    public interface IEffectsSpawner
    {
        void SpawnExplosion(Vector2 position);
    }
}