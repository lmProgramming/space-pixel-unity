using Core.Gameplay.Sound;
using Events.Gameplay.Shooting;
using UnityEngine;
using Zenject;

namespace Gameplay.Shooting
{
    public class ShootingSoundPlayer : MonoBehaviour
    {
        [Inject] private ShootingEventChannel _shootingEventChannel;
        [Inject] private ISoundManager _soundManager;

        private void OnEnable()
        {
            if (_shootingEventChannel != null) _shootingEventChannel.Register(HandleShooting);
        }

        private void OnDisable()
        {
            if (_shootingEventChannel != null) _shootingEventChannel.Unregister(HandleShooting);
        }

        private void HandleShooting(ShootingData data)
        {
            if (data is BulletShootingData bulletData)
                _soundManager.Play(SoundIdentifier.BulletShooting, bulletData.point);
        }
    }
}