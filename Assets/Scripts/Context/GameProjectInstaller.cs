using Core.Services;
using Events.Game;
using Events.Gameplay.Shooting;
using Events.UI;
using Services;
using Services.GameInput;
using UnityEngine;
using Zenject;

namespace Context
{
    public class GameProjectInstaller : MonoInstaller
    {
        [SerializeField] private ScriptableObject shipModuleCatalog;
        [SerializeField] private ScriptableObject gameContentCatalog;
        [SerializeField] private ScriptableObject skirmishSnapshotCatalog;
        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;
        [SerializeField] private PauseStateEventChannel pauseStateChannel;
        [SerializeField] private ShootingEventChannel shootingEventChannel;

        public override void InstallBindings()
        {
            if (pointerOverUiChannel == null)
                throw new UnityException("[GameProjectInstaller] Pointer Over UI event channel must be assigned.");

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

            Container.Bind<PointerOverUiEventChannel>()
                .FromInstance(pointerOverUiChannel)
                .AsSingle();

            Container.Bind<PauseStateEventChannel>()
                .FromInstance(pauseStateChannel)
                .AsSingle();

            Container.Bind<ShootingEventChannel>()
                .FromInstance(shootingEventChannel)
                .AsSingle();

            Container.Bind<IGameInput>()
                .To<GameInput>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("GameInput")
                .AsSingle()
                .NonLazy();
        }
    }
}