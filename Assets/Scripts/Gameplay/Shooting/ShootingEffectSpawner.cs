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
            if (_camera == null)
                throw new UnityException("[ShootingEffectSpawner] Main camera is required but not found.");
        }

        private void OnEnable()
        {
            if (_shootingEventChannel != null) _shootingEventChannel.Register(Handle);
        }

        private void OnDisable()
        {
            if (_shootingEventChannel != null) _shootingEventChannel.Unregister(Handle);
        }

        private void Handle(ShootingData data)
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