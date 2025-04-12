using UnityEngine;
using Zenject;

namespace Events.Collision
{
    public class CollisionEffectSpawner : MonoBehaviour
    {
        private const float DefaultExplosionChance = 0.25f;
        [SerializeField] private EffectsSpawner effectsSpawner;
        [Inject] private CollisionEventChannelSO _collisionEventChannel;

        private void OnEnable()
        {
            if (_collisionEventChannel != null) _collisionEventChannel.RegisterListener(HandleCollision);
        }

        private void OnDisable()
        {
            if (_collisionEventChannel != null) _collisionEventChannel.UnregisterListener(HandleCollision);
        }

        private void HandleCollision(CollisionData data)
        {
            var count = Mathf.Min(Mathf.Max(1, data.pixelsDestroyed.Length * DefaultExplosionChance),
                data.pixelsDestroyed.Length);

            for (var i = 0; i < count; i++)
                effectsSpawner.SpawnExplosion(data.pixelsDestroyed[i]);
        }
    }
}