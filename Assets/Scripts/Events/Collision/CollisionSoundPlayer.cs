using LM;
using UnityEngine;

namespace Events.Collision
{
    public class CollisionSoundPlayer : MonoBehaviour
    {
        [SerializeField] private CollisionEventChannelSO collisionEventChannel;

        [SerializeField] private SoundManager soundManager;

        private void OnEnable()
        {
            if (collisionEventChannel != null) collisionEventChannel.RegisterListener(HandleCollision);
        }

        private void OnDisable()
        {
            if (collisionEventChannel != null) collisionEventChannel.UnregisterListener(HandleCollision);
        }

        private void HandleCollision(CollisionData data)
        {
            soundManager.Play("explosion");
        }
    }
}