using Core.Services;
using Events.Gameplay.Shooting;
using PrimeTween;
using UnityEngine;
using Zenject;

namespace Gameplay.Shooting
{
    public class ShootingEffectSpawner : MonoBehaviour
    {
        private Camera _camera;
        [Inject] private IEffectsSpawner _effectsSpawner;
        [Inject] private ShootingEventChannel _shootingEventChannel;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            if (_shootingEventChannel != null) _shootingEventChannel.Register(HandleCollision);
        }

        private void OnDisable()
        {
            if (_shootingEventChannel != null) _shootingEventChannel.Unregister(HandleCollision);
        }

        private void HandleCollision(ShootingData data)
        {
            if (data is BulletShootingData bulletData)
                Tween.ShakeCamera(
                    _camera,
                    duration: 0.1f,
                    strengthFactor: Mathf.Sqrt(bulletData.Momentum) / 100
                );
        }
    }
}