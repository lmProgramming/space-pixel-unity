using LM;
using UnityEngine;
using Zenject;

namespace Events.Collision
{
    public class CollisionSoundPlayer : MonoBehaviour
    {
        [SerializeField] private SoundManager soundManager;

        [SerializeField] private float minMagnitudeForClunk;
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
            if (data.pixelsDestroyed.Length > 0) soundManager.Play(SoundIdentifier.Explosion);
            if (data.SpeedDifference != null && data.SpeedDifference.Value.magnitude > minMagnitudeForClunk)
                soundManager.Play(SoundIdentifier.Collision);
        }
    }
}