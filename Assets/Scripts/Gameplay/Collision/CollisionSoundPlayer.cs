using Core.Gameplay.Sound;
using Events.Gameplay.Collision;
using UnityEngine;
using Zenject;

namespace Gameplay.Collision
{
    public class CollisionSoundPlayer : MonoBehaviour
    {
        [SerializeField] private float minMagnitudeForClunk;
        [Inject] private CollisionEventChannelSO _collisionEventChannel;
        [Inject] private ISoundManager _soundManager;

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
            if (data.pixelsDestroyed.Length > 0) _soundManager.Play(SoundIdentifier.Explosion, data.contactPoint);

            if (data.SpeedDifference != null)
            {
                Debug.Log(data.SpeedDifference.Value.magnitude);
                if (data.SpeedDifference.Value.magnitude > minMagnitudeForClunk)
                    _soundManager.Play(SoundIdentifier.Collision, data.contactPoint);
            }
        }
    }
}