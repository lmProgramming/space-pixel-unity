using Core.Gameplay.Sound;
using Core.Services;
using Events.Collision;
using Services;
using Services.Sound;
using UnityEngine;
using Zenject;

namespace Context
{
    public class GameSceneInstaller : MonoInstaller
    {
        [Header("Event Channels")] [SerializeField]
        private CollisionEventChannelSO physicsCollisionChannelAsset;

        [SerializeField]
        private DebrisSpawner debrisSpawner;

        [SerializeField] private MapInfo mapInfo;
        [SerializeField] private ProjectilesSpawner projectilesSpawner;
        [SerializeField] private ShipService shipService;
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private EffectsSpawner effectSpawner;

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

            Container.Bind<IDebrisSpawner>()
                .FromInstance(debrisSpawner)
                .AsSingle();

            Container.Bind<IProjectilesSpawner>()
                .FromInstance(projectilesSpawner)
                .AsSingle();

            Container.Bind<IMapInfo>()
                .FromInstance(mapInfo)
                .AsSingle();

            Container.Bind<IShipService>()
                .FromInstance(shipService)
                .AsSingle();

            Container.Bind<ISoundManager>()
                .FromInstance(soundManager)
                .AsSingle();

            Container.Bind<IEffectsSpawner>()
                .FromInstance(effectSpawner)
                .AsSingle();
        }
    }
}