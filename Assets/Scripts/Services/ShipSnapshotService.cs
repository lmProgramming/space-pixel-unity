using System.Collections.Generic;
using System.IO;
using Core.Pixelation;
using Core.Services;
using Core.Ship;
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

            ship.DestroyAllModules();
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
            var identity = module.Transform.GetComponent<ModuleInstanceIdentity>();
            if (identity == null)
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
                defaultPixelHealth = module.PixelatedRigidbody.DefaultPixelHealthForSnapshot,
                maxArmorHealth = module.PixelatedRigidbody.MaxArmorHealthForSnapshot,
                typePayloadJson = module.CaptureTypePayloadJson(_gameContentCatalog)
            };

            var grid = module.PixelatedRigidbody.TexturePixelGrid;
            if (grid == null) return moduleSnapshot;
            var dimensions = grid.Dimensions();
            moduleSnapshot.colorGrid = new PixelGridSnapshot(dimensions.x, dimensions.y);

            for (var y = 0; y < dimensions.y; y++)
            for (var x = 0; x < dimensions.x; x++)
            {
                var pos = new Vector2Int(x, y);
                if (grid.IsPixel(pos)) moduleSnapshot.colorGrid.SetPixel(x, y, grid.GetValue(pos));
            }

            moduleSnapshot.armorGrid = module.PixelatedRigidbody.CaptureArmorGridSnapshot();
            moduleSnapshot.healthGrid = module.PixelatedRigidbody.CaptureHealthGridSnapshot();

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

                module.Setup(ship);
                module.SetResources(ms.resources);
                module.ApplyTypePayloadJson(ms.typePayloadJson, _gameContentCatalog);
                createdModules.Add((moduleGo, ms, module));
            }

            foreach (var (moduleGo, ms, _) in createdModules)
            {
                injectionContainer.InjectGameObject(moduleGo);

                var pixelatedRigidbody = moduleGo.GetComponent<IPixelatedRigidbody>();
                ApplyPixelData(pixelatedRigidbody, ms.colorGrid, ms.moduleName);
                pixelatedRigidbody.ApplyArmorGridSnapshot(ms.armorGrid);
                pixelatedRigidbody.ApplyHealthGridSnapshot(ms.healthGrid);

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

        private static void ApplyPixelData(IPixelatedRigidbody pixelatedRb, PixelGridSnapshot pg, string moduleName)
        {
            if (pixelatedRb == null)
                throw new UnityException(
                    $"[ShipSnapshotService] PixelatedRigidbody is null on module '{moduleName}'");

            if (pg == null || pg.width == 0 || pg.height == 0)
                throw new UnityException(
                    $"[ShipSnapshotService] Module '{moduleName}' has no pixel data in snapshot. " +
                    "Re-capture the snapshot — old snapshots with empty pixel grids are not supported.");

            var colors = new Color32[pg.width, pg.height];

            for (var y = 0; y < pg.height; y++)
            for (var x = 0; x < pg.width; x++)
                colors[x, y] = pg.GetPixel(x, y);

            pixelatedRb.SetTextureFromColors(colors);
        }
    }
}