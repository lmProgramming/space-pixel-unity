using System;
using Core.Services;
using Core.Ships;
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
                InstanceOrigin.CatalogPrefab => CreateFromCatalog(snapshot, parent),
                InstanceOrigin.Custom => CreateCustom(snapshot, parent),
                _ => throw new ArgumentOutOfRangeException(nameof(snapshot.origin), snapshot.origin,
                    "Unknown module origin.")
            };

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

        private GameObject CreateFromCatalog(ModuleSnapshot snapshot, Transform parent)
        {
            if (!_shipModuleCatalog.TryGetModulePrefab(snapshot.archetypeId, out var prefab) || !prefab)
                throw new UnityException(
                    $"[ModuleRestoreFactory] Missing module prefab for archetype '{snapshot.archetypeId}'.");

            var instance = _instantiator.InstantiatePrefab(prefab, parent);
            instance.name = snapshot.moduleName;
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