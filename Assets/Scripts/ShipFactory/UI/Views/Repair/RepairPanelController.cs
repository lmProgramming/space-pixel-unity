using System;
using System.Threading;
using Core.Constants;
using Core.Gameplay.Progression;
using Core.Services;
using Core.Services.Repair;
using Core.State;
using Core.UI;
using Cysharp.Threading.Tasks;
using Services.Repair;
using ShipFactory.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Zenject;

namespace ShipFactory.UI.Views.Repair
{
    public class RepairPanelController : IDisposable
    {
        private const string RemoveGhostConfirmOptionId = "confirm";
        private readonly Label _costLabel;
        private readonly Label _creditsLabel;

        private readonly Button _finishButton;
        private readonly VisualElement _fleetList;
        private readonly IGameUi _gameUi;
        private readonly ProgressionConstants _progressionConstants;
        private readonly IProgressionRepository _progressionRepository;
        private readonly Button _removeGhostButton;
        private readonly Button _repairAllButton;
        private readonly Button _repairModuleButton;
        private readonly IShipRepairRunner _repairRunner;
        private readonly IShipRepairService _repairService;
        private readonly VisualElement _root;
        private readonly Label _selectionLabel;
        private readonly IShipSnapshotService _snapshotService;
        private readonly Label _statusLabel;
        private ShipFactoryCanvasController _canvasController;
        private CancellationTokenSource _cts;
        private int _selectedAllyIndex;
        private ModuleGhost _selectedGhost;
        private ShipModuleSOInstanceBundle _selectedModule;
        private ProgressionSave _workingSave;

        public RepairPanelController(
            VisualElement shipFactoryRoot,
            IShipRepairService repairService,
            IShipRepairRunner repairRunner,
            IProgressionRepository progressionRepository,
            IShipSnapshotService snapshotService,
            IGameUi gameUi,
            ProgressionConstants progressionConstants)
        {
            _repairService = repairService ?? throw new ArgumentNullException(nameof(repairService));
            _repairRunner = repairRunner ?? throw new ArgumentNullException(nameof(repairRunner));
            _progressionRepository = progressionRepository ??
                                     throw new ArgumentNullException(nameof(progressionRepository));
            _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
            _gameUi = gameUi ?? throw new ArgumentNullException(nameof(gameUi));
            _progressionConstants =
                progressionConstants ?? throw new ArgumentNullException(nameof(progressionConstants));

            _root = shipFactoryRoot.Q<VisualElement>("repair-panel");
            if (_root == null)
                throw new InvalidOperationException("[RepairPanelController] repair-panel missing in UXML.");

            _creditsLabel = _root.Q<Label>("repair-credits-label");
            _fleetList = _root.Q<VisualElement>("repair-fleet-list");
            _selectionLabel = _root.Q<Label>("repair-selection-label");
            _costLabel = _root.Q<Label>("repair-cost-label");
            _statusLabel = _root.Q<Label>("repair-status-label");
            _repairModuleButton = _root.Q<Button>("repair-module-button");
            _repairAllButton = _root.Q<Button>("repair-all-button");
            _removeGhostButton = _root.Q<Button>("remove-ghost-button");
            _finishButton = _root.Q<Button>("finish-button");

            if (_creditsLabel == null || _fleetList == null || _selectionLabel == null || _costLabel == null ||
                _statusLabel == null || _repairModuleButton == null || _repairAllButton == null ||
                _removeGhostButton == null || _finishButton == null)
                throw new InvalidOperationException("[RepairPanelController] Required controls missing in UXML.");

            _repairModuleButton.clicked += OnRepairModuleClicked;
            _repairAllButton.clicked += OnRepairAllClicked;
            _removeGhostButton.clicked += OnRemoveGhostClicked;
            _finishButton.clicked += OnFinishClicked;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _repairModuleButton.clicked -= OnRepairModuleClicked;
            _repairAllButton.clicked -= OnRepairAllClicked;
            _removeGhostButton.clicked -= OnRemoveGhostClicked;
            _finishButton.clicked -= OnFinishClicked;

            if (_canvasController == null) return;
            _canvasController.ModuleSelected -= OnModuleSelected;
            _canvasController.GhostSelected -= OnGhostSelected;
            _canvasController.SelectionCleared -= OnSelectionCleared;
            _canvasController.OnShipCompositionChanged -= OnShipCompositionChanged;
        }

