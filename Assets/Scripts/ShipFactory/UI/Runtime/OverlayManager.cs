using System;
using System.Collections.Generic;
using Core.Services;
using Core.Ships.Module;
using ShipFactory.Helpers;
using ShipFactory.Models;
using Ships;
using UnityEngine;
using ZLinq;
using Object = UnityEngine.Object;

namespace ShipFactory.UI.Runtime
{
    public class OverlayManager : IDisposable
    {
        private const int OverlaySortingOrder = 100;
        private const int GhostSortingOrder = OverlaySortingOrder - 1;
        private const int DraggedOverlaySortingOrder = OverlaySortingOrder + 1;
        private readonly Dictionary<ShipModuleSOInstanceBundle, ModuleOverlay> _bundleToOverlay = new();
        private readonly IShipModuleCatalog _catalog;
        private readonly List<ModuleGhost> _ghosts = new();
        private readonly Transform _overlayRoot = new GameObject("ModuleOverlays").transform;

        public OverlayManager(IShipModuleCatalog moduleCatalog)
        {
            _catalog = moduleCatalog ?? throw new ArgumentNullException(nameof(moduleCatalog));
        }

        public IEnumerable<ShipModuleSOInstanceBundle> AllBundles => _bundleToOverlay.Keys;
        public IReadOnlyList<ModuleGhost> Ghosts => _ghosts;

        public void Dispose()
        {
            DestroyAllGhosts();
            if (_overlayRoot)
                Object.Destroy(_overlayRoot.gameObject);
        }

        public void CreateOverlay(ShipModuleSOInstanceBundle bundle)
        {
            var overlay = ModuleOverlay.Create(bundle, _overlayRoot, OverlaySortingOrder);
            _bundleToOverlay[bundle] = overlay;
        }

        public void RemoveOverlay(ShipModuleSOInstanceBundle bundle)
        {
            if (!_bundleToOverlay.TryGetValue(bundle, out var overlay)) return;

            if (overlay) Object.Destroy(overlay.gameObject);
            _bundleToOverlay.Remove(bundle);
        }

        private void DestroyAllOverlays()
        {
            foreach (var overlay in _bundleToOverlay.Values.AsValueEnumerable().Where(overlay => overlay != null))
                Object.Destroy(overlay.gameObject);

            _bundleToOverlay.Clear();
        }

        public void RebuildFromShip(DesignShip ship)
        {
            DestroyAllOverlays();
            DestroyAllGhosts();

            if (!ship) return;

            foreach (Transform child in ship.gameObject.transform)
            {
                var module = child.GetComponent<IModule>();
                if (module == null) continue;

                var moduleSO = ModuleCatalogResolver.ResolveModuleSO(child.gameObject, _catalog);
                CreateOverlay(new ShipModuleSOInstanceBundle(child.gameObject, moduleSO, module));
            }

            RebuildGhosts(ship);
        }

        public void RebuildGhosts(DesignShip ship)
        {
            DestroyAllGhosts();
            if (!ship || ship.Blueprint?.modules == null) return;

            var liveIds = ship.AllModules.AsValueEnumerable()
                .Where(m => m.Blueprint != null)
                .Select(m => m.Blueprint.blueprintId)
                .ToHashSet();

            var layoutParent = ship.CommandModule?.Transform ?? ship.transform;

            foreach (var blueprint in ship.Blueprint.modules.AsValueEnumerable()
                         .Where(b => b != null && !b.removedByPlayer && !liveIds.Contains(b.blueprintId)))
            {
                if (!_catalog.TryGetModuleSO(blueprint.archetypeId, out var moduleSO) || !moduleSO)
                    throw new InvalidOperationException(
                        $"[OverlayManager] Ghost archetype '{blueprint.archetypeId}' was not found in catalog.");

                _ghosts.Add(ModuleGhost.Create(blueprint, moduleSO, layoutParent, GhostSortingOrder));
            }
        }

        public void RemoveGhost(ModuleGhost ghost)
        {
            if (ghost == null) return;
            _ghosts.Remove(ghost);
            if (ghost) Object.Destroy(ghost.gameObject);
        }

        public void SetPosition(ShipModuleSOInstanceBundle bundle, Vector2 worldPos)
        {
            var transform = bundle.Instance.transform;
            var position = transform.position;
            position.x = worldPos.x;
            position.y = worldPos.y;
            transform.position = position;
            SyncTransformFromBundle(bundle);
        }

        public void BringOverlayToFront(ShipModuleSOInstanceBundle bundle)
        {
            if (_bundleToOverlay.TryGetValue(bundle, out var overlay))
                overlay.SetSortingOrder(DraggedOverlaySortingOrder);
        }

        public void ResetOverlaySortingOrder(ShipModuleSOInstanceBundle bundle)
        {
            if (_bundleToOverlay.TryGetValue(bundle, out var overlay))
                overlay.SetSortingOrder(OverlaySortingOrder);
        }

        public void SyncTransformFromBundle(ShipModuleSOInstanceBundle bundle)
        {
            if (!_bundleToOverlay.TryGetValue(bundle, out var overlay) || !overlay)
                return;

            ModuleOverlay.SyncTransformFromBundle(overlay.transform, bundle);
            var dims = bundle.ModuleSO.Dimensions;
            overlay.transform.localScale = new Vector3(dims.x, dims.y, 1f);
        }

        public void SetColor(ShipModuleSOInstanceBundle bundle, Color color)
        {
            if (_bundleToOverlay.TryGetValue(bundle, out var overlay)) overlay.SetColor(color);
        }

        public ShipModuleSOInstanceBundle FindBundleAtWorldPosition(Vector2 worldPos)
        {
            foreach (var (bundle, _) in _bundleToOverlay)
                if (ModuleRotationUtility.ContainsWorldPoint(bundle, worldPos))
                    return bundle;

            return null;
        }

        public ModuleGhost FindGhostAtWorldPosition(Vector2 worldPos)
        {
            foreach (var ghost in _ghosts.AsValueEnumerable().Where(g => g))
                if (ghost.ContainsWorldPoint(worldPos))
                    return ghost;

            return null;
        }

        public IEnumerable<(Vector2 min, Vector2 max)> GetGhostBounds()
        {
            return _ghosts.AsValueEnumerable().Where(g => g).Select(g => g.GetAxisAlignedBounds()).ToList();
        }

        private void DestroyAllGhosts()
        {
            foreach (var ghost in _ghosts.AsValueEnumerable().Where(g => g != null))
                Object.Destroy(ghost.gameObject);
            _ghosts.Clear();
        }
    }
}