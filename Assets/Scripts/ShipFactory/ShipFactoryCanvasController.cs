using System;
using System.Collections.Generic;
using System.Linq;
using Core.Ship;
using JetBrains.Annotations;
using ShipFactory.LegalPositionCalculator;
using Ships;
using UnityEngine;
using UnityEngine.UIElements;
using ZLinq;
using Object = UnityEngine.Object;
using Resources = Core.Ship.Resources;

namespace ShipFactory
{
    public class ShipFactoryCanvasController
    {
        private const string InsideOtherOverlayClassName = "placed-module--inside-other";
        private const string OutsideShipOverlayClassName = "placed-module--outside-ship";
        private const string SelectedOverlayClassName = "placed-module--selected";
        private const string RemoveButtonHiddenClassName = "remove-module-button--hidden";
        private const string ActionPopupWarningClassName = "action-popup--warning";
        private const string ActionPopupErrorClassName = "action-popup--error";

        private static readonly Vector3 HackSoOverlayPlacedAppearsOk = new(0, Snapper.SnapUnits);

        private readonly VisualElement _actionPopup;
        private readonly Label _actionPopupLabel;
        private readonly Dictionary<ShipModuleSOInstanceBundle, VisualElement> _bundleToOverlay = new();

        private readonly Camera _cam;
        private readonly VisualElement _canvasArea;
        private readonly VisualElement _inputBlocker;
        private readonly Label _moduleDescriptionLabel;

        private readonly Label _moduleNameLabel;
        private readonly Label _moduleSizeLabel;
        private readonly Label _moduleTypeLabel;

        private readonly Dictionary<VisualElement, ShipModuleSOInstanceBundle> _placedModuleElements = new();
        private readonly Button _removeModuleButton;
        private readonly Label _resourceCrewNeededLabel;
        private readonly Label _resourceCrewQuartersLabel;
        private readonly Label _resourceEnergyCapacityLabel;
        private readonly Label _resourceEnergyDrawLabel;
        private readonly Label _resourceEnergyProductionLabel;
        private readonly VisualElement _shipResourceCrewFill;
        private readonly Label _shipResourceCrewLabel;
        private readonly VisualElement _shipResourceEnergyFill;
        private readonly Label _shipResourceEnergyLabel;

        private readonly VisualElement _shipResourcesPanel;

        private ShipModuleSOInstanceBundle _draggedModuleBundle;
        private bool _draggedModuleWasNew;

        private VisualElement _dragGhost;
        private Vector2 _dragStartWorldPos;
        private Vector2 _ghostSizePx;
        private ShipModuleSO _hoveredPaletteModule;
        private ShipModuleSOInstanceBundle _hoveredPlacedBundle;
        private ShipModuleSOInstanceBundle _selectedModuleBundle;

        private Ship _ship;

