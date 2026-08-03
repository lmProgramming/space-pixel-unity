using System;
using System.Linq;
using Core.Services;
using Core.ShipFactory;
using Core.Ships;
using Core.Ships.Module;
using Core.UI;
using JetBrains.Annotations;
using ShipFactory.Helpers;
using ShipFactory.Helpers.LegalPositionCalculator;
using ShipFactory.Models;
using ShipFactory.UI.Runtime;
using ShipFactory.UI.ToolkitComponents;
using Ships;
using UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;
using ZLinq;
using Object = UnityEngine.Object;

namespace ShipFactory.UI
{
    public class ShipFactoryCanvasController : IDisposable
    {
        private readonly DragAnimator _animator;
        private readonly CameraInfoPanel _cameraInfoPanel;
        private readonly ShipFactoryFeedback _feedback;
        private readonly IGameInput _gameInput;
        private readonly IGameUi _gameUi;
        private readonly ModuleInfoPanel _infoPanel;

        private readonly VisualElement _inputBlocker;
        private readonly IInstantiator _instantiator;

        private readonly ResourcesPanel _resourcesPanel;
        private Quaternion _dragStartLocalRotation;
        private float _dragStartLocalZ;
        private Vector2 _dragStartWorldPos;
        private Vector2 _dragWorldOffset;

        private ShipModuleSOInstanceBundle _draggedModuleBundle;
        private bool _draggedModuleWasNew;
        private bool _ghostSelectionEnabled;

        private ShipModuleSO _hoveredPaletteModule;
        private ShipModuleSOInstanceBundle _hoveredPlacedBundle;

        private bool _isPointerOverCanvas;

        public ShipFactoryCanvasController(
            VisualElement root,
            ShipFactoryFeedback feedback,
            IGameUi gameUi,
            IGameInput gameInput,
            IInstantiator instantiator,
            IShipModuleCatalog moduleCatalog,
            CameraInfoPanel.Factory cameraInfoPanelFactory)
        {
            _gameInput = gameInput;
            _gameUi = gameUi ?? throw new ArgumentNullException(nameof(gameUi));
            _instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
            if (moduleCatalog == null) throw new ArgumentNullException(nameof(moduleCatalog));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));

            if (cameraInfoPanelFactory == null)
                throw new ArgumentNullException(nameof(cameraInfoPanelFactory));

            var canvasContainer = root.Q<VisualElement>("canvas-container");
            _inputBlocker = root.Q<VisualElement>("ship-factory-input-blocker");

            if (canvasContainer == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCanvasController] canvas-container not found in UXML!");

            // 1. Initialize Sub-Panels

            _resourcesPanel = root.Q("resources-panel")?.Q<ResourcesPanel>();
            if (_resourcesPanel == null)
                throw new InvalidOperationException(
                    "[ShipFactoryCanvasController] ResourcesPanel 'resources-panel' not found in UXML!");

            _infoPanel = new ModuleInfoPanel(root);
            _cameraInfoPanel = cameraInfoPanelFactory.Create(root);

            _infoPanel.OnRemoveModuleClicked += RemoveSelectedModule;
            _infoPanel.OnRotateClockwiseClicked += () => RotateActiveModule(90);
            _infoPanel.OnRotateCounterClockwiseClicked += () => RotateActiveModule(-90);

            // 2. Initialize Managers
            OverlayManager = new OverlayManager(moduleCatalog);
            _animator = new DragAnimator(OverlayManager);

            // 3. Register Inputs
            RegisterInputEvents(root, canvasContainer);