        public void Show(ShipFactoryCanvasController canvasController)
        {
            _canvasController = canvasController ?? throw new ArgumentNullException(nameof(canvasController));
            _canvasController.SetGhostSelectionEnabled(true);
            _canvasController.ModuleSelected += OnModuleSelected;
            _canvasController.GhostSelected += OnGhostSelected;
            _canvasController.SelectionCleared += OnSelectionCleared;
            _canvasController.OnShipCompositionChanged += OnShipCompositionChanged;
            _canvasController.CanAffordModulePlacement = moduleSo =>
                moduleSo != null && _workingSave.credits >= moduleSo.Cost;
            _canvasController.OnModulePurchased = moduleSo =>
            {
                if (moduleSo == null) return;
                if (_workingSave.credits < moduleSo.Cost)
                    throw new InvalidOperationException(
                        $"[RepairPanelController] Cannot afford module '{moduleSo.Name}'.");
                _workingSave.credits -= moduleSo.Cost;
                _creditsLabel.text = $"Credits: {_workingSave.credits}";
            };

            _root.style.display = DisplayStyle.Flex;
            _workingSave = _progressionRepository.Load(SaveState.ProgressionSlotIndex);
            _selectedAllyIndex = Mathf.Clamp(SaveState.SelectedAllyIndex, 0,
                Mathf.Max(0, _workingSave.allies.Length - 1));

            LoadSelectedAllyOntoCanvas();
            RefreshUi();
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        private void LoadSelectedAllyOntoCanvas()
        {
            if (_workingSave.allies == null || _workingSave.allies.Length == 0)
                throw new InvalidOperationException("[RepairPanelController] Progression save has no allies.");

            var snapshot = _workingSave.allies[_selectedAllyIndex];
            _snapshotService.ApplySnapshot(_canvasController.Ship, snapshot);
            _canvasController.Ship.transform.position = new Vector3(0, 0, 0);
            _canvasController.Ship.transform.rotation = Quaternion.identity;
            _canvasController.Ship.InitializeModules();
            _canvasController.RebuildShipModules();
            _repairService.BindShip(_canvasController.Ship);
        }

        private void PersistCurrentShipIntoWorkingSave()
        {
            var snapshot = _snapshotService.CaptureSnapshot(_canvasController.Ship);
            if (snapshot == null)
                throw new InvalidOperationException("[RepairPanelController] Failed to capture ship snapshot.");
            _workingSave.allies[_selectedAllyIndex] = snapshot;
        }

        private void RefreshUi()
        {
            _creditsLabel.text = $"Credits: {_workingSave.credits}";
            RebuildFleetList();
            RefreshSelectionLabels();
        }

        private void RebuildFleetList()
        {
            _fleetList.Clear();
            for (var i = 0; i < _workingSave.allies.Length; i++)
            {
                var index = i;
                var button = new Button(() => SelectAlly(index))
                {
                    text = string.IsNullOrWhiteSpace(_workingSave.allies[i].shipName)
                        ? $"Ship {i + 1}"
                        : _workingSave.allies[i].shipName
                };
                button.AddToClassList("ds-btn");
                button.AddToClassList(index == _selectedAllyIndex ? "ds-btn--primary" : "ds-btn--ghost");
                button.AddToClassList("ds-btn--block");
                button.style.marginBottom = 4;
                _fleetList.Add(button);
            }
        }

        private void SelectAlly(int index)
        {
            if (index == _selectedAllyIndex) return;
            PersistCurrentShipIntoWorkingSave();
            _selectedAllyIndex = index;
            _selectedModule = null;
            _selectedGhost = null;
            LoadSelectedAllyOntoCanvas();
            RefreshUi();
        }

        private void RefreshSelectionLabels()
        {
            if (_selectedGhost != null)
            {
                var cost = _repairService.EstimateRepairCostForBlueprint(_selectedGhost.Blueprint) *
                           Mathf.Max(0, _progressionConstants.creditsPerRepairedPixel);
                _selectionLabel.text = $"Ghost: {_selectedGhost.ModuleSO.Name}";
                _costLabel.text = $"Rebuild cost: {cost}";
                _removeGhostButton.style.display = DisplayStyle.Flex;
                _repairModuleButton.SetEnabled(!_repairRunner.IsRunning && cost > 0);
                return;
            }

            _removeGhostButton.style.display = DisplayStyle.None;

            if (_selectedModule != null)
            {
                var cost = _repairService.EstimateRepairCostForModule(_selectedModule.PlacedModule) *
                           Mathf.Max(0, _progressionConstants.creditsPerRepairedPixel);
                _selectionLabel.text = $"Module: {_selectedModule.ModuleSO.Name}";
                _costLabel.text = $"Repair cost: {cost}";
                _repairModuleButton.SetEnabled(!_repairRunner.IsRunning && cost > 0);
                return;
            }

            var allCost = _repairService.EstimateRepairCostForShip() *
                          Mathf.Max(0, _progressionConstants.creditsPerRepairedPixel);
            _selectionLabel.text = "No module selected";
            _costLabel.text = $"Repair All cost: {allCost}";
            _repairModuleButton.SetEnabled(false);
            _repairAllButton.SetEnabled(!_repairRunner.IsRunning && allCost > 0);
        }

        private void OnModuleSelected(ShipModuleSOInstanceBundle bundle)
        {
            _selectedModule = bundle;
            _selectedGhost = null;
            RefreshSelectionLabels();
        }

        private void OnGhostSelected(ModuleGhost ghost)
        {
            _selectedGhost = ghost;
            _selectedModule = null;
            RefreshSelectionLabels();
        }

        private void OnSelectionCleared()
        {
            _selectedGhost = null;
            _selectedModule = null;
            RefreshSelectionLabels();
        }

        private void OnShipCompositionChanged()
        {
            _repairService.BindShip(_canvasController.Ship);
            _canvasController.OverlayManager.RebuildGhosts(_canvasController.Ship);
            RefreshSelectionLabels();
        }

        private void OnRepairModuleClicked()
        {
            RunRepair(async token =>
            {
                if (_selectedGhost != null)
                    return await _repairRunner.RepairBlueprintAsync(_selectedGhost.Blueprint, GetCredits, SpendCredits,
                        token);
                if (_selectedModule != null)
                    return await _repairRunner.RepairModuleAsync(_selectedModule.PlacedModule, GetCredits, SpendCredits,
                        token);
                return new ShipRepairRunResult(ShipRepairStopReason.Failed, 0, "Nothing selected.");
            }).Forget();
        }

        private void OnRepairAllClicked()
        {
            RunRepair(token => _repairRunner.RepairAllAsync(GetCredits, SpendCredits, token)).Forget();
        }

        private async UniTaskVoid RunRepair(Func<CancellationToken, UniTask<ShipRepairRunResult>> work)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            SetBusy(true);
            _statusLabel.text = "Repairing...";

            try
            {
                var result = await work(_cts.Token);
                _statusLabel.text = result.Reason switch
                {
                    ShipRepairStopReason.Completed => $"Restored {result.PixelsRestored} pixels.",
                    ShipRepairStopReason.OutOfCredits => "Not enough credits",
                    ShipRepairStopReason.Cancelled => "Cancelled",
                    _ => result.Message ?? "Repair failed."
                };

                _canvasController.RebuildShipModules();
                _repairService.BindShip(_canvasController.Ship);
                RefreshUi();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private int GetCredits()
        {
            return _workingSave.credits;
        }

        private void SpendCredits(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (_workingSave.credits < amount)
                throw new InvalidOperationException("[RepairPanelController] Not enough credits.");
            _workingSave.credits -= amount;
            _creditsLabel.text = $"Credits: {_workingSave.credits}";
        }

        private void SetBusy(bool busy)
        {
            _repairModuleButton.SetEnabled(!busy);
            _repairAllButton.SetEnabled(!busy);
            _finishButton.SetEnabled(!busy);
            _removeGhostButton.SetEnabled(!busy);
            _canvasController.SetExternalInputLock(busy);
            if (!busy)
                RefreshSelectionLabels();
        }

        private void OnRemoveGhostClicked()
        {
            if (_selectedGhost == null) return;

            _gameUi.ShowOptions(
                "Remove ghost permanently?",
                "This module will never be restored. You can place a new module in its place afterwards.",
                optionId =>
                {
                    if (optionId != RemoveGhostConfirmOptionId) return;
                    _repairService.MarkBlueprintRemoved(_selectedGhost.Blueprint);
                    _canvasController.OverlayManager.RemoveGhost(_selectedGhost);
                    _selectedGhost = null;
                    RefreshSelectionLabels();
                },
                new OptionsPopupOption("cancel", "Cancel", OptionsPopupOptionStyle.Ghost),
                new OptionsPopupOption(RemoveGhostConfirmOptionId, "Remove", OptionsPopupOptionStyle.Danger));
        }

        private void OnFinishClicked()
        {
            PersistCurrentShipIntoWorkingSave();
            _progressionRepository.Save(SaveState.ProgressionSlotIndex, _workingSave);
            SceneManager.LoadScene(SceneNames.NextBattle);
        }

        public class Factory : PlaceholderFactory<VisualElement, RepairPanelController>
        {
        }
    }
}