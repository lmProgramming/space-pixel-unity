using Core.Gameplay.Sound;
using Core.Grid;
using Core.Pixelation;
using Core.Services;
using Core.Services.Dummies;
using Pixelation;
using Pixelation.CollisionResolver;
using Services;
using Services.Sound;
using ShipFactory.UI;
using ShipFactory.UI.Runtime;
using ShipFactory.UI.ToolkitComponents;
using UI.Components.Notification;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace Context
{
    public class GameSceneInstaller : MonoInstaller
    {
        [SerializeField] private DebrisSpawner debrisSpawner;
        [SerializeField] private bool dummyDebrisSpawner;
        [SerializeField] private MapInfo mapInfo;
        [SerializeField] private ProjectilesSpawner projectilesSpawner;
        [SerializeField] private bool dummyProjectileSpawner;
        [SerializeField] private ShipService shipService;
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private EffectsSpawner effectSpawner;
        [SerializeField] private NavigationService navigationService;
        [SerializeField] private MissionService missionService;
        [SerializeField] private SkirmishSpawner skirmishSpawner;
        [SerializeField] private PixelatedRigidbodyFactory pixelatedRigidbodyFactory;
        [SerializeField] private ModuleRestoreFactory moduleRestoreFactory;

        [SerializeField]
        private bool validateAll;

        public override void InstallBindings()
        {
            if (validateAll)
            {
                if (!debrisSpawner) throw new UnityException($"Missing {nameof(debrisSpawner)}");
                if (!mapInfo) throw new UnityException($"Missing {nameof(mapInfo)}");
                if (!projectilesSpawner) throw new UnityException($"Missing {nameof(projectilesSpawner)}");
                if (!shipService) throw new UnityException($"Missing {nameof(shipService)}");
                if (!soundManager) throw new UnityException($"Missing {nameof(soundManager)}");
                if (!effectSpawner) throw new UnityException($"Missing {nameof(effectSpawner)}");
                if (!navigationService) throw new UnityException($"Missing {nameof(navigationService)}");
                if (!missionService) throw new UnityException($"Missing {nameof(missionService)}");
                if (!skirmishSpawner) throw new UnityException($"Missing {nameof(skirmishSpawner)}");
            }

            IDebrisSpawner actualDebrisSpawner =
                dummyDebrisSpawner ? new DummyDebrisSpawner() : debrisSpawner;
            IProjectilesSpawner actualProjectilesSpawner =
                dummyProjectileSpawner ? new DummyProjectileSpawner() : projectilesSpawner;

            if (debrisSpawner || dummyDebrisSpawner)
                Container.Bind<IDebrisSpawner>().FromInstance(actualDebrisSpawner).AsSingle();

            if (projectilesSpawner || dummyProjectileSpawner)
                Container.Bind<IProjectilesSpawner>().FromInstance(actualProjectilesSpawner).AsSingle();

            if (mapInfo)
                Container.Bind<IMapInfo>().FromInstance(mapInfo).AsSingle();

            if (shipService)
                Container.Bind<IShipService>().FromInstance(shipService).AsSingle();

            if (soundManager)
                Container.Bind<ISoundManager>().FromInstance(soundManager).AsSingle();

            if (effectSpawner)
                Container.Bind<IEffectsSpawner>().FromInstance(effectSpawner).AsSingle();

            if (navigationService)
                Container.Bind<INavigationService>().FromInstance(navigationService).AsSingle();

            if (missionService)
                Container.Bind<IMissionService>().FromInstance(missionService).AsSingle();

            if (skirmishSpawner)
                Container.Bind<ISkirmishSpawner>().FromInstance(skirmishSpawner).AsSingle();

            if (pixelatedRigidbodyFactory)
                Container.Bind<IPixelatedRigidbodyFactory>()
                    .FromInstance(pixelatedRigidbodyFactory)
                    .AsSingle();

            if (moduleRestoreFactory)
                Container.Bind<IModuleRestoreFactory>()
                    .FromInstance(moduleRestoreFactory)
                    .AsSingle();

            BindClasses();
            BindFactories();
        }

        private void BindClasses()
        {
            Container.Bind<IActivePlayerShipProvider>()
                .To<ActivePlayerShipProvider>()
                .AsSingle();

            Container.Bind<FreeModeBattleSpawnConfigurationProvider>()
                .AsSingle();

            Container.Bind<ProgressionBattleSpawnConfigurationProvider>()
                .AsSingle();

            Container.Bind<IBattleSpawnConfigurationProvider>()
                .To<BattleSpawnConfigurationProvider>()
                .AsSingle();
        }

        private void BindFactories()
        {
            Container.BindFactory<ITexturePixelGrid, PixelatedRigidbody, PolygonCollider2D, PixelCollisionHandler,
                PixelCollisionHandler.Factory>();

            Container
                .BindFactory<PixelCollisionHandler, IPixelatedRigidbody, PhysicsCollision, PhysicsCollision.Factory>();

            Container.BindFactory<PixelCollisionHandler, IPixelatedRigidbody, DestroyCollidingPixel,
                DestroyCollidingPixel.Factory>();

            Container.BindFactory<VisualElement, ModulePaletteController, ModulePaletteController.Factory>();

            Container.BindFactory<VisualElement, CameraInfoPanel, CameraInfoPanel.Factory>();

            Container.BindFactory<VisualElement, NotificationView, ShipFactoryFeedback, ShipFactoryCanvasController,
                ShipFactoryCanvasController.Factory>();
        }
    }
}