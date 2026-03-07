using System;
using System.Collections.Generic;
using Core.Ship;
using JetBrains.Annotations;
using ShipFactory.LegalPositionCalculator;
using Ships;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ShipFactory
{
    public class ShipFactoryCanvasController
    {
        private const int OverlayPixelsPerUnit = 16;

        private const string InsideOtherOverlayClassName = "placed-module--inside-other";
        private const string OutsideShipOverlayClassName = "placed-module--outside-ship";

        // How many screen pixels equal one world unit at the default camera zoom.
        // Overlay elements are repositioned every frame via UpdateOverlays().
        private static readonly Vector3 HackSoOverlayPlacedAppearsOk = new(0, Snapper.SnapUnits);
        private readonly Camera _cam;

        private readonly VisualElement _canvasArea;

        private readonly Dictionary<VisualElement, ShipModuleSOInstanceBundle> _placedModuleElements = new();
        private ShipModuleSOInstanceBundle _draggedModuleBundle;
        private VisualElement _dragGhost;
        private Vector2 _ghostSizePx;
        private Ship _ship;

        public ShipFactoryCanvasController(VisualElement root)
        {
            _cam = Camera.main;

            var canvasViewport = root.Q<VisualElement>("canvas-viewport");
            _canvasArea = root.Q<VisualElement>("canvas-area");

            if (canvasViewport == null || _canvasArea == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCanvasController] Required canvas elements not found in UXML!");

            // Canvas area is transparent — clicks pass through to game unless we're dragging
            _canvasArea.pickingMode = PickingMode.Ignore;
            canvasViewport.pickingMode = PickingMode.Ignore;

            RegisterDragEvents(root);
        }

        private bool IsDraggingModule => _draggedModuleBundle != null;

        public event Action OnModuleDragFinished;

        public void SetShip(Ship ship)
        {
            _ship = ship;
            RebuildOverlaysFromShip();
        }

        public void BeginModuleDrop(ShipModuleSO shipModuleSO, Vector2 pointerScreenPos)
        {
            var worldPos = ScreenToSnappedWorldPosition(pointerScreenPos);
            var bundle = InstantiateModule(shipModuleSO, worldPos);

            var dragGhost = AddOverlayElement(bundle);

            BeginModuleDrop(bundle, pointerScreenPos, dragGhost);
        }

        private void BeginModuleDrop(ShipModuleSOInstanceBundle bundle, Vector2 pointerScreenPos,
            VisualElement dragGhost)
        {
            _draggedModuleBundle = bundle;

            _dragGhost = dragGhost;

            _ghostSizePx = WorldSizeToScreenPx(bundle.ModuleSO.Dimensions);

            _dragGhost.style.width = _ghostSizePx.x;
            _dragGhost.style.height = _ghostSizePx.y;

            MoveGhostToPointer(pointerScreenPos);
        }

        private void RegisterDragEvents(VisualElement root)
        {
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!IsDraggingModule) return;
            MoveGhostToPointer(evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!IsDraggingModule) return;

            if (_ship == null)
            {
                Debug.LogWarning("[ShipFactory] No ship assigned — cannot place module.");
                _draggedModuleBundle = null;
                return;
            }

            _draggedModuleBundle = null;

            OnModuleDragFinished?.Invoke();
        }

        private void MoveGhostToPointer(Vector2 screenPos)
        {
            var snapped = ScreenToSnappedWorldPosition(screenPos);
            var snappedOverlayPos = ScreenToSnappedWorldPosition(screenPos) + (Vector2)HackSoOverlayPlacedAppearsOk;
            var snappedScreen = WorldToScreenPos(snappedOverlayPos);

            var legality = Calculator.CalculateLegalityPosition(_draggedModuleBundle, _placedModuleElements.Values);

            _dragGhost.EnableInClassList(InsideOtherOverlayClassName, legality == PositionLegality.InsideOther);
            _dragGhost.EnableInClassList(OutsideShipOverlayClassName, legality == PositionLegality.OutsideShip);

            _dragGhost.style.left = snappedScreen.x - _ghostSizePx.x / 2f;
            _dragGhost.style.top = snappedScreen.y - _ghostSizePx.y / 2f;
            _draggedModuleBundle.Instance.transform.position = snapped;
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
            return Snapper.SnapToGrid(worldPos);
        }

        private Vector2 WorldToScreenPos(Vector2 worldPos)
        {
            if (!_cam) return Vector2.zero;

            var screenPos = _cam.WorldToScreenPoint(worldPos);
            // Convert Unity screen (y bottom-up) to UI Toolkit panel (y top-down)
            return new Vector2(screenPos.x, Screen.height - screenPos.y);
        }

        [CanBeNull]
        private ShipModuleSOInstanceBundle InstantiateModule(ShipModuleSO shipModuleSO, Vector2 worldPosition)
        {
            var instance = (GameObject)Object.Instantiate((Object)shipModuleSO.Prefab, _ship.transform);
            var module = instance.GetComponent<IModule>();

            if (module == null)
            {
                Debug.LogError($"[ShipFactory] Prefab '{shipModuleSO.name}' has no IModule component!", shipModuleSO);
                Object.Destroy(instance);
                return null;
            }

            var localPosition = (Vector2)_ship.transform.InverseTransformPoint(worldPosition);
            _ship.AddModule(module);
            module.SetLocalPosition(localPosition);

            instance.GetComponent<Rigidbody2D>().simulated = false;

            return new ShipModuleSOInstanceBundle(instance, shipModuleSO, module);
        }

        private VisualElement AddOverlayElement(ShipModuleSOInstanceBundle moduleBundle)
        {
            var element = new VisualElement();
            element.AddToClassList("placed-module");

            var label = new Label(moduleBundle.ModuleSO.Name);
            label.AddToClassList("placed-module-label");
            element.Add(label);

            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                BeginModuleDrop(moduleBundle, evt.position, element);
                evt.StopPropagation();
            });

            PositionInitialOverlayElement(element, moduleBundle);
            _canvasArea.pickingMode = PickingMode.Position;
            _canvasArea.Add(element);
            _placedModuleElements[element] = moduleBundle;

            return element;
        }

        private void PositionInitialOverlayElement(VisualElement element, ShipModuleSOInstanceBundle module)
        {
            var screenPos = WorldToScreenPos(module.Instance.transform.position + HackSoOverlayPlacedAppearsOk);

            var size = WorldSizeToScreenPx(module.ModuleSO.Dimensions);

            element.style.left = screenPos.x - size.x / 2f;
            element.style.top = screenPos.y - size.y / 2f;
            element.style.width = size.x;
            element.style.height = size.y;
        }

        private Vector2 WorldSizeToScreenPx(Vector2 worldSize)
        {
            if (!_cam) throw new InvalidOperationException("Missing camera");

            var origin = _cam.WorldToScreenPoint(Vector3.zero);
            var offset = _cam.WorldToScreenPoint(worldSize);
            return new Vector2(Mathf.Abs(offset.x - origin.x), Mathf.Abs(offset.y - origin.y));
        }

        private void RebuildOverlaysFromShip()
        {
            _canvasArea.Clear();
            _placedModuleElements.Clear();
            _dragGhost = null;

            if (_ship == null) return;

            foreach (Transform transform in _ship.gameObject.transform)
            {
                var shipModuleSO = transform.GetComponent<ShipModuleSOContainer>();
                var module = transform.GetComponent<IModule>();

                AddOverlayElement(new ShipModuleSOInstanceBundle(transform.gameObject, shipModuleSO.Module, module));
            }
        }
    }
}