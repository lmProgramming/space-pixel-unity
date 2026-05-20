using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Services;
using Core.Ship;
using Pixelation;
using Ships.Modules;
using UnityEngine;
using Module = Ships.Modules.Module;
using Object = UnityEngine.Object;

namespace Services
{
    public class ModuleRestoreFactory
    {
        private static readonly Dictionary<string, Type> ModuleTypeMap = BuildModuleTypeMap();
        private readonly IShipModuleCatalog _shipModuleCatalog;

        public ModuleRestoreFactory(IShipModuleCatalog shipModuleCatalog)
        {
            _shipModuleCatalog = shipModuleCatalog;
        }

        public GameObject CreateModuleObject(ModuleSnapshot snapshot, Transform parent)
        {
            return snapshot.origin switch
            {
                ModuleOrigin.CatalogPrefab => CreateFromCatalog(snapshot, parent),
                ModuleOrigin.Custom => CreateCustom(snapshot, parent),
                _ => throw new ArgumentOutOfRangeException(nameof(snapshot.origin), snapshot.origin,
                    "Unknown module origin.")
            };
        }

        private GameObject CreateFromCatalog(ModuleSnapshot snapshot, Transform parent)
        {
            if (!_shipModuleCatalog.TryGetModulePrefab(snapshot.archetypeId, out var prefab) || !prefab)
                throw new UnityException(
                    $"[ModuleRestoreFactory] Missing module prefab for archetype '{snapshot.archetypeId}'.");

            var instance = Object.Instantiate(prefab, parent);
            instance.name = snapshot.moduleName;
            return instance;
        }

        private static GameObject CreateCustom(ModuleSnapshot snapshot, Transform parent)
        {
            var moduleGo = new GameObject(snapshot.moduleName);
            moduleGo.transform.SetParent(parent);

            moduleGo.AddComponent<SpriteRenderer>();

            var rb = moduleGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;

            moduleGo.AddComponent<PolygonCollider2D>();

            var moduleType = ResolveModuleType(snapshot);

            if (moduleType == typeof(LaserBeam))
                moduleGo.AddComponent<LineRenderer>();

            moduleGo.AddComponent<PixelatedRigidbody>();
            moduleGo.AddComponent(moduleType);
            return moduleGo;
        }

        private static Type ResolveModuleType(ModuleSnapshot snapshot)
        {
            var typeName = snapshot.moduleTypeName;
            if (!string.IsNullOrEmpty(typeName) && ModuleTypeMap.TryGetValue(typeName, out var type))
                return type;
            return snapshot.moduleType switch
            {
                ModuleType.Command => typeof(Command),
                ModuleType.Engine => typeof(Engine),
                ModuleType.Weapon => typeof(Cannon),
                _ => typeof(Basic)
            };
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
    }
}