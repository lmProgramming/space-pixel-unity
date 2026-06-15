using Core.Services;
using Events.Gameplay.Collision;
using PrimeTween;
using UnityEngine;
using Zenject;

namespace Gameplay.Collision
{
    public class CollisionEffectSpawner : MonoBehaviour
    {
        private const float DefaultExplosionChance = 0.25f;
        private Camera _camera;
        [Inject] private CollisionEventChannelSO _collisionEventChannel;
        [Inject] private IEffectsSpawner _effectsSpawner;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            if (_collisionEventChannel != null) _collisionEventChannel.Register(HandleCollision);
        }

        private void OnDisable()
        {
            if (_collisionEventChannel != null) _collisionEventChannel.Unregister(HandleCollision);
        }

        private bool PlayerCollision(CollisionData data)
        {
            return data.instigator.CompareTag("Player") || (data.otherObject && data.otherObject.CompareTag("Player"));
        }

        private void HandleCollision(CollisionData data)
        {
            var count = Mathf.Min(Mathf.Max(1, data.pixelsDestroyed.Length * DefaultExplosionChance),
                data.pixelsDestroyed.Length);

            for (var i = 0; i < count; i++)
                _effectsSpawner.SpawnExplosion(data.pixelsDestroyed[i]);

            if (data.pixelsDestroyed.Length > 0 && PlayerCollision(data))
            {
                Debug.Log(data.pixelsDestroyed.Length);
                var strengthFactor = Mathf.Clamp(Mathf.Log(Mathf.Sqrt(data.pixelsDestroyed.Length), 4), 0.1f, 0.5f);
                Debug.Log(strengthFactor);
                if (strengthFactor > 0.1)
                    Tween.ShakeCamera(
                        _camera,
                        duration: strengthFactor / 5f,
                        strengthFactor: strengthFactor
                    );
            }
        }
    }
}