using Events.Collision;
using UnityEngine;
using Zenject;

namespace Context
{
    public class GameSceneInstaller : MonoInstaller
    {
        [Header("Event Channels")] [SerializeField]
        private CollisionEventChannelSO physicsCollisionChannelAsset;

        public override void InstallBindings()
        {
            if (!physicsCollisionChannelAsset)
            {
                Debug.LogError("PhysicsCollisionChannel Asset is not assigned in GameSceneInstaller!", this);
                return;
            }

            Container.Bind<CollisionEventChannelSO>()
                .FromInstance(physicsCollisionChannelAsset)
                .AsSingle();
        }
    }
}