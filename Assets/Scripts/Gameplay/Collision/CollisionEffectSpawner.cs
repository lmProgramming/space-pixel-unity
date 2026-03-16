using Core.Services;
using Events.Collision;
using UnityEngine;
using Zenject;

namespace Gameplay.Collision
{
    public class CollisionEffectSpawner : MonoBehaviour
    {
        private const float DefaultExplosionChance = 0.25f;
        [Inject] private CollisionEventChannelSO _collisionEventChannel;
        [Inject] private IEffectsSpawner _effectsSpawner;

        private void OnEnable()
        {
            if (_collisionEventChannel != null) _collisionEventChannel.Register(HandleCollision);
        }

        private void OnDisable()
        {
            if (_collisionEventChannel != null) _collisionEventChannel.Unregister(HandleCollision);
        }

        private void HandleCollision(CollisionData data)
        {
            var count = Mathf.Min(Mathf.Max(1, data.pixelsDestroyed.Length * DefaultExplosionChance),
                data.pixelsDestroyed.Length);

            for (var i = 0; i < count; i++)
                _effectsSpawner.SpawnExplosion(data.pixelsDestroyed[i]);
        }
    }
}