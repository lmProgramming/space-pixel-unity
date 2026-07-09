using Core.Constants;
using Core.Gameplay.Sound;
using Core.Services;
using Core.Ships;
using Events.Gameplay.Collision;
using Services;
using Services.Sound;
using Ships;
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
        [SerializeField] private NavigationService navigationService;
        [SerializeField] private MissionService missionService;
        [SerializeField] private SkirmishSpawner skirmishSpawner;

        [SerializeField] private PlayerShip playerShip;

        public override void InstallBindings()
        {
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

            Container.Bind<INavigationService>()
                .FromInstance(navigationService)
                .AsSingle();

            Container.Bind<IMissionService>()
                .FromInstance(missionService)
                .AsSingle();

            Container.Bind<ISkirmishSpawner>()
                .FromInstance(skirmishSpawner)
                .AsSingle();

            Container.Bind<IShip>()
                .WithId(Constants.PlayerShipId)
                .FromInstance(playerShip)
                .AsSingle();

            Container.Bind<IPixelatedRigidbodyFactory>()
                .To<PixelatedRigidbodyFactory>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.Bind<IModuleRestoreFactory>()
                .To<ModuleRestoreFactory>()
                .FromComponentInHierarchy()
                .AsSingle();
        }
    }
}