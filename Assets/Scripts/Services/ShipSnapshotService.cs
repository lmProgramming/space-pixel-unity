using System.IO;
using Core.Services;
using Core.Ships;
using LMPro.External.IsAlive;
using Ships;
using UnityEngine;
using Zenject;

namespace Services
{
    public class ShipSnapshotService : IShipSnapshotService
    {
        private readonly DiContainer _container;
        private readonly IGameContentCatalog _gameContentCatalog;
        private readonly ModuleRestoreFactory _moduleRestoreFactory;
        private readonly SceneContextRegistry _sceneContextRegistry;

        [Inject]
        public ShipSnapshotService(DiContainer container,
            SceneContextRegistry sceneContextRegistry,
            IShipModuleCatalog shipModuleCatalog,
            IGameContentCatalog gameContentCatalog)
        {
            _container = container;
            _sceneContextRegistry = sceneContextRegistry;
            _gameContentCatalog = gameContentCatalog;
            _moduleRestoreFactory = new ModuleRestoreFactory(shipModuleCatalog);
        }

        public ShipSnapshot CaptureSnapshot(IShip ship)
        {
            if (!ship.IsAlive())
            {
                Debug.LogError("[ShipSnapshotService] Cannot capture snapshot: ship is null");
                return null;
            }

            var snapshot = new ShipSnapshot(ship.Name);
            foreach (var module in ship.AllModules)
            {
                var moduleSnapshot = module.CaptureToSnapshot(_gameContentCatalog);
                snapshot.modules.Add(moduleSnapshot);

                if (ship.CommandModule == module)
                    snapshot.commandModuleInstanceId = moduleSnapshot.instanceId;
            }

            Debug.Log(
                $"[ShipSnapshotService] Captured snapshot of '{ship.Name}' with {snapshot.modules.Count} modules");

            return snapshot;
        }

        public void ApplySnapshot(IShip ship, ShipSnapshot snapshot)
        {
            if (!ship.IsAlive())
            {
                Debug.LogError("[ShipSnapshotService] Cannot apply snapshot: ship is null");
                return;
            }

            if (snapshot == null)
            {
                Debug.LogError("[ShipSnapshotService] Cannot apply snapshot: snapshot is null");
                return;
            }

            ship.DestroyAllModulesSilently();
            CreateModulesFromSnapshot(ship, snapshot);

            Debug.Log(
                $"[ShipSnapshotService] Applied snapshot '{snapshot.shipName}' to '{ship.Name}' ({snapshot.modules.Count} modules)");
        }

        public ShipSnapshot LoadSnapshotFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var snapshot = FromJson(json);

            return snapshot;
        }

        public static ShipSnapshot FromJson(string json)
        {
            return JsonUtility.FromJson<ShipSnapshot>(json);
        }

        private void CreateModulesFromSnapshot(IShip ship, ShipSnapshot snapshot)
        {
            var injectionContainer = ResolveInjectionContainer(ship);

            var concreteShip = ship as Ship;

            Debug.Assert(concreteShip,
                "[ShipSnapshotService] Ship is not a concrete Ship class. Module restoration may fail if the ship's module attachment logic is customized.");

            foreach (var ms in snapshot.modules)
            {
                var moduleGo = _moduleRestoreFactory.CreateModuleObject(ms, concreteShip.transform);
                moduleGo.SetActive(false);
                moduleGo.transform.localPosition = ms.localPosition;
                moduleGo.transform.localRotation = ms.localRotation;

                var identity = moduleGo.GetComponent<GameObjectInstanceIdentity>();
                if (!identity)
                    identity = moduleGo.AddComponent<GameObjectInstanceIdentity>();
                identity.RestoreFromSnapshot(ms.instanceId, ms.origin, ms.archetypeId);

                var module = moduleGo.GetComponent<IModule>();
                if (module == null)
                    throw new UnityException(
                        $"[ShipSnapshotService] Failed to add a Module component for '{ms.moduleName}' (typeName: '{ms.moduleTypeName}', moduleType: {ms.moduleType}).");

                module.SetShip(ship);
                injectionContainer.InjectGameObject(moduleGo);
                module.RestoreFromSnapshot(ms, _gameContentCatalog);

                moduleGo.SetActive(true);
                moduleGo.gameObject.layer = concreteShip.gameObject.layer;
            }
        }

        private DiContainer ResolveInjectionContainer(IShip ship)
        {
            if (ship is not Component shipComponent || _sceneContextRegistry == null) return _container;
            var sceneContainer =
                _sceneContextRegistry.TryGetContainerForScene(shipComponent.gameObject.scene);

            return sceneContainer ?? _container;
        }
    }
}