        public ShipFactoryCanvasController(VisualElement root)
        {
            _cam = Camera.main;

            var canvasViewport = root.Q<VisualElement>("canvas-viewport");
            _canvasArea = root.Q<VisualElement>("canvas-area");
            _inputBlocker = root.Q<VisualElement>("ship-factory-input-blocker");

            _moduleNameLabel = root.Q<Label>("module-info-name");
            _moduleTypeLabel = root.Q<Label>("module-info-type");
            _moduleSizeLabel = root.Q<Label>("module-info-size");
            _moduleDescriptionLabel = root.Q<Label>("module-info-description");
            _resourceEnergyProductionLabel = root.Q<Label>("module-info-resource-energy-production");
            _resourceEnergyDrawLabel = root.Q<Label>("module-info-resource-energy-draw");
            _resourceEnergyCapacityLabel = root.Q<Label>("module-info-resource-energy-capacity");
            _resourceCrewNeededLabel = root.Q<Label>("module-info-resource-crew-needed");
            _resourceCrewQuartersLabel = root.Q<Label>("module-info-resource-crew-quarters");
            _removeModuleButton = root.Q<Button>("remove-module-button");
            _shipResourcesPanel = root.Q<VisualElement>("ship-resources-panel");
            _shipResourceEnergyLabel = root.Q<Label>("ship-resource-energy-label");
            _shipResourceCrewLabel = root.Q<Label>("ship-resource-crew-label");
            _shipResourceEnergyFill = root.Q<VisualElement>("ship-resource-energy-fill");
            _shipResourceCrewFill = root.Q<VisualElement>("ship-resource-crew-fill");
            _actionPopup = root.Q<VisualElement>("action-popup");
            _actionPopupLabel = root.Q<Label>("action-popup-label");

            if (canvasViewport == null || _canvasArea == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCanvasController] Required canvas elements not found in UXML!");

            if (_inputBlocker == null || _moduleNameLabel == null || _moduleTypeLabel == null ||
                _moduleSizeLabel == null ||
                _moduleDescriptionLabel == null || _resourceEnergyProductionLabel == null ||
                _resourceEnergyDrawLabel == null || _resourceEnergyCapacityLabel == null ||
                _resourceCrewNeededLabel == null || _resourceCrewQuartersLabel == null || _removeModuleButton == null ||
                _shipResourcesPanel == null || _shipResourceEnergyLabel == null || _shipResourceCrewLabel == null ||
                _shipResourceEnergyFill == null || _shipResourceCrewFill == null ||
                _actionPopup == null || _actionPopupLabel == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCanvasController] Required details panel elements are missing in UXML!");

            _canvasArea.pickingMode = PickingMode.Ignore;
            canvasViewport.pickingMode = PickingMode.Ignore;

            _removeModuleButton.clicked += RemoveSelectedModule;

            SetInputLocked(false);
            RefreshInfoPanelFromCurrentContext();
            RefreshShipResourcesPanel();
            RegisterDragEvents(root);
        }

        private bool IsDraggingModule => _draggedModuleBundle != null;

        public bool IsInputLocked { get; private set; }

        public event Action OnModuleDragFinished;
        public event Action<bool> OnInputLockChanged;

        public void SetShip(Ship ship)
        {
            _ship = ship;
            RebuildOverlaysFromShip();
            RefreshShipResourcesPanel();
        }

        public void BeginModuleDrop(ShipModuleSO shipModuleSO, Vector2 pointerScreenPos)
        {
            if (IsInputLocked) return;

            if (_ship == null)
            {
                Debug.LogWarning("[ShipFactory] No ship assigned — cannot place module.");
                return;
            }

            _hoveredPaletteModule = null;

            var worldPos = ScreenToSnappedWorldPosition(pointerScreenPos);
            var bundle = InstantiateModule(shipModuleSO, worldPos);
            if (bundle == null) return;

            var dragGhost = AddOverlayElement(bundle);
            BeginModuleDrop(bundle, pointerScreenPos, dragGhost, true);
        }

        public void ShowPaletteModuleInfo(ShipModuleSO moduleSO)
        {
            if (moduleSO == null)
                throw new ArgumentNullException(nameof(moduleSO));

            if (IsDraggingModule || IsInputLocked) return;

            _hoveredPaletteModule = moduleSO;
            RefreshInfoPanelFromCurrentContext();
        }

        public void HidePaletteModuleInfo(ShipModuleSO moduleSO)
        {
            if (moduleSO == null || _hoveredPaletteModule != moduleSO) return;
            if (IsDraggingModule) return;

            _hoveredPaletteModule = null;
            RefreshInfoPanelFromCurrentContext();
        }

