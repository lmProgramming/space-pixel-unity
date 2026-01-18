using Core;
using UnityEngine;
using Zenject;

namespace Events.Collision
{
    public class CollisionSoundPlayer : MonoBehaviour
    {
        [SerializeField] private float minMagnitudeForClunk;
        [Inject] private CollisionEventChannelSO _collisionEventChannel;
        [Inject] private ISoundManager _soundManager;

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
            if (data.pixelsDestroyed.Length > 0) _soundManager.Play(SoundIdentifier.Explosion);
            if (data.SpeedDifference != null && data.SpeedDifference.Value.magnitude > minMagnitudeForClunk)
                _soundManager.Play(SoundIdentifier.Collision);
        }
    }
}