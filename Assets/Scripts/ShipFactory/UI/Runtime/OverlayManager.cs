using System;
using System.Collections.Generic;
using Core.Ships;
using Ships;
using UnityEngine;
using ZLinq;
using Object = UnityEngine.Object;

namespace ShipFactory.UI.Runtime
{
    public class OverlayManager : IDisposable
    {
        private const int OverlaySortingOrder = 100;
        private const int DraggedOverlaySortingOrder = OverlaySortingOrder + 1;
        private readonly Dictionary<ShipModuleSOInstanceBundle, ModuleOverlay> _bundleToOverlay = new();
        private readonly Transform _overlayRoot = new GameObject("ModuleOverlays").transform;

        public IEnumerable<ShipModuleSOInstanceBundle> AllBundles => _bundleToOverlay.Keys;

        public void Dispose()
        {
            if (_overlayRoot != null)
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

            if (overlay != null) Object.Destroy(overlay.gameObject);
            _bundleToOverlay.Remove(bundle);
        }

        private void DestroyAllOverlays()
        {
            foreach (var overlay in _bundleToOverlay.Values.AsValueEnumerable().Where(overlay => overlay != null))
                Object.Destroy(overlay.gameObject);

            _bundleToOverlay.Clear();
        }

        public void RebuildFromShip(Ship ship)
        {
            DestroyAllOverlays();

            if (ship == null) return;

            foreach (Transform child in ship.gameObject.transform)
            {
                var container = child.GetComponent<ShipModuleSOContainer>();
                var module = child.GetComponent<IModule>();

                if (container == null || container.Module == null || module == null)
                    throw new InvalidOperationException(
                        "[ShipFactoryOverlayManager] Ship child is missing module setup.");

                CreateOverlay(new ShipModuleSOInstanceBundle(child.gameObject, container.Module, module));
            }
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
    }
}