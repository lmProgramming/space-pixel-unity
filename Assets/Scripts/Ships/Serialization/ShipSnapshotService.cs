using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Ship;
using LMPro.External.IsAlive;
using Pixelation;
using Ships.Modules;
using UnityEngine;
using Zenject;
using Module = Ships.Modules.Module;

namespace Ships.Serialization
{
    public class ShipSnapshotService : IShipSnapshotService
    {
        private static readonly Dictionary<string, Type> ModuleTypeMap = BuildModuleTypeMap();

        private readonly DiContainer _container;

        public ShipSnapshotService()
        {
        }

        [Inject]
        public ShipSnapshotService(DiContainer container)
        {
            _container = container;
        }

        public ShipSnapshot CaptureSnapshot(IShip ship)
        {
            if (!ship.IsAlive())
            {
                Debug.LogError("[ShipSnapshotService] Cannot capture snapshot: ship is null");
                return null;
            }

            var snapshot = new ShipSnapshot(ship.Name);
            var modules = ship.AllModules.ToArray();
            var moduleToIndex = new Dictionary<IModule, int>();

            for (var i = 0; i < modules.Length; i++)
            {
                var module = modules[i];
                moduleToIndex[module] = i;

                var moduleSnapshot = CaptureModuleSnapshot(module);
                snapshot.modules.Add(moduleSnapshot);

                if (ship.CommandModule != null && (Module)module == (Module)ship.CommandModule)
                    snapshot.commandModuleIndex = i;
            }

            ModuleConnectionDetector.DetectAndCaptureConnections(snapshot, modules, moduleToIndex);

            Debug.Log(
                $"[ShipSnapshotService] Captured snapshot of '{ship.Name}' with {snapshot.modules.Count} modules and {snapshot.connections.Count} connections");

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

            ship.InitializeModules();

            Debug.Log(
                $"[ShipSnapshotService] Applied snapshot '{snapshot.shipName}' to '{ship.Name}' ({snapshot.modules.Count} modules)");
        }

        public string ToJson(ShipSnapshot snapshot, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(snapshot, prettyPrint);
        }

        public ShipSnapshot FromJson(string json)
        {
            return JsonUtility.FromJson<ShipSnapshot>(json);
        }

        private static Dictionary<string, Type> BuildModuleTypeMap()
        {
            var baseType = typeof(Module);
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            var shipsAssembly = typeof(Module).Assembly;

            Type[] types;
            try
            {
                types = shipsAssembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            foreach (var type in types)
            {
                if (type == null || type.IsAbstract || !baseType.IsAssignableFrom(type))
                    continue;

                map[type.Name] = type;
            }

            return map;
        }

        private static ModuleSnapshot CaptureModuleSnapshot(IModule module)
        {
            var typeName = module.GetType().Name;
            var moduleSnapshot = new ModuleSnapshot(module.Transform.name, module.Type, typeName)
            {
                localPosition = module.Transform.localPosition,
                localRotation = module.Transform.localRotation,
                resources = module.Resources,
                moduleComponentJson = JsonUtility.ToJson(module)
            };

            var pixelatedRb = module.PixelatedRigidbody as PixelatedRigidbody;

            if (pixelatedRb && pixelatedRb.TexturePixelGrid == null)
                pixelatedRb.Setup(null, true);

            if (pixelatedRb?.TexturePixelGrid == null) return moduleSnapshot;

            var grid = pixelatedRb.TexturePixelGrid;
            var dimensions = grid.Dimensions();
            moduleSnapshot.pixelGrid = new PixelGridSnapshot(dimensions.x, dimensions.y);

            for (var y = 0; y < dimensions.y; y++)
            for (var x = 0; x < dimensions.x; x++)
            {
                var pos = new Vector2Int(x, y);
                if (grid.IsPixel(pos)) moduleSnapshot.pixelGrid.SetPixel(x, y, grid.GetValue(pos));
            }

            return moduleSnapshot;
        }

        private void CreateModulesFromSnapshot(IShip ship, ShipSnapshot snapshot)
        {
            var createdModules = new List<(GameObject go, ModuleSnapshot ms)>();

            foreach (var ms in snapshot.modules)
            {
                var moduleGo = CreateModuleGameObject(ms, (ship as Ship)?.transform);
                createdModules.Add((moduleGo, ms));

                var module = moduleGo.GetComponent<Module>();
                if (!module)
                    throw new UnityException(
                        $"[ShipSnapshotService] Failed to add a Module component for '{ms.moduleName}' (typeName: '{ms.moduleTypeName}', moduleType: {ms.moduleType}).");

                if (!string.IsNullOrEmpty(ms.moduleComponentJson))
                    JsonUtility.FromJsonOverwrite(ms.moduleComponentJson, module);

                module.SetResources(ms.resources);
            }

            foreach (var (moduleGo, ms) in createdModules)
            {
                moduleGo.SetActive(true);

                _container?.InjectGameObject(moduleGo);

                var pixelatedRb = moduleGo.GetComponent<PixelatedRigidbody>();
                ApplyPixelData(pixelatedRb, ms.pixelGrid, ms.moduleName);
            }
        }

        private static GameObject CreateModuleGameObject(ModuleSnapshot ms, Transform parent)
        {
            var moduleGo = new GameObject(ms.moduleName);
            moduleGo.SetActive(false);
            moduleGo.transform.SetParent(parent);
            moduleGo.transform.localPosition = ms.localPosition;
            moduleGo.transform.localRotation = ms.localRotation;

            moduleGo.AddComponent<SpriteRenderer>();

            var rb = moduleGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;

            moduleGo.AddComponent<PolygonCollider2D>();

            var moduleType = ResolveModuleType(ms);

            if (moduleType == typeof(LaserBeam))
                moduleGo.AddComponent<LineRenderer>();

            moduleGo.AddComponent<PixelatedRigidbody>();
            moduleGo.AddComponent(moduleType);

            return moduleGo;
        }

        private static Type ResolveModuleType(ModuleSnapshot moduleSnapshot)
        {
            var typeName = moduleSnapshot.moduleTypeName;
            if (!string.IsNullOrEmpty(typeName) && ModuleTypeMap.TryGetValue(typeName, out var type))
                return type;

            var fallbackType = ResolveFallbackModuleType(moduleSnapshot.moduleType);
            Debug.LogWarning(
                $"[ShipSnapshotService] Unknown module type name '{typeName}', falling back to '{fallbackType.Name}' for module type '{moduleSnapshot.moduleType}'.");

            return fallbackType;
        }

        private static Type ResolveFallbackModuleType(ModuleType moduleType)
        {
            return moduleType switch
            {
                ModuleType.Command => typeof(Command),
                ModuleType.Engine => typeof(Engine),
                ModuleType.Weapon => typeof(Cannon),
                _ => typeof(Basic)
            };
        }

        private static void ApplyPixelData(PixelatedRigidbody pixelatedRb, PixelGridSnapshot pg, string moduleName)
        {
            if (!pixelatedRb)
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