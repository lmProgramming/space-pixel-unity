using LM;
using UnityEngine;
using Zenject;

namespace Events.Collision
{
    public class CollisionSoundPlayer : MonoBehaviour
    {
        [SerializeField] private SoundManager soundManager;
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
            soundManager.Play(SoundIdentifier.Explosion);
        }
    }
}