            SetInputLocked(false);
            RefreshInfoPanelFromCurrentContext();
            _resourcesPanel.Refresh(Ship);
        }

        public bool IsInputLocked { get; private set; }
        public bool IsDraggingModule => _draggedModuleBundle != null;
        public bool ShipHasModules => OverlayManager.AllBundles.AsValueEnumerable().Any();

        public bool ShipHasCommandModule => OverlayManager.AllBundles.AsValueEnumerable()
            .Any(bundle => bundle.PlacedModule.Type == ModuleType.Command);

        public Func<ShipModuleSO, bool> CanAffordModulePlacement { get; set; }
        public Action<ShipModuleSO> OnModulePurchased { get; set; }

        public ShipModuleSOInstanceBundle SelectedModuleBundle { get; private set; }

        public ModuleGhost SelectedGhost { get; private set; }

        public DesignShip Ship { get; private set; }

        public OverlayManager OverlayManager { get; }

        public void Dispose()
        {
            _animator.Dispose();
            OverlayManager.Dispose();
        }

        public event Action OnModuleDragFinished;
        public event Action OnShipCompositionChanged;
        public event Action OnClearShipRequested;
        public event Action<bool> OnInputLockChanged;
        public event Action<ShipModuleSOInstanceBundle> ModuleSelected;
        public event Action<ModuleGhost> GhostSelected;
        public event Action SelectionCleared;

        public void SetGhostSelectionEnabled(bool enabled)
        {
            _ghostSelectionEnabled = enabled;
            if (!enabled)
                ClearGhostSelection();
        }

        public void SetExternalInputLock(bool isLocked)
        {
            SetInputLocked(isLocked);
        }

        public void ShowInfoMessage(string message)
        {
            _gameUi.Notify(message);
        }

        public void ShowWarningMessage(string message)
        {
            _gameUi.Notify(message, PopupLevel.Warning);
        }

        public void ShowErrorMessage(string message)
        {
            _gameUi.Notify(message, PopupLevel.Error);
        }

        public void SetShip(DesignShip ship)
        {
            Ship = ship;
            _draggedModuleBundle = null;
            SelectedModuleBundle = null;
            _hoveredPlacedBundle = null;

            RebuildShipModules();
        }

        public void RebuildShipModules()
        {
            OverlayManager.RebuildFromShip(Ship);
            _resourcesPanel.Refresh(Ship);
            RefreshInfoPanelFromCurrentContext();
            OnShipCompositionChanged?.Invoke();
        }

        public void ShowPaletteModuleInfo(ShipModuleSO moduleSO)
        {
            if (moduleSO == null) throw new ArgumentNullException(nameof(moduleSO));
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

        private void SelectBundle(ShipModuleSOInstanceBundle bundle)
        {
            ClearGhostSelection();

            var oldSelected = SelectedModuleBundle;
            SelectedModuleBundle = bundle;

            if (oldSelected != null) RefreshOverlayColor(oldSelected);
            if (SelectedModuleBundle != null) RefreshOverlayColor(SelectedModuleBundle);

            RefreshInfoPanelFromCurrentContext();
            ModuleSelected?.Invoke(SelectedModuleBundle);
        }

        private void SelectGhost(ModuleGhost ghost)
        {
            if (SelectedModuleBundle != null)
            {
                var old = SelectedModuleBundle;
                SelectedModuleBundle = null;
                RefreshOverlayColor(old);
            }

            if (SelectedGhost != null && SelectedGhost != ghost)
                SelectedGhost.SetSelected(false);

            SelectedGhost = ghost;
            SelectedGhost?.SetSelected(true);

            if (ghost != null)
            {
                _infoPanel.ApplyPaletteInfo(ghost.ModuleSO, false, IsInputLocked, false);
                GhostSelected?.Invoke(ghost);
            }
            else
            {
                RefreshInfoPanelFromCurrentContext();
                SelectionCleared?.Invoke();
            }
        }

        private void ClearGhostSelection()
        {
            if (SelectedGhost == null) return;
            SelectedGhost.SetSelected(false);
            SelectedGhost = null;
        }

        private void RefreshOverlayColor(ShipModuleSOInstanceBundle bundle)
        {
            if (bundle == null || bundle == _draggedModuleBundle) return;

            if (bundle == SelectedModuleBundle)
                OverlayManager.SetColor(bundle, ModuleOverlay.SelectedColor);
            else if (bundle == _hoveredPlacedBundle)
                OverlayManager.SetColor(bundle, ModuleOverlay.HoverColor);
            else
                OverlayManager.SetColor(bundle, ModuleOverlay.NormalColor);
        }

        private void RefreshInfoPanelFromCurrentContext()
        {
            if (IsDraggingModule)
                _infoPanel.ApplyPaletteInfo(_draggedModuleBundle.ModuleSO, _draggedModuleWasNew, IsInputLocked, true);
            else if (_hoveredPlacedBundle != null)
                _infoPanel.ApplyPaletteInfo(_hoveredPlacedBundle.ModuleSO, false, IsInputLocked, false);
            else if (_hoveredPaletteModule)
                _infoPanel.ApplyPaletteInfo(_hoveredPaletteModule, true, IsInputLocked, false);
            else if (SelectedModuleBundle != null)
                _infoPanel.ApplyPaletteInfo(SelectedModuleBundle.ModuleSO, false, IsInputLocked, false);
            else
                _infoPanel.ApplyEmptyInfo();
        }

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

            var worldPos = _gameInput.WorldPointerPosition;
            var bundle = OverlayManager.FindBundleAtWorldPosition(worldPos);
            if (bundle != null)
            {
                SelectBundle(bundle);
                BeginModuleDrag(bundle, false);
                evt.StopPropagation();
                return;
            }

            if (_ghostSelectionEnabled)
            {
                var ghost = OverlayManager.FindGhostAtWorldPosition(worldPos);
                if (ghost != null)
                {
                    SelectGhost(ghost);
                    evt.StopPropagation();
                    return;
                }
            }

            SelectGhost(null);
            if (SelectedModuleBundle != null)
            {
                var old = SelectedModuleBundle;
                SelectedModuleBundle = null;
                RefreshOverlayColor(old);
                RefreshInfoPanelFromCurrentContext();
                SelectionCleared?.Invoke();
            }
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

            var bundle = OverlayManager.FindBundleAtWorldPosition(_gameInput.WorldPointerPosition);
            if (bundle == _hoveredPlacedBundle) return;

            var oldHovered = _hoveredPlacedBundle;
            _hoveredPlacedBundle = bundle;
            _hoveredPaletteModule = null;

            if (oldHovered != null) RefreshOverlayColor(oldHovered);
            if (_hoveredPlacedBundle != null) RefreshOverlayColor(_hoveredPlacedBundle);

            RefreshInfoPanelFromCurrentContext();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!IsDraggingModule || IsInputLocked) return;
            HandleDragRelease();
        }

        public void BeginModuleDrop(ShipModuleSO shipModuleSO)
        {
            if (IsInputLocked) return;

            if (Ship == null)
            {
                Debug.LogWarning("[ShipFactory] No ship assigned — cannot place module.");
                return;
            }

            if (CanAffordModulePlacement != null && !CanAffordModulePlacement(shipModuleSO))
            {
                ShowWarningMessage("Not enough credits");
                OnModuleDragFinished?.Invoke();
                return;
            }

            _hoveredPaletteModule = null;

            if (!ShipHasModules)
            {
                PlaceFirstModuleAtOrigin(shipModuleSO);
                return;
            }

            if (ShipHasCommandModule && IsCommandModulePrefab(shipModuleSO))
            {
                ShowWarningMessage("Ship can only have 1 command module");
                OnModuleDragFinished?.Invoke();
                return;
            }

            var localSnapped = Snapper.SnapModuleLocalCenter(
                WorldToLayoutLocal(_gameInput.WorldPointerPosition),
                shipModuleSO.Dimensions);
            var worldPos = LayoutLocalToWorld(localSnapped);
            var bundle = InstantiateModule(shipModuleSO, worldPos);
            if (bundle == null) return;

            OverlayManager.CreateOverlay(bundle);
            BeginModuleDrag(bundle, true);
        }

        private void PlaceFirstModuleAtOrigin(ShipModuleSO shipModuleSO)
        {
            if (!IsCommandModulePrefab(shipModuleSO))
            {
                ShowWarningMessage("Place a command module first.");
                OnModuleDragFinished?.Invoke();
                return;
            }

            var localSnapped = Snapper.SnapModuleLocalCenter(Vector2.zero, shipModuleSO.Dimensions);
            var worldPos = LayoutLocalToWorld(localSnapped);
            var bundle = InstantiateModule(shipModuleSO, worldPos);
            if (bundle == null)
            {
                OnModuleDragFinished?.Invoke();
                return;
            }

            OverlayManager.CreateOverlay(bundle);
            SelectBundle(bundle);
            RefreshInfoPanelFromCurrentContext();
            _resourcesPanel.Refresh(Ship);
            OnModulePurchased?.Invoke(shipModuleSO);
            _feedback.PlayPlaced(worldPos);
            _cameraInfoPanel.RequestReset();
            OnModuleDragFinished?.Invoke();
            OnShipCompositionChanged?.Invoke();
        }

        private static bool IsCommandModulePrefab(ShipModuleSO shipModuleSO)
        {
            var prefabModule = shipModuleSO.Prefab.GetComponent<IModule>();
            if (prefabModule == null)
                throw new InvalidOperationException(
                    $"[ShipFactory] Prefab '{shipModuleSO.name}' has no IModule component!");

            return prefabModule.Type == ModuleType.Command;
        }

        private void BeginModuleDrag(ShipModuleSOInstanceBundle bundle, bool isNewBundle)
        {
            _draggedModuleBundle = bundle;
            _draggedModuleWasNew = isNewBundle;
            _dragStartWorldPos = bundle.Instance.transform.position;
            _dragStartLocalRotation = bundle.Instance.transform.localRotation;
            _hoveredPaletteModule = null;

            _dragWorldOffset = !isNewBundle
                ? (Vector2)bundle.Instance.transform.position - _gameInput.WorldPointerPosition
                : Vector2.zero;

            _dragStartLocalZ = bundle.Instance.transform.localPosition.z;
            var localPosition = bundle.Instance.transform.localPosition;
            localPosition.z = GetMaxModuleLocalZ(bundle) - 1f;
            bundle.Instance.transform.localPosition = localPosition;

            OverlayManager.BringOverlayToFront(bundle);
            OverlayManager.SyncTransformFromBundle(bundle);

            SelectBundle(bundle);
            RefreshInfoPanelFromCurrentContext();
            MoveGhostToPointer();
        }

        private void MoveGhostToPointer()
        {
            var draggedTransform = _draggedModuleBundle.Instance.transform;
            var localSnapped = Snapper.SnapModuleLocalCenter(
                WorldToLayoutLocal(_gameInput.WorldPointerPosition + _dragWorldOffset),
                _draggedModuleBundle.ModuleSO.Dimensions,
                draggedTransform.localRotation);
            var worldPos = LayoutLocalToWorld(localSnapped);
            OverlayManager.SetPosition(_draggedModuleBundle, worldPos);
            RefreshDraggedModuleLegalityOverlay();
        }

        private void HandleDragRelease()
        {
            var legality = Calculator.CalculatePositionLegality(_draggedModuleBundle, OverlayManager.AllBundles,
                OverlayManager.GetGhostBounds());
            if (legality == PositionLegality.Correct)
            {
                var wasNewModule = _draggedModuleWasNew;
                var placedModuleSo = _draggedModuleBundle.ModuleSO;
                var placedWorldPos = (Vector2)_draggedModuleBundle.Instance.transform.position;
                FinishActiveDrag();
                if (wasNewModule)
                {
                    OnModulePurchased?.Invoke(placedModuleSo);
                    _feedback.PlayPlaced(placedWorldPos);
                    OnShipCompositionChanged?.Invoke();
                }

                return;
            }

            var activeBundle = _draggedModuleBundle;
            var currentWorldPos = (Vector2)activeBundle.Instance.transform.position;

            SetInputLocked(true);

            if (_draggedModuleWasNew)
            {
                var bottomWorldTarget = _animator.CalculateOffScreenBottomPosition(currentWorldPos.x);

                _animator.AnimateBundleMovement(activeBundle, currentWorldPos, bottomWorldTarget, () =>
                {
                    if (Ship != null)
                        Ship.ManualRemoveModule(activeBundle.PlacedModule);

                    OverlayManager.RemoveOverlay(activeBundle);
                    Object.Destroy(activeBundle.Instance);

                    SelectBundle(null);
                    FinishActiveDrag();
                    SetInputLocked(false);
                });
                return;
            }

            RestoreDragStartRotation(activeBundle);

            _animator.AnimateBundleMovement(activeBundle, currentWorldPos, _dragStartWorldPos, () =>
            {
                RestoreDragStartRotation(activeBundle);
                FinishActiveDrag();
                SetInputLocked(false);
            });
        }

        private void FinishActiveDrag()
        {
            var finishedBundle = _draggedModuleBundle;
            _draggedModuleBundle = null;
            _draggedModuleWasNew = false;

            if (finishedBundle != null)
            {
                var localPosition = finishedBundle.Instance.transform.localPosition;
                localPosition.z = _dragStartLocalZ;
                finishedBundle.Instance.transform.localPosition = localPosition;
                OverlayManager.ResetOverlaySortingOrder(finishedBundle);
                OverlayManager.SyncTransformFromBundle(finishedBundle);
                RefreshOverlayColor(finishedBundle);

                if (finishedBundle.PlacedModule.Type == ModuleType.Command)
                    ReapplyNonCommandModulesFromLayout();

                finishedBundle.PlacedModule.SyncBlueprintLayoutFromTransform();
            }

            RefreshInfoPanelFromCurrentContext();
            _resourcesPanel.Refresh(Ship);
            OnModuleDragFinished?.Invoke();
        }

        private void RemoveSelectedModule()
        {
            if (SelectedModuleBundle == null || Ship == null || IsDraggingModule || IsInputLocked) return;

            if (SelectedModuleBundle.PlacedModule.Type == ModuleType.Command)
            {
                OnClearShipRequested?.Invoke();
                return;
            }

            if (WouldRemovalCreateIslands(SelectedModuleBundle))
            {
                ShowErrorMessage("Cannot remove this module: it would split the ship into islands.");
                return;
            }

            var removedWorldPos = (Vector2)SelectedModuleBundle.Instance.transform.position;

            Ship.ManualRemoveModule(SelectedModuleBundle.PlacedModule);
            OverlayManager.RemoveOverlay(SelectedModuleBundle);
            Object.Destroy(SelectedModuleBundle.Instance);

            if (_hoveredPlacedBundle == SelectedModuleBundle)
                _hoveredPlacedBundle = null;

            SelectBundle(null);
            _resourcesPanel.Refresh(Ship);
            _feedback.PlayDeleted(removedWorldPos);
            OnShipCompositionChanged?.Invoke();
        }

        public void ClearShip()
        {
            if (Ship == null || IsDraggingModule || IsInputLocked) return;

            var effectOrigin = Vector2.zero;
            foreach (var bundle in OverlayManager.AllBundles)
            {
                if (bundle.PlacedModule.Type != ModuleType.Command) continue;
                effectOrigin = bundle.Instance.transform.position;
                break;
            }

            ClearEntireShip(effectOrigin);
        }

        private void ClearEntireShip(Vector2 effectOrigin)
        {
            Ship.DestroyAllModulesSilently();
            Ship.InitializeModules();

            _draggedModuleBundle = null;
            _hoveredPlacedBundle = null;
            SelectBundle(null);
            RebuildShipModules();
            _feedback.PlayDeleted(effectOrigin);
            _cameraInfoPanel.RequestReset();
        }

        private bool WouldRemovalCreateIslands(ShipModuleSOInstanceBundle bundleToRemove)
        {
            var remainingBundles = OverlayManager.AllBundles.AsValueEnumerable()
                .Where(bundle => bundle != bundleToRemove).ToList();

            return remainingBundles.Count > 1 && remainingBundles.AsValueEnumerable()
                .Select(bundle => Calculator.CalculatePositionLegality(bundle, remainingBundles))
                .Any(legality => legality != PositionLegality.Correct);
        }

        [CanBeNull]
        private ShipModuleSOInstanceBundle InstantiateModule(ShipModuleSO shipModuleSO, Vector2 worldPosition)
        {
            var instance = _instantiator.InstantiatePrefab(shipModuleSO.Prefab, Ship.transform);
            var module = instance.GetComponent<IModule>();

            if (module == null)
            {
                Debug.LogError($"[ShipFactory] Prefab '{shipModuleSO.name}' has no IModule component!", shipModuleSO);
                Object.Destroy(instance);
                return null;
            }

            var layoutPosition = WorldToLayoutLocal(worldPosition);
            Ship.ManualAddModule(module);
            module.SetLocalPosition(layoutPosition);

            var identity = instance.GetComponent<GameObjectInstanceIdentity>();
            if (identity == null)
                identity = instance.AddComponent<GameObjectInstanceIdentity>();
            identity.EnsureAssigned(InstanceOrigin.CatalogPrefab, shipModuleSO.ArchetypeId);
            module.EnsureBlueprintIdentity();

            return new ShipModuleSOInstanceBundle(instance, shipModuleSO, module);
        }

        private void SetInputLocked(bool isLocked)
        {
            IsInputLocked = isLocked;
            _inputBlocker.visible = isLocked;
            RefreshInfoPanelFromCurrentContext();
            OnInputLockChanged?.Invoke(isLocked);
        }

        public void RefreshShipResourcesPanel()
        {
            _resourcesPanel.Refresh(Ship);
        }

        public void RotateActiveModule(int degrees)
        {
            if (degrees is not (90 or -90) || IsInputLocked) return;

            var bundle = _draggedModuleBundle ?? SelectedModuleBundle;
            if (bundle == null) return;

            var previousRotation = bundle.Instance.transform.localRotation;
            var deltaSteps = -degrees / 90;
            ModuleRotationUtility.ApplyQuarterTurn(bundle, deltaSteps);

            if (IsDraggingModule)
                ResnapDraggedModuleToGrid();

            OverlayManager.SyncTransformFromBundle(bundle);

            if (IsDraggingModule)
            {
                RefreshDraggedModuleLegalityOverlay();
                _feedback.PlayRotated(bundle.Instance.transform.position);
                return;
            }

            var legality = Calculator.CalculatePositionLegality(bundle, OverlayManager.AllBundles,
                OverlayManager.GetGhostBounds());
            if (legality == PositionLegality.Correct)
            {
                _feedback.PlayRotated(bundle.Instance.transform.position);
                return;
            }

            bundle.Instance.transform.localRotation = previousRotation;
            OverlayManager.SyncTransformFromBundle(bundle);

            var message = legality switch
            {
                PositionLegality.InsideOther => "Cannot rotate: modules would overlap.",
                PositionLegality.OutsideShip => "Cannot rotate: module would be outside the ship.",
                PositionLegality.DisconnectsShip => "Cannot rotate: it would split the ship into islands.",
                _ => "Cannot rotate module to this orientation."
            };

            if (legality == PositionLegality.DisconnectsShip)
                ShowErrorMessage(message);
            else
                ShowWarningMessage(message);
        }

        private void ResnapDraggedModuleToGrid()
        {
            var transform = _draggedModuleBundle.Instance.transform;
            var layoutPosition = WorldToLayoutLocal(transform.position);
            var localSnapped = Snapper.SnapModuleLocalCenter(
                layoutPosition,
                _draggedModuleBundle.ModuleSO.Dimensions,
                transform.localRotation);
            var worldPos = LayoutLocalToWorld(localSnapped);
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }

        private void RestoreDragStartRotation(ShipModuleSOInstanceBundle bundle)
        {
            bundle.Instance.transform.localRotation = _dragStartLocalRotation;
            OverlayManager.SyncTransformFromBundle(bundle);
        }

        private float GetMaxModuleLocalZ(ShipModuleSOInstanceBundle exclude = null)
        {
            return Ship.transform.Cast<Transform>().AsValueEnumerable()
                .Where(child => exclude == null || child.gameObject != exclude.Instance).Aggregate(0f,
                    (current, child) => Mathf.Max(current, child.localPosition.z));
        }

        private void RefreshDraggedModuleLegalityOverlay()
        {
            var legality = Calculator.CalculatePositionLegality(_draggedModuleBundle, OverlayManager.AllBundles,
                OverlayManager.GetGhostBounds());
            var color = legality switch
            {
                PositionLegality.InsideOther => ModuleOverlay.InsideOtherColor,
                PositionLegality.OutsideShip or PositionLegality.DisconnectsShip => ModuleOverlay.OutsideShipColor,
                _ => ModuleOverlay.SelectedColor
            };

            OverlayManager.SetColor(_draggedModuleBundle, color);
        }

        public void RefreshCameraInfoPanel(Camera camera)
        {
            _cameraInfoPanel.Update(camera);
        }

        private Vector2 WorldToLayoutLocal(Vector2 world)
        {
            return ShipLayoutSpace.WorldToLocal(Ship, world);
        }

        private Vector2 LayoutLocalToWorld(Vector2 layoutLocal)
        {
            return ShipLayoutSpace.LocalToWorld(Ship, layoutLocal);
        }

        private void ReapplyNonCommandModulesFromLayout()
        {
            if (Ship?.CommandModule?.Transform == null) return;

            var origin = Ship.CommandModule.Transform;
            foreach (var bundle in OverlayManager.AllBundles)
            {
                if (bundle.PlacedModule.Type == ModuleType.Command) continue;

                var blueprint = bundle.PlacedModule.Blueprint;
                if (blueprint == null) continue;

                var moduleTransform = bundle.Instance.transform;
                moduleTransform.SetPositionAndRotation(
                    origin.TransformPoint(blueprint.localPosition),
                    origin.rotation * blueprint.localRotation);
            }
        }

        public class Factory : PlaceholderFactory<VisualElement, ShipFactoryFeedback, ShipFactoryCanvasController>
        {
        }
    }
}