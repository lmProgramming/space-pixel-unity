using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Ship;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using LMPro;
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
        private const int OverlaySortingOrder = 100;
        private const string RemoveButtonHiddenClassName = "remove-module-button--hidden";
        private const string ActionPopupWarningClassName = "action-popup--warning";
        private const string ActionPopupErrorClassName = "action-popup--error";

        private readonly VisualElement _actionPopup;
        private readonly Label _actionPopupLabel;
        private readonly Dictionary<ShipModuleSOInstanceBundle, ModuleOverlay> _bundleToOverlay = new();
        private readonly Camera _cam;
        private readonly VisualElement _inputBlocker;
        private readonly Label _moduleDescriptionLabel;
        private readonly Label _moduleNameLabel;
        private readonly Label _moduleSizeLabel;
        private readonly Label _moduleTypeLabel;
        private readonly Transform _overlayRoot;
        private readonly Button _removeModuleButton;
        private readonly Label _resourceCrewNeededLabel;
        private readonly Label _resourceCrewQuartersLabel;
        private readonly Label _resourceEnergyCapacityLabel;
        private readonly Label _resourceEnergyDrawLabel;
        private readonly Label _resourceEnergyProductionLabel;
        private readonly VisualElement _shipResourceCrewBufferFill;
        private readonly VisualElement _shipResourceCrewFill;
        private readonly Label _shipResourceCrewLabel;
        private readonly VisualElement _shipResourceEnergyBufferFill;
        private readonly VisualElement _shipResourceEnergyFill;
        private readonly Label _shipResourceEnergyLabel;
        private readonly VisualElement _shipResourcesPanel;

        private CancellationTokenSource _animationCts;
        private int _animationRunId;
        private ShipModuleSOInstanceBundle _draggedModuleBundle;
        private bool _draggedModuleWasNew;
        private ModuleOverlay _draggedOverlay;
        private Vector2 _dragStartWorldPos;
        private Vector2 _dragWorldOffset;
        private ShipModuleSO _hoveredPaletteModule;
        private ShipModuleSOInstanceBundle _hoveredPlacedBundle;
        private bool _isPointerOverCanvas;
        private ShipModuleSOInstanceBundle _selectedModuleBundle;
        private Ship _ship;

        public ShipFactoryCanvasController(VisualElement root)
        {
            _cam = Camera.main;

            var canvasContainer = root.Q<VisualElement>("canvas-container");
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
            _shipResourceEnergyBufferFill = root.Q<VisualElement>("ship-resource-energy-buffer-fill");
            _shipResourceCrewFill = root.Q<VisualElement>("ship-resource-crew-fill");
            _shipResourceCrewBufferFill = root.Q<VisualElement>("ship-resource-crew-buffer-fill");
            _actionPopup = root.Q<VisualElement>("action-popup");
            _actionPopupLabel = root.Q<Label>("action-popup-label");

            if (canvasContainer == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCanvasController] canvas-container not found in UXML!");

            if (_inputBlocker == null || _moduleNameLabel == null || _moduleTypeLabel == null ||
                _moduleSizeLabel == null ||
                _moduleDescriptionLabel == null || _resourceEnergyProductionLabel == null ||
                _resourceEnergyDrawLabel == null || _resourceEnergyCapacityLabel == null ||
                _resourceCrewNeededLabel == null || _resourceCrewQuartersLabel == null || _removeModuleButton == null ||
                _shipResourcesPanel == null || _shipResourceEnergyLabel == null || _shipResourceCrewLabel == null ||
                _shipResourceEnergyFill == null || _shipResourceEnergyBufferFill == null ||
                _shipResourceCrewFill == null || _shipResourceCrewBufferFill == null ||
                _actionPopup == null || _actionPopupLabel == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCanvasController] Required details panel elements are missing in UXML!");

            _overlayRoot = new GameObject("ModuleOverlays").transform;

            RegisterInputEvents(root, canvasContainer);
            _removeModuleButton.clicked += RemoveSelectedModule;

            SetInputLocked(false);
            RefreshInfoPanelFromCurrentContext();
            RefreshShipResourcesPanel();
        }

        private bool IsDraggingModule => _draggedModuleBundle != null;

        public bool IsInputLocked { get; private set; }

        public event Action OnModuleDragFinished;
        public event Action<bool> OnInputLockChanged;

        public void Dispose()
        {
            _animationRunId++;
            _animationCts?.Cancel();
            _animationCts = null;

            if (_overlayRoot != null)
                Object.Destroy(_overlayRoot.gameObject);
        }

        public void SetExternalInputLock(bool isLocked)
        {
            SetInputLocked(isLocked);
        }

        public void SetShip(Ship ship)
        {
            _ship = ship;
            RebuildOverlaysFromShip();
            RefreshShipResourcesPanel();
        }

        public void BeginModuleDrop(ShipModuleSO shipModuleSO)
        {
            if (IsInputLocked) return;

            if (_ship == null)
            {
                Debug.LogWarning("[ShipFactory] No ship assigned — cannot place module.");
                return;
            }

            _hoveredPaletteModule = null;

            var worldPos = Snapper.SnapToGrid(GameInput.WorldPointerPosition);
            var bundle = InstantiateModule(shipModuleSO, worldPos);
            if (bundle == null) return;

            var overlay = CreateOverlay(bundle);
            BeginModuleDrag(bundle, overlay, true);
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

            var netEnergy = rm.EnergyProduction - rm.EnergyDraw;
            var netEnergyFormatted = netEnergy >= 0 ? $"+{netEnergy:0.#}" : $"{netEnergy:0.#}";

            _shipResourceEnergyLabel.text =
                $"Energy capacity: {rm.EnergyCapacity:0.#}. Net energy: {netEnergyFormatted}";
            _shipResourceCrewLabel.text = $"Crew: {rm.Crew}/{rm.CrewCapacity}";

            ApplySegmentedResourceBar(
                _shipResourceEnergyFill,
                _shipResourceEnergyBufferFill,
                rm.EnergyDraw,
                rm.EnergyProduction,
                new Color(80f / 255f, 172f / 255f, 250f / 255f));

            ApplySegmentedResourceBar(
                _shipResourceCrewFill,
                _shipResourceCrewBufferFill,
                rm.Crew,
                rm.CrewCapacity,
                new Color(80f / 255f, 172f / 255f, 250f / 255f));
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

        #region Module Instantiation

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
            _ship.ManualAddModule(module);
            module.SetLocalPosition(localPosition);

            instance.GetComponent<Rigidbody2D>().simulated = false;

            return new ShipModuleSOInstanceBundle(instance, shipModuleSO, module);
        }

        #endregion

        #region Resource Bar

        private static void ApplySegmentedResourceBar(
            VisualElement usageFill,
            VisualElement bufferFill,
            float usage,
            float production,
            Color usageHealthyColor)
        {
            var isIdle = Mathf.Approximately(usage, 0f) && Mathf.Approximately(production, 0f);

            if (isIdle)
            {
                usageFill.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                usageFill.style.width = Length.Percent(0f);
                bufferFill.style.width = Length.Percent(0f);
                return;
            }

            var normalizationFactor = Mathf.Max(usage, production, 0.0001f);
            var coveredUsage = Mathf.Min(usage, production);
            var balanceDelta = production - usage;

            var coveredPercent = Mathf.Clamp01(coveredUsage / normalizationFactor);
            var deltaPercent = Mathf.Clamp01(Mathf.Abs(balanceDelta) / normalizationFactor);

            usageFill.style.left = Length.Percent(0f);
            usageFill.style.width = Length.Percent(coveredPercent * 100f);
            usageFill.style.backgroundColor = usageHealthyColor;

            var hasDeficit = balanceDelta < 0f;
            bufferFill.style.left = Length.Percent(coveredPercent * 100f);
            bufferFill.style.width = Length.Percent(deltaPercent * 100f);
            bufferFill.style.backgroundColor = hasDeficit
                ? Color.red
                : new Color(136f / 255f, 208f / 255f, 116f / 255f);
        }

        #endregion

        #region Validation

        private bool WouldRemovalCreateIslands(ShipModuleSOInstanceBundle bundleToRemove)
        {
            var remainingBundles = _bundleToOverlay.Keys.AsValueEnumerable()
                .Where(bundle => bundle != bundleToRemove).ToList();

            return remainingBundles.Count > 1 && remainingBundles
                .Select(bundle => Calculator.CalculateLegalityPosition(bundle, remainingBundles)).AsValueEnumerable()
                .Any(legality => legality != PositionLegality.Correct);
        }

        #endregion

        private enum PopupLevel
        {
            Info,
            Warning,
            Error
        }

        #region Input

        private void RegisterInputEvents(VisualElement root, VisualElement canvasContainer)
        {
            canvasContainer.RegisterCallback<PointerEnterEvent>(_ => _isPointerOverCanvas = true);
            canvasContainer.RegisterCallback<PointerLeaveEvent>(_ => OnCanvasPointerLeave());
            canvasContainer.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);

            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        private void OnCanvasPointerLeave()
        {
            _isPointerOverCanvas = false;
            if (IsDraggingModule || IsInputLocked) return;
            if (_hoveredPlacedBundle == null) return;

            var oldHovered = _hoveredPlacedBundle;
            _hoveredPlacedBundle = null;
            RefreshOverlayColor(oldHovered);
            RefreshInfoPanelFromCurrentContext();
        }

        private void OnCanvasPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || IsInputLocked || IsDraggingModule) return;

            var bundle = FindBundleAtWorldPosition(GameInput.WorldPointerPosition);
            if (bundle == null) return;

            if (!_bundleToOverlay.TryGetValue(bundle, out var overlay)) return;

            BeginModuleDrag(bundle, overlay, false);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (IsInputLocked) return;

            if (IsDraggingModule)
            {
                MoveGhostToPointer();
                return;
            }

            if (!_isPointerOverCanvas) return;
            UpdateHover();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!IsDraggingModule || IsInputLocked) return;
            HandleDragRelease();
        }

        private void UpdateHover()
        {
            var bundle = FindBundleAtWorldPosition(GameInput.WorldPointerPosition);

            if (bundle == _hoveredPlacedBundle) return;

            var oldHovered = _hoveredPlacedBundle;
            _hoveredPlacedBundle = bundle;
            _hoveredPaletteModule = null;

            if (oldHovered != null) RefreshOverlayColor(oldHovered);
            if (_hoveredPlacedBundle != null) RefreshOverlayColor(_hoveredPlacedBundle);

            RefreshInfoPanelFromCurrentContext();
        }

        #endregion

        #region Drag

        private void BeginModuleDrag(ShipModuleSOInstanceBundle bundle, ModuleOverlay overlay, bool isNewBundle)
        {
            _draggedModuleBundle = bundle;
            _draggedOverlay = overlay;
            _draggedModuleWasNew = isNewBundle;
            _dragStartWorldPos = bundle.Instance.transform.position;
            _hoveredPaletteModule = null;

            _dragWorldOffset = !isNewBundle
                ? (Vector2)bundle.Instance.transform.position - GameInput.WorldPointerPosition
                : Vector2.zero;

            SelectBundle(bundle);
            RefreshInfoPanelFromCurrentContext();
            MoveGhostToPointer();
        }

        private void MoveGhostToPointer()
        {
            var snapped = Snapper.SnapToGrid(GameInput.WorldPointerPosition + _dragWorldOffset);
            SetBundleAndOverlayPosition(_draggedModuleBundle, _draggedOverlay, snapped);

            var legality = Calculator.CalculateLegalityPosition(_draggedModuleBundle, _bundleToOverlay.Keys);
            var color = legality switch
            {
                PositionLegality.InsideOther => ModuleOverlay.InsideOtherColor,
                PositionLegality.OutsideShip or PositionLegality.DisconnectsShip => ModuleOverlay.OutsideShipColor,
                _ => ModuleOverlay.SelectedColor
            };
            _draggedOverlay.SetColor(color);
        }

        private void HandleDragRelease()
        {
            var legality = Calculator.CalculateLegalityPosition(_draggedModuleBundle, _bundleToOverlay.Keys);
            if (legality == PositionLegality.Correct)
            {
                FinishActiveDrag();
                return;
            }

            var activeBundle = _draggedModuleBundle;
            var activeOverlay = _draggedOverlay;
            var currentWorldPos = (Vector2)activeBundle.Instance.transform.position;

            SetInputLocked(true);

            if (_draggedModuleWasNew)
            {
                var bottomWorldTarget = CalculateOffScreenBottomPosition(currentWorldPos.x);

                AnimateBundleMovement(activeBundle, activeOverlay, currentWorldPos, bottomWorldTarget, () =>
                {
                    if (_ship != null)
                        _ship.ManualRemoveModule(activeBundle.PlacedModule);

                    _bundleToOverlay.Remove(activeBundle);
                    Object.Destroy(activeOverlay.gameObject);
                    Object.Destroy(activeBundle.Instance);

                    SelectBundle(null);
                    FinishActiveDrag();
                    SetInputLocked(false);
                });

                return;
            }

            AnimateBundleMovement(activeBundle, activeOverlay, currentWorldPos, _dragStartWorldPos, () =>
            {
                FinishActiveDrag();
                SetInputLocked(false);
            });
        }

        private void FinishActiveDrag()
        {
            var finishedBundle = _draggedModuleBundle;
            _draggedModuleBundle = null;
            _draggedOverlay = null;
            _draggedModuleWasNew = false;

            if (finishedBundle != null)
                RefreshOverlayColor(finishedBundle);

            RefreshInfoPanelFromCurrentContext();
            OnModuleDragFinished?.Invoke();
        }

        #endregion

        #region Overlay Management

        private ModuleOverlay CreateOverlay(ShipModuleSOInstanceBundle bundle)
        {
            var overlay = ModuleOverlay.Create(bundle, _overlayRoot, OverlaySortingOrder);
            _bundleToOverlay[bundle] = overlay;
            return overlay;
        }

        private void DestroyAllOverlays()
        {
            foreach (var overlay in _bundleToOverlay.Values)
                if (overlay != null)
                    Object.Destroy(overlay.gameObject);

            _bundleToOverlay.Clear();
        }

        private void RebuildOverlaysFromShip()
        {
            DestroyAllOverlays();
            _draggedOverlay = null;
            _draggedModuleBundle = null;
            _selectedModuleBundle = null;
            _hoveredPlacedBundle = null;

            if (_ship == null)
            {
                RefreshInfoPanelFromCurrentContext();
                return;
            }

            foreach (Transform child in _ship.gameObject.transform)
            {
                var container = child.GetComponent<ShipModuleSOContainer>();
                var module = child.GetComponent<IModule>();

                if (container == null || container.Module == null || module == null)
                    throw new InvalidOperationException(
                        "[ShipFactoryCanvasController] Ship child is missing module setup.");

                CreateOverlay(new ShipModuleSOInstanceBundle(child.gameObject, container.Module, module));
            }

            RefreshInfoPanelFromCurrentContext();
        }

        private void RefreshOverlayColor(ShipModuleSOInstanceBundle bundle)
        {
            if (!_bundleToOverlay.TryGetValue(bundle, out var overlay)) return;
            if (bundle == _draggedModuleBundle) return;

            if (bundle == _selectedModuleBundle)
                overlay.SetColor(ModuleOverlay.SelectedColor);
            else if (bundle == _hoveredPlacedBundle)
                overlay.SetColor(ModuleOverlay.HoverColor);
            else
                overlay.SetColor(ModuleOverlay.NormalColor);
        }

        #endregion

        #region Position & Hit Testing

        private static void SetBundleAndOverlayPosition(ShipModuleSOInstanceBundle bundle, ModuleOverlay overlay,
            Vector2 worldPos)
        {
            bundle.Instance.transform.position = worldPos;
            overlay.transform.position = worldPos;
        }

        [CanBeNull]
        private ShipModuleSOInstanceBundle FindBundleAtWorldPosition(Vector2 worldPos)
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

        private Vector2 CalculateOffScreenBottomPosition(float worldX)
        {
            if (_cam)
            {
                var viewportBottom = _cam.ViewportToWorldPoint(new Vector3(0.5f, -0.15f, _cam.nearClipPlane));
                return Snapper.SnapToGrid(new Vector2(worldX, viewportBottom.y));
            }

            return Snapper.SnapToGrid(new Vector2(worldX, -100f));
        }

        #endregion

        #region Animation

        private void AnimateBundleMovement(ShipModuleSOInstanceBundle bundle, ModuleOverlay overlay,
            Vector2 from, Vector2 to, Action onComplete)
        {
            _animationRunId++;
            _animationCts?.Cancel();

            var cts = new CancellationTokenSource();
            _animationCts = cts;
            var runId = _animationRunId;

            AnimateBundleMovementAsync(bundle, overlay, from, to, onComplete, cts, runId).Forget();
        }

        private async UniTask AnimateBundleMovementAsync(ShipModuleSOInstanceBundle bundle, ModuleOverlay overlay,
            Vector2 from, Vector2 to, Action onComplete, CancellationTokenSource cts, int runId)
        {
            try
            {
                var token = cts.Token;
                const float duration = 0.22f;
                var elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    var eased = 1f - Mathf.Pow(1f - t, 3f);
                    var world = Vector2.Lerp(from, to, eased);

                    SetBundleAndOverlayPosition(bundle, overlay, world);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                SetBundleAndOverlayPosition(bundle, overlay, to);
                onComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer animation or Dispose.
            }
            finally
            {
                if (_animationRunId == runId)
                    _animationCts = null;

                cts.Dispose();
            }
        }

        #endregion

        #region Selection & Removal

        private void SelectBundle(ShipModuleSOInstanceBundle bundle)
        {
            var oldSelected = _selectedModuleBundle;
            _selectedModuleBundle = bundle;

            if (oldSelected != null) RefreshOverlayColor(oldSelected);
            if (_selectedModuleBundle != null) RefreshOverlayColor(_selectedModuleBundle);

            RefreshInfoPanelFromCurrentContext();
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

            _ship.ManualRemoveModule(_selectedModuleBundle.PlacedModule);
            _bundleToOverlay.Remove(_selectedModuleBundle);
            Object.Destroy(overlay.gameObject);
            Object.Destroy(_selectedModuleBundle.Instance);

            if (_hoveredPlacedBundle == _selectedModuleBundle)
                _hoveredPlacedBundle = null;

            SelectBundle(null);
            RefreshShipResourcesPanel();
        }

        #endregion

        #region Info Panel

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
            _removeModuleButton.SetEnabled(!IsInputLocked && !IsDraggingModule);
        }

        #endregion

        #region Input Lock & Popups

        private void SetInputLocked(bool isLocked)
        {
            IsInputLocked = isLocked;
            _inputBlocker.style.display = isLocked ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshInfoPanelFromCurrentContext();
            OnInputLockChanged?.Invoke(isLocked);
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

        #endregion
    }
}