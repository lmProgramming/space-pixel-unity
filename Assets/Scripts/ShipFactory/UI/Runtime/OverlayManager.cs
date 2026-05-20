using System;
using System.Collections.Generic;
using Core.Ship;
using Ships;
using UnityEngine;
using ZLinq;
using Object = UnityEngine.Object;

namespace ShipFactory.UI.Runtime
{
    public class OverlayManager : IDisposable
    {
        private const int OverlaySortingOrder = 100;
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
            bundle.Instance.transform.position = worldPos;
            if (_bundleToOverlay.TryGetValue(bundle, out var overlay)) overlay.transform.position = worldPos;
        }

        public void SetColor(ShipModuleSOInstanceBundle bundle, Color color)
        {
            if (_bundleToOverlay.TryGetValue(bundle, out var overlay)) overlay.SetColor(color);
        }

        public ShipModuleSOInstanceBundle FindBundleAtWorldPosition(Vector2 worldPos)
        {
            foreach (var (bundle, _) in _bundleToOverlay)
            {
                var pos = (Vector2)bundle.Instance.transform.position;
                var halfDims = (Vector2)bundle.ModuleSO.Dimensions / 2f;

                if (worldPos.x >= pos.x - halfDims.x && worldPos.x <= pos.x + halfDims.x &&
                    worldPos.y >= pos.y - halfDims.y && worldPos.y <= pos.y + halfDims.y)
                    return bundle;
            }

            return null;
        }
    }
}