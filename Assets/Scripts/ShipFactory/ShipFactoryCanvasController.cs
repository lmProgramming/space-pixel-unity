using System;
using System.Collections.Generic;
using Core.Ship;
using Ships;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ShipFactory
{
    public class ShipFactoryCanvasController
    {
        private const int SnapUnits = 8;

        private const string SelectedModuleClass = "placed-module--selected";

        // How many screen pixels equal one world unit at the default camera zoom.
        // Overlay elements are repositioned every frame via UpdateOverlays().
        private const float OverlayPixelsPerUnit = 32f;
        private readonly Camera _cam;

        private readonly VisualElement _canvasArea;
        private readonly VisualElement _dragGhost;
        private readonly Dictionary<VisualElement, IModule> _placedModuleElements = new();

        private Vector2 _ghostSizePx;
        private bool _isDraggingModule;
        private GameObject _pendingPrefab;
        private VisualElement _selectedElement;
        private Ship _ship;

        public ShipFactoryCanvasController(VisualElement root)
        {
            _cam = Camera.main;

            var canvasViewport = root.Q<VisualElement>("canvas-viewport");
            _canvasArea = root.Q<VisualElement>("canvas-area");
            _dragGhost = root.Q<VisualElement>("drag-ghost");

            if (canvasViewport == null || _canvasArea == null || _dragGhost == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCanvasController] Required canvas elements not found in UXML!");

            // Canvas area is transparent — clicks pass through to game unless we're dragging
            _canvasArea.pickingMode = PickingMode.Ignore;
            canvasViewport.pickingMode = PickingMode.Ignore;

            RegisterDragEvents(root);
        }

        public void SetShip(Ship ship)
        {
            _ship = ship;
            RebuildOverlaysFromShip();
        }

        public void BeginModuleDrop(GameObject prefab, Vector2 pointerScreenPos)
        {
            _pendingPrefab = prefab;
            _isDraggingModule = true;

            _ghostSizePx = EstimateScreenSizePx(prefab);

            _dragGhost.style.width = _ghostSizePx.x;
            _dragGhost.style.height = _ghostSizePx.y;
            _dragGhost.RemoveFromClassList("hidden");

            MoveGhostToPointer(pointerScreenPos);
        }

        public void UpdateOverlays()
        {
            foreach (var (element, module) in _placedModuleElements)
                PositionOverlayElement(element, module);
        }

        private void RegisterDragEvents(VisualElement root)
        {
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDraggingModule) return;
            MoveGhostToPointer(evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDraggingModule) return;

            _dragGhost.AddToClassList("hidden");
            _isDraggingModule = false;

            if (_ship == null)
            {
                Debug.LogWarning("[ShipFactory] No ship assigned — cannot place module.");
                _pendingPrefab = null;
                return;
            }

            var worldPos = ScreenToSnappedWorldPosition(evt.position);
            PlaceModuleAtWorldPosition(_pendingPrefab, worldPos);
            _pendingPrefab = null;
        }

        private void MoveGhostToPointer(Vector2 screenPos)
        {
            var snapped = ScreenToSnappedWorldPosition(screenPos);
            var snappedScreen = WorldToScreenPos(snapped);

            _dragGhost.style.left = snappedScreen.x - _ghostSizePx.x / 2f;
            _dragGhost.style.top = snappedScreen.y - _ghostSizePx.y / 2f;
        }

        private static Vector2 ScreenToSnappedWorldPosition(Vector2 screenPos)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[ShipFactory] No main camera found!");
                return Vector2.zero;
            }

            var unityScreenPos = new Vector3(screenPos.x, Screen.height - screenPos.y, cam.nearClipPlane);
            var worldPos = (Vector2)cam.ScreenToWorldPoint(unityScreenPos);
            return SnapToGrid(worldPos);
        }

        private Vector2 WorldToScreenPos(Vector2 worldPos)
        {
            if (!_cam) return Vector2.zero;

            var screenPos = _cam.WorldToScreenPoint(worldPos);
            // Convert Unity screen (y bottom-up) to UI Toolkit panel (y top-down)
            return new Vector2(screenPos.x, Screen.height - screenPos.y);
        }

        private static Vector2 SnapToGrid(Vector2 worldPosition)
        {
            return new Vector2(
                Mathf.Round(worldPosition.x / SnapUnits) * SnapUnits,
                Mathf.Round(worldPosition.y / SnapUnits) * SnapUnits);
        }

        private void PlaceModuleAtWorldPosition(GameObject prefab, Vector2 worldPosition)
        {
            var instance = (GameObject)Object.Instantiate((Object)prefab, _ship.transform);
            var module = instance.GetComponent<IModule>();

            if (module == null)
            {
                Debug.LogError($"[ShipFactory] Prefab '{prefab.name}' has no IModule component!", prefab);
                Object.Destroy(instance);
                return;
            }

            var localPosition = (Vector2)_ship.transform.InverseTransformPoint(worldPosition);
            _ship.AddModule(module);
            module.SetLocalPosition(localPosition);

            AddOverlayElement(module);
        }

        private void AddOverlayElement(IModule module)
        {
            var element = new VisualElement();
            element.AddToClassList("placed-module");

            var label = new Label(module.Transform.name);
            label.AddToClassList("placed-module-label");
            element.Add(label);

            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                SelectModule(element, module);
                evt.StopPropagation();
            });

            PositionOverlayElement(element, module);
            _canvasArea.pickingMode = PickingMode.Position; // temporarily allow picking for placed overlays
            _canvasArea.Add(element);
            _placedModuleElements[element] = module;
        }

        private void PositionOverlayElement(VisualElement element, IModule module)
        {
            var screenPos = WorldToScreenPos(module.Transform.position);
            var size = EstimateScreenSizePxFromModule(module);

            element.style.left = screenPos.x - size.x / 2f;
            element.style.top = screenPos.y - size.y / 2f;
            element.style.width = size.x;
            element.style.height = size.y;
        }

        private Vector2 EstimateScreenSizePx(GameObject prefab)
        {
            var sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                return WorldSizeToScreenPx(sr.sprite.bounds.size);

            return WorldSizeToScreenPx(new Vector3(SnapUnits, SnapUnits, 0));
        }

        private Vector2 EstimateScreenSizePxFromModule(IModule module)
        {
            var sr = module.Transform.GetComponentInChildren<SpriteRenderer>();
            if (sr && sr.sprite)
                return WorldSizeToScreenPx(sr.sprite.bounds.size);

            return WorldSizeToScreenPx(new Vector3(SnapUnits, SnapUnits, 0));
        }

        private Vector2 WorldSizeToScreenPx(Vector3 worldSize)
        {
            if (!_cam) return new Vector2(SnapUnits * OverlayPixelsPerUnit, SnapUnits * OverlayPixelsPerUnit);

            var origin = _cam.WorldToScreenPoint(Vector3.zero);
            var offset = _cam.WorldToScreenPoint(worldSize);
            return new Vector2(Mathf.Abs(offset.x - origin.x), Mathf.Abs(offset.y - origin.y));
        }

        private void SelectModule(VisualElement element, IModule module)
        {
            _selectedElement?.RemoveFromClassList(SelectedModuleClass);
            _selectedElement = element;
            _selectedElement.AddToClassList(SelectedModuleClass);
            Debug.Log($"[ShipFactory] Selected module: {module.Transform.name}");
        }

        private void RebuildOverlaysFromShip()
        {
            _canvasArea.Clear();
            _placedModuleElements.Clear();
            _selectedElement = null;

            if (_ship == null) return;

            foreach (var module in _ship.GetComponentsInChildren<IModule>())
                AddOverlayElement(module);
        }
    }
}