        public void RefreshShipResourcesPanel()
        {
            if (!_ship || !_ship.ResourceManager)
            {
                _shipResourcesPanel.style.display = DisplayStyle.None;
                return;
            }

            _shipResourcesPanel.style.display = DisplayStyle.Flex;

            var rm = _ship.ResourceManager;
            var energyPercent = rm.EnergyCapacity > 0f ? Mathf.Clamp01(rm.Energy / rm.EnergyCapacity) : 0f;
            var crewPercent = rm.CrewCapacity > 0 ? Mathf.Clamp01((float)rm.Crew / rm.CrewCapacity) : 0f;

            _shipResourceEnergyLabel.text =
                $"Energy: {rm.Energy:0.#}/{rm.EnergyCapacity:0.#}  (+{rm.EnergyProduction:0.#} / -{rm.EnergyDraw:0.#})";
            _shipResourceCrewLabel.text = $"Crew: {rm.Crew}/{rm.CrewCapacity}";

            _shipResourceEnergyFill.style.width = Length.Percent(energyPercent * 100f);
            _shipResourceCrewFill.style.width = Length.Percent(crewPercent * 100f);
        }

        public void ShowInfoMessage(string message)
        {
            ShowActionPopup(message);
        }

        public void ShowWarningMessage(string message)
        {
            ShowActionPopup(message, PopupLevel.Warning);
        }

        public void ShowErrorMessage(string message)
        {
            ShowActionPopup(message, PopupLevel.Error);
        }

        private void RemoveSelectedModule()
        {
            if (_selectedModuleBundle == null || _ship == null || IsDraggingModule || IsInputLocked) return;
            if (_selectedModuleBundle.PlacedModule.Type == ModuleType.Command)
            {
                const string message = "Command module cannot be removed.";
                ShowActionPopup(message, PopupLevel.Warning);
                Debug.LogWarning($"[ShipFactory] {message}");
                return;
            }

            if (WouldRemovalCreateIslands(_selectedModuleBundle))
            {
                const string message = "Cannot remove this module: it would split the ship into islands.";
                ShowActionPopup(message, PopupLevel.Error);
                Debug.LogWarning($"[ShipFactory] {message}");
                return;
            }

            if (!_bundleToOverlay.TryGetValue(_selectedModuleBundle, out var overlay)) return;

            _ship.RemoveModule(_selectedModuleBundle.PlacedModule);
            _placedModuleElements.Remove(overlay);
            _bundleToOverlay.Remove(_selectedModuleBundle);
            overlay.RemoveFromHierarchy();

            Object.Destroy(_selectedModuleBundle.Instance);

            if (_hoveredPlacedBundle == _selectedModuleBundle)
                _hoveredPlacedBundle = null;

            SelectBundle(null);
            UpdateCanvasPickingMode();
            RefreshShipResourcesPanel();
        }

        private void BeginModuleDrop(ShipModuleSOInstanceBundle bundle, Vector2 pointerScreenPos,
            VisualElement dragGhost, bool isNewBundle)
        {
            _draggedModuleBundle = bundle;
            _dragGhost = dragGhost;
            _draggedModuleWasNew = isNewBundle;
            _dragStartWorldPos = bundle.Instance.transform.position;
            _hoveredPaletteModule = null;

            _ghostSizePx = WorldSizeToScreenPx(bundle.ModuleSO.Dimensions);
            _dragGhost.style.width = _ghostSizePx.x;
            _dragGhost.style.height = _ghostSizePx.y;

            SelectBundle(bundle);
            RefreshInfoPanelFromCurrentContext();
            MoveGhostToPointer(pointerScreenPos);
        }

        private void RegisterDragEvents(VisualElement root)
        {
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!IsDraggingModule || IsInputLocked) return;
            MoveGhostToPointer(evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!IsDraggingModule || IsInputLocked) return;

            var legality = Calculator.CalculateLegalityPosition(_draggedModuleBundle, _placedModuleElements.Values);
            if (legality == PositionLegality.Correct)
            {
                FinishActiveDrag();
                return;
            }

            var activeBundle = _draggedModuleBundle;
            var activeGhost = _dragGhost;
            var currentWorldPos = (Vector2)activeBundle.Instance.transform.position;

            SetInputLocked(true);

