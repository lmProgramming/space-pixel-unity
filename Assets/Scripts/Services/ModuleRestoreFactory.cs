using System;
using Core.Services;
using Core.Ships;
using Core.Ships.Blueprints;
using Core.Ships.Snapshots.Module;
using Pixelation;
using UnityEngine;
using Zenject;

namespace Services
{
    public class ModuleRestoreFactory : MonoBehaviour, IModuleRestoreFactory
    {
        private DiContainer _container;
        private IInstantiator _instantiator;
        private IPixelatedRigidbodyFactory _pixelatedRigidbodyFactory;
        private IShipModuleCatalog _shipModuleCatalog;

        public GameObject CreateModuleShell(ModuleSnapshot snapshot, Transform parent)
        {
            var moduleGo = snapshot.origin switch
            {
                InstanceOrigin.CatalogPrefab => CreateFromCatalog(snapshot.archetypeId, snapshot.moduleName, parent),
                InstanceOrigin.Custom => CreateCustom(snapshot, parent),
                _ => throw new ArgumentOutOfRangeException(nameof(snapshot.origin), snapshot.origin,
                    "Unknown module origin.")
            };

            _container.InjectGameObject(moduleGo);
            return moduleGo;
        }

        public GameObject CreateModuleShellFromBlueprint(ModuleBlueprint blueprint, Transform parent)
        {
            if (blueprint == null) throw new ArgumentNullException(nameof(blueprint));
            if (string.IsNullOrWhiteSpace(blueprint.archetypeId))
                throw new UnityException("[ModuleRestoreFactory] Blueprint archetypeId is required.");

            var moduleGo = CreateFromCatalog(blueprint.archetypeId, blueprint.archetypeId, parent);
            _container.InjectGameObject(moduleGo);
            return moduleGo;
        }

        [Inject]
        private void Construct(
            IInstantiator instantiator,
            DiContainer container,
            IShipModuleCatalog shipModuleCatalog,
            IPixelatedRigidbodyFactory pixelatedRigidbodyFactory)
        {
            _instantiator = instantiator;
            _container = container;
            _shipModuleCatalog = shipModuleCatalog;
            _pixelatedRigidbodyFactory = pixelatedRigidbodyFactory;
        }

        private GameObject CreateFromCatalog(string archetypeId, string moduleName, Transform parent)
        {
            if (!_shipModuleCatalog.TryGetModulePrefab(archetypeId, out var prefab) || !prefab)
                throw new UnityException(
                    $"[ModuleRestoreFactory] Missing module prefab for archetype '{archetypeId}'.");

            var instance = _instantiator.InstantiatePrefab(prefab, parent);
            instance.name = moduleName;
            return instance;
        }

        private GameObject CreateCustom(ModuleSnapshot snapshot, Transform parent)
        {
            var builder = _pixelatedRigidbodyFactory.CreatePixelatedRigidbodyShell(
                    parent,
                    snapshot.moduleName,
                    Vector3.zero,
                    Quaternion.identity,
                    RigidbodyType2D.Dynamic)
                .WithPixelatedRigidbody<PixelatedRigidbody>();

            var moduleType = SnapshotComponentRegistry.ResolveModuleType(snapshot.concreteModuleType);
            _container.InstantiateComponent(moduleType, builder.GameObject);

            return builder.GameObject;
        }
    }
}