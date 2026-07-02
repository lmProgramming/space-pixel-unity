using System.Collections.Generic;
using System.IO;
using Core.Pixelation;
using Core.Services;
using Core.Ship;
using LMPro.External.IsAlive;
using Ships;
using Ships.Modules;
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
                var moduleSnapshot = CaptureModuleSnapshot(module);
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

        public string ToJson(ShipSnapshot snapshot, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(snapshot, prettyPrint);
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

        private ModuleSnapshot CaptureModuleSnapshot(IModule module)
        {
            if (!module.Transform)
                throw new UnityException(
                    $"[ShipSnapshotService] Cannot capture snapshot for module '{module}' because its transform is null. " +
                    "Ensure that all modules have valid transforms before capturing snapshots.");

            var identity = module.Transform.GetComponent<ModuleInstanceIdentity>();
            if (!identity)
            {
                identity = module.Transform.gameObject.AddComponent<ModuleInstanceIdentity>();
                identity.EnsureAssigned(ModuleOrigin.Custom);
            }
            else if (string.IsNullOrWhiteSpace(identity.InstanceId))
            {
                identity.EnsureAssigned(identity.Origin, identity.ArchetypeId);
            }

            var typeName = module.GetType().Name;
            var moduleSnapshot = new ModuleSnapshot(identity.InstanceId, module.Transform.name, module.Type, typeName)
            {
                origin = identity.Origin,
                archetypeId = identity.ArchetypeId,
                localPosition = module.Transform.localPosition,
                localRotation = module.Transform.localRotation,
                resources = module.Resources,
                pixelatedRigidbody = module.PixelatedRigidbody.CaptureToSnapshot(),
                typePayloadJson = module.CaptureTypePayloadJson(_gameContentCatalog)
            };

            return moduleSnapshot;
        }

        private void CreateModulesFromSnapshot(IShip ship, ShipSnapshot snapshot)
        {
            var injectionContainer = ResolveInjectionContainer(ship);
            var createdModules = new List<(GameObject go, ModuleSnapshot ms, IModule module)>();

            var concreteShip = ship as Ship;

            Debug.Assert(concreteShip,
                "[ShipSnapshotService] Ship is not a concrete Ship class. Module restoration may fail if the ship's module attachment logic is customized.");

            foreach (var ms in snapshot.modules)
            {
                var moduleGo = _moduleRestoreFactory.CreateModuleObject(ms, concreteShip.transform);
                moduleGo.SetActive(false);
                moduleGo.transform.localPosition = ms.localPosition;
                moduleGo.transform.localRotation = ms.localRotation;

                var identity = moduleGo.GetComponent<ModuleInstanceIdentity>();
                if (!identity)
                    identity = moduleGo.AddComponent<ModuleInstanceIdentity>();
                identity.RestoreFromSnapshot(ms.instanceId, ms.origin, ms.archetypeId);

                var module = moduleGo.GetComponent<IModule>();
                if (module == null)
                    throw new UnityException(
                        $"[ShipSnapshotService] Failed to add a Module component for '{ms.moduleName}' (typeName: '{ms.moduleTypeName}', moduleType: {ms.moduleType}).");

                module.SetShip(ship);
                module.SetResources(ms.resources);
                module.ApplyTypePayloadJson(ms.typePayloadJson, _gameContentCatalog);
                createdModules.Add((moduleGo, ms, module));
            }

            foreach (var (moduleGo, ms, module) in createdModules)
            {
                injectionContainer.InjectGameObject(moduleGo);

                var pixelatedRigidbody = moduleGo.GetComponent<IPixelatedRigidbody>();
                pixelatedRigidbody.RestoreFromSnapshot(ms.pixelatedRigidbody);

                if (module is Engine engine)
                    engine.RestorePendingNozzleSnapshots();

                moduleGo.SetActive(true);
                moduleGo.gameObject.layer = concreteShip.gameObject.layer;
            }
        }

        private DiContainer ResolveInjectionContainer(IShip ship)
        {
            if (ship is Component shipComponent && _sceneContextRegistry != null)
            {
                var sceneContainer =
                    _sceneContextRegistry.TryGetContainerForScene(shipComponent.gameObject.scene);

                if (sceneContainer != null)
                    return sceneContainer;
            }

            return _container;
        }
    }
}