            if (_draggedModuleWasNew)
            {
                var currentScreen = WorldToScreenPos(currentWorldPos + (Vector2)HackSoOverlayPlacedAppearsOk);
                var bottomScreenTarget = new Vector2(currentScreen.x, Screen.height - 16f);
                var bottomWorldTarget = ScreenToSnappedWorldPosition(bottomScreenTarget);

                AnimateBundleMovement(activeBundle, activeGhost, currentWorldPos, bottomWorldTarget, () =>
                {
                    if (_ship != null)
                        _ship.RemoveModule(activeBundle.PlacedModule);

                    _placedModuleElements.Remove(activeGhost);
                    _bundleToOverlay.Remove(activeBundle);
                    activeGhost.RemoveFromHierarchy();
                    Object.Destroy(activeBundle.Instance);

                    SelectBundle(null);
                    FinishActiveDrag();
                    SetInputLocked(false);
                });

                return;
            }

            AnimateBundleMovement(activeBundle, activeGhost, currentWorldPos, _dragStartWorldPos, () =>
            {
                FinishActiveDrag();
                SetInputLocked(false);
            });
        }

        private void FinishActiveDrag()
        {
            _draggedModuleBundle = null;
            _dragGhost = null;
            _draggedModuleWasNew = false;

            RefreshInfoPanelFromCurrentContext();
            OnModuleDragFinished?.Invoke();
        }

        private void MoveGhostToPointer(Vector2 screenPos)
        {
            var snapped = ScreenToSnappedWorldPosition(screenPos);
            SetBundleWorldAndOverlayPosition(_draggedModuleBundle, _dragGhost, snapped);

            var legality = Calculator.CalculateLegalityPosition(_draggedModuleBundle, _placedModuleElements.Values);
            _dragGhost.EnableInClassList(InsideOtherOverlayClassName, legality == PositionLegality.InsideOther);
            _dragGhost.EnableInClassList(OutsideShipOverlayClassName,
                legality is PositionLegality.OutsideShip or PositionLegality.DisconnectsShip);
        }

        private void AnimateBundleMovement(ShipModuleSOInstanceBundle bundle, VisualElement overlay, Vector2 from,
            Vector2 to, Action onComplete)
        {
            const float duration = 0.22f;
            var startedAt = Time.unscaledTime;
            var finished = false;

            _canvasArea.schedule.Execute(() =>
            {
                if (finished) return;

                var t = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                var world = Vector2.Lerp(from, to, eased);

                SetBundleWorldAndOverlayPosition(bundle, overlay, world);

                if (t < 1f) return;

                finished = true;
                overlay.RemoveFromClassList(InsideOtherOverlayClassName);
                overlay.RemoveFromClassList(OutsideShipOverlayClassName);
                onComplete?.Invoke();
            }).Every(16);
        }

