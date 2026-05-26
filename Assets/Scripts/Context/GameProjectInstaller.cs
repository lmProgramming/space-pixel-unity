using Core.Services;
using Services;
using UnityEngine;
using Zenject;

namespace Context
{
    public class GameProjectInstaller : MonoInstaller
    {
        [SerializeField] private ScriptableObject shipModuleCatalog;
        [SerializeField] private ScriptableObject gameContentCatalog;
        [SerializeField] private ScriptableObject skirmishSnapshotCatalog;

        public override void InstallBindings()
        {
            if (shipModuleCatalog is not IShipModuleCatalog typedShipModuleCatalog)
                throw new UnityException("[GameProjectInstaller] Ship module catalog must implement IShipModuleCatalog.");

            if (gameContentCatalog is not IGameContentCatalog typedGameContentCatalog)
                throw new UnityException("[GameProjectInstaller] Game content catalog must implement IGameContentCatalog.");

            if (skirmishSnapshotCatalog is not ISkirmishSnapshotCatalog typedSkirmishSnapshotCatalog)
                throw new UnityException(
                    "[GameProjectInstaller] Skirmish snapshot catalog must implement ISkirmishSnapshotCatalog.");

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
        }
    }
}