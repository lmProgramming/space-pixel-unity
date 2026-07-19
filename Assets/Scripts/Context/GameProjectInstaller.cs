using Core.Constants;
using Core.Services;
using Events.Camera;
using Events.Game;
using Events.Game.BattleOver;
using Events.Gameplay.Collision;
using Events.Gameplay.Shooting;
using Events.UI;
using Services;
using Services.GameInput;
using ShipFactory.Models;
using UnityEngine;
using Zenject;

namespace Context
{
    public class GameProjectInstaller : MonoInstaller
    {
        [Header("Channels")]
        [SerializeField] private CollisionEventChannelSO physicsCollisionChannel;

        [SerializeField] private CameraResetRequestEventChannel cameraResetRequestEventChannel;
        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;
        [SerializeField] private TextInputFocusEventChannel textInputFocusChannel;
        [SerializeField] private PauseStateEventChannel pauseStateChannel;
        [SerializeField] private ShootingEventChannel shootingEventChannel;

        [Header("SOs")]
        [SerializeField] private ShipModuleCatalog shipModuleCatalog;

        [SerializeField] private GameContentCatalog gameContentCatalog;
        [SerializeField] private SkirmishSnapshotCatalog skirmishSnapshotCatalog;
        [SerializeField] private GameplayConstants gameplayConstants;

        [SerializeField]
        private BattleOverEventChannel battleOverEventChannel;

        public override void InstallBindings()
        {
            if (pointerOverUiChannel == null)
                throw new UnityException("[GameProjectInstaller] Pointer Over UI event channel must be assigned.");

            if (textInputFocusChannel == null)
                throw new UnityException("[GameProjectInstaller] Text Input Focus event channel must be assigned.");

            if (pauseStateChannel == null)
                throw new UnityException("[GameProjectInstaller] Pause State event channel must be assigned.");

            if (shipModuleCatalog is not IShipModuleCatalog typedShipModuleCatalog)
                throw new UnityException(
                    "[GameProjectInstaller] Ship module catalog must implement IShipModuleCatalog.");

            if (gameContentCatalog is not IGameContentCatalog typedGameContentCatalog)
                throw new UnityException(
                    "[GameProjectInstaller] Game content catalog must implement IGameContentCatalog.");

            if (skirmishSnapshotCatalog is not ISkirmishSnapshotCatalog typedSkirmishSnapshotCatalog)
                throw new UnityException(
                    "[GameProjectInstaller] Skirmish snapshot catalog must implement ISkirmishSnapshotCatalog.");

            if (shootingEventChannel == null)
                throw new UnityException("[GameProjectInstaller] Shooting event channel must be assigned.");

            if (battleOverEventChannel == null)
                throw new UnityException("[GameProjectInstaller] Battle victory event channel must be assigned.");

            if (!cameraResetRequestEventChannel)
                throw new UnityException(
                    $"[ShipFactoryInstaller] {nameof(cameraResetRequestEventChannel)} must be assigned.");

            if (!physicsCollisionChannel)
                throw new UnityException($"Missing {nameof(physicsCollisionChannel)}");

            Container.Bind<IShipModuleCatalog>()
                .FromInstance(typedShipModuleCatalog)
                .AsSingle();

            Container.Bind<IGameContentCatalog>()
                .FromInstance(typedGameContentCatalog)
                .AsSingle();

            Container.Bind<ISkirmishSnapshotCatalog>()
                .FromInstance(typedSkirmishSnapshotCatalog)
                .AsSingle();

            Container.Bind<IShipSnapshotService>()
                .To<ShipSnapshotService>()
                .AsSingle();

            Container.Bind<IShipSnapshotRepository>()
                .To<ShipSnapshotRepository>()
                .AsSingle();

            Container.Bind<IProgressionRepository>()
                .To<ProgressionRepository>()
                .AsSingle();

            Container.Bind<PointerOverUiEventChannel>()
                .FromInstance(pointerOverUiChannel)
                .AsSingle();

            Container.Bind<TextInputFocusEventChannel>()
                .FromInstance(textInputFocusChannel)
                .AsSingle();

            Container.Bind<PauseStateEventChannel>()
                .FromInstance(pauseStateChannel)
                .AsSingle();

            Container.Bind<ShootingEventChannel>()
                .FromInstance(shootingEventChannel)
                .AsSingle();

            Container.Bind<BattleOverEventChannel>()
                .FromInstance(battleOverEventChannel)
                .AsSingle();

            Container.Bind<IGameInput>()
                .To<GameInput>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("GameInput")
                .AsSingle()
                .NonLazy();

            Container.Bind<GameplayConstants>()
                .FromInstance(gameplayConstants)
                .AsSingle();

            Container.Bind<CameraResetRequestEventChannel>()
                .FromInstance(cameraResetRequestEventChannel)
                .AsSingle();

            if (physicsCollisionChannel)
                Container.Bind<CollisionEventChannelSO>().FromInstance(physicsCollisionChannel).AsSingle();
        }
    }
}