        private void SetBundleWorldAndOverlayPosition(ShipModuleSOInstanceBundle bundle, VisualElement overlay,
            Vector2 snappedWorldPos)
        {
            bundle.Instance.transform.position = snappedWorldPos;

            var overlayScreenPos = WorldToScreenPos(snappedWorldPos + (Vector2)HackSoOverlayPlacedAppearsOk);
            overlay.style.left = overlayScreenPos.x - _ghostSizePx.x / 2f;
            overlay.style.top = overlayScreenPos.y - _ghostSizePx.y / 2f;
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

            element.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (IsDraggingModule || IsInputLocked) return;
                _hoveredPlacedBundle = moduleBundle;
                _hoveredPaletteModule = null;
                RefreshInfoPanelFromCurrentContext();
            });

            element.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (_hoveredPlacedBundle != moduleBundle) return;
                _hoveredPlacedBundle = null;
                RefreshInfoPanelFromCurrentContext();
            });

            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || IsInputLocked) return;
                BeginModuleDrop(moduleBundle, evt.position, element, false);
                evt.StopPropagation();
            });

            PositionInitialOverlayElement(element, moduleBundle);
            _canvasArea.Add(element);

            _placedModuleElements[element] = moduleBundle;
            _bundleToOverlay[moduleBundle] = element;
            UpdateCanvasPickingMode();

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
            _bundleToOverlay.Clear();
            _dragGhost = null;
            _draggedModuleBundle = null;
            _selectedModuleBundle = null;
            _hoveredPlacedBundle = null;

            if (_ship == null)
            {
                RefreshInfoPanelFromCurrentContext();
                UpdateCanvasPickingMode();
                return;
            }

            foreach (Transform transform in _ship.gameObject.transform)
            {
                var shipModuleSO = transform.GetComponent<ShipModuleSOContainer>();
                var module = transform.GetComponent<IModule>();

                if (shipModuleSO == null || shipModuleSO.Module == null || module == null)
                    throw new InvalidOperationException(
                        "[ShipFactoryCanvasController] Ship child is missing module setup.");

                AddOverlayElement(new ShipModuleSOInstanceBundle(transform.gameObject, shipModuleSO.Module, module));
            }

            RefreshInfoPanelFromCurrentContext();
            UpdateCanvasPickingMode();
        }

        private void SelectBundle(ShipModuleSOInstanceBundle bundle)
        {
            if (_selectedModuleBundle != null &&
                _bundleToOverlay.TryGetValue(_selectedModuleBundle, out var oldOverlay))
                oldOverlay.RemoveFromClassList(SelectedOverlayClassName);

            _selectedModuleBundle = bundle;

            if (_selectedModuleBundle != null &&
                _bundleToOverlay.TryGetValue(_selectedModuleBundle, out var newOverlay))
                newOverlay.AddToClassList(SelectedOverlayClassName);

            RefreshInfoPanelFromCurrentContext();
        }

        private void RefreshInfoPanelFromCurrentContext()
        {
            var context = ResolveContext();

            if (context.Bundle != null)
                ApplyBundleInfo(context.Bundle, context.IsNewModuleContext);
            else if (context.PaletteModuleSO != null)
                ApplyPaletteInfo(context.PaletteModuleSO);
            else
                ApplyEmptyInfo();
        }

        private (ShipModuleSOInstanceBundle Bundle, ShipModuleSO PaletteModuleSO, bool IsNewModuleContext)
            ResolveContext()
        {
            if (IsDraggingModule)
                return (_draggedModuleBundle, null, _draggedModuleWasNew);

            if (_hoveredPlacedBundle != null)
                return (_hoveredPlacedBundle, null, false);

            if (_hoveredPaletteModule != null)
                return (null, _hoveredPaletteModule, true);

            if (_selectedModuleBundle != null)
                return (_selectedModuleBundle, null, false);

            return (null, null, false);
        }

        private void ApplyBundleInfo(ShipModuleSOInstanceBundle bundle, bool isNewModuleContext)
        {
            _moduleNameLabel.text = bundle.ModuleSO.Name;
            _moduleTypeLabel.text = $"Type: {bundle.PlacedModule.Type}";
            _moduleSizeLabel.text = $"Dimensions: {bundle.ModuleSO.Dimensions.x}x{bundle.ModuleSO.Dimensions.y}";
            _moduleDescriptionLabel.text = string.IsNullOrWhiteSpace(bundle.ModuleSO.Description)
                ? "No description."
                : bundle.ModuleSO.Description;

            ApplyResources(bundle.PlacedModule.Resources);
            UpdateRemoveButton(isNewModuleContext);
        }

        private void ApplyPaletteInfo(ShipModuleSO moduleSO)
        {
            var module = moduleSO.Prefab.GetComponent<IModule>();
            if (module == null)
                throw new InvalidOperationException(
                    $"[ShipFactoryCanvasController] Prefab '{moduleSO.Prefab.name}' is missing IModule component.");

            _moduleNameLabel.text = moduleSO.Name;
            _moduleTypeLabel.text = $"Type: {module.Type}";
            _moduleSizeLabel.text = $"Dimensions: {moduleSO.Dimensions.x}x{moduleSO.Dimensions.y}";
            _moduleDescriptionLabel.text = string.IsNullOrWhiteSpace(moduleSO.Description)
                ? "No description."
                : moduleSO.Description;

            ApplyResources(module.Resources);
            UpdateRemoveButton(true);
        }

        private void ApplyResources(Resources resources)
        {
            _resourceEnergyProductionLabel.text = $"Energy Production: {resources.energyProduction:0.##}";
            _resourceEnergyDrawLabel.text = $"Energy Draw: {resources.energyDraw:0.##}";
            _resourceEnergyCapacityLabel.text = $"Energy Capacity: {resources.energyCapacity:0.##}";
            _resourceCrewNeededLabel.text = $"Crew Needed: {resources.crewNeeded}";
            _resourceCrewQuartersLabel.text = $"Crew Quarters: {resources.crewQuarters}";
        }

        private void ApplyEmptyInfo()
        {
            _moduleNameLabel.text = "No module selected";
            _moduleTypeLabel.text = "Type: -";
            _moduleSizeLabel.text = "Dimensions: -";
            _moduleDescriptionLabel.text = "Hover or drag a module to inspect it.";

            _resourceEnergyProductionLabel.text = "Energy Production: -";
            _resourceEnergyDrawLabel.text = "Energy Draw: -";
            _resourceEnergyCapacityLabel.text = "Energy Capacity: -";
            _resourceCrewNeededLabel.text = "Crew Needed: -";
            _resourceCrewQuartersLabel.text = "Crew Quarters: -";

            _removeModuleButton.SetEnabled(false);
            _removeModuleButton.AddToClassList(RemoveButtonHiddenClassName);
        }

        private void UpdateRemoveButton(bool isNewModuleContext)
        {
            if (isNewModuleContext)
            {
                _removeModuleButton.SetEnabled(false);
                _removeModuleButton.AddToClassList(RemoveButtonHiddenClassName);
                return;
            }

            _removeModuleButton.RemoveFromClassList(RemoveButtonHiddenClassName);

            // Keep button clickable for placed command modules so we can show explicit feedback popup.
            _removeModuleButton.SetEnabled(!IsInputLocked && !IsDraggingModule);
        }

        private void SetInputLocked(bool isLocked)
        {
            IsInputLocked = isLocked;
            _inputBlocker.style.display = isLocked ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshInfoPanelFromCurrentContext();
            OnInputLockChanged?.Invoke(isLocked);
        }

        private void UpdateCanvasPickingMode()
        {
            _canvasArea.pickingMode = _placedModuleElements.Count > 0 ? PickingMode.Position : PickingMode.Ignore;
        }

        private void ShowActionPopup(string message, PopupLevel level = PopupLevel.Info)
        {
            _actionPopup.RemoveFromClassList(ActionPopupWarningClassName);
            _actionPopup.RemoveFromClassList(ActionPopupErrorClassName);

            switch (level)
            {
                case PopupLevel.Warning:
                    _actionPopup.AddToClassList(ActionPopupWarningClassName);
                    break;
                case PopupLevel.Error:
                    _actionPopup.AddToClassList(ActionPopupErrorClassName);
                    break;
                case PopupLevel.Info:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }

            _actionPopupLabel.text = message;
            _actionPopup.style.display = DisplayStyle.Flex;

            _actionPopup.schedule.Execute(() => { _actionPopup.style.display = DisplayStyle.None; }).StartingIn(1600);
        }

        private bool WouldRemovalCreateIslands(ShipModuleSOInstanceBundle bundleToRemove)
        {
            var remainingBundles = _placedModuleElements.Values.AsValueEnumerable()
                .Where(bundle => bundle != bundleToRemove).ToList();

            return remainingBundles.Count > 1 && remainingBundles
                .Select(bundle => Calculator.CalculateLegalityPosition(bundle, remainingBundles)).AsValueEnumerable()
                .Any(legality => legality != PositionLegality.Correct);
        }

        private enum PopupLevel
        {
            Info,
            Warning,
            Error
        }
    }
}