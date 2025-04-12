using Events.Collision;
using UnityEngine;
using Zenject;

namespace Context
{
    public class GameSceneInstaller : MonoInstaller
    {
        [Header("Event Channels")] [SerializeField]
        private CollisionEventChannelSO physicsCollisionChannelAsset;

        [Header("Services")] [SerializeField] private JunkSpawner junkSpawner;
        [SerializeField] private ProjectilesSpawner projectilesSpawner;
        [SerializeField] private MapInfo mapInfo;

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

            Container.Bind<JunkSpawner>()
                .FromInstance(junkSpawner)
                .AsSingle();

            Container.Bind<ProjectilesSpawner>()
                .FromInstance(projectilesSpawner)
                .AsSingle();

            Container.Bind<MapInfo>()
                .FromInstance(mapInfo)
                .AsSingle();
        }
    }
}