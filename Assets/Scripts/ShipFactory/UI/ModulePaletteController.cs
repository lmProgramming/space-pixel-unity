using System;
using System.Collections.Generic;
using Core.Services;
using Core.ShipFactory;
using Core.Ships;
using ShipFactory.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace ShipFactory.UI
{
    public class ModulePaletteController
    {
        private const string ActiveTabClass = "is-active";
        private const string CommandLimitTitle = "Command module";
        private const string CommandLimitDescription = "Ship can only have 1 command module";

        private readonly IShipModuleCatalog _library;
        private readonly VisualElement _paletteContent;
        private readonly Dictionary<ModuleType, Button> _tabButtons = new();

        private VisualElement _activeTabButton;
        private ModuleType _activeType = ModuleType.Command;
        private ModulePaletteCard _draggingCard;
        private bool _isInputLocked;
        private bool _shipHasCommandModule;
        private bool _shipHasModules;

        public ModulePaletteController(VisualElement root, IShipModuleCatalog library)
        {
            _library = library ?? throw new ArgumentNullException(nameof(library),
                "[ModulePaletteController] ShipModuleCatalog library must be assigned!");

            _paletteContent = root.Q<VisualElement>("palette-content");
            if (_paletteContent == null)
                throw new InvalidOperationException(
                    "[ModulePaletteController] 'palette-content' container not found in UXML!");

            BindTabButtons(root);
            SelectTab(ModuleType.Command, _tabButtons[ModuleType.Command]);
        }

        public event Action<ShipModuleSO, Vector2> OnModuleDragStarted;
        public event Action OnModuleDragFinished;
        public event Action<ShipModuleSO> OnModuleHoverStarted;
        public event Action<ShipModuleSO> OnModuleHoverEnded;
        public event Action<string, string> OnBlockedPlacementClicked;

        public void SyncToShipState(bool shipHasModules, bool shipHasCommandModule)
        {
            _shipHasModules = shipHasModules;
            _shipHasCommandModule = shipHasCommandModule;

            foreach (var (type, button) in _tabButtons)
                button.SetEnabled(shipHasModules || type == ModuleType.Command);

            if (!shipHasModules && _activeType != ModuleType.Command)
                SelectTab(ModuleType.Command, _tabButtons[ModuleType.Command]);
            else
                PopulatePalette();
        }

        private void BindTabButtons(VisualElement root)
        {
            BindTab(root, "tab-Command", ModuleType.Command);
            BindTab(root, "tab-Resources", ModuleType.Resources);
            BindTab(root, "tab-Weapon", ModuleType.Weapon);
            BindTab(root, "tab-Engine", ModuleType.Engine);
        }

        private void BindTab(VisualElement root, string buttonName, ModuleType type)
        {
            var button = root.Q<Button>(buttonName);

            _tabButtons[type] = button ?? throw new InvalidOperationException(
                $"[ModulePaletteController] Tab button '{buttonName}' not found in UXML!");
            button.clicked += () => SelectTab(type, button);
        }

        private void SelectTab(ModuleType type, VisualElement tabButton)
        {
            if (!_shipHasModules && type != ModuleType.Command)
                return;

            _activeTabButton?.RemoveFromClassList(ActiveTabClass);
            _activeType = type;
            _activeTabButton = tabButton;
            _activeTabButton.AddToClassList(ActiveTabClass);

            PopulatePalette();
        }

        private void PopulatePalette()
        {
            _paletteContent.Clear();

            var moduleSOs = _library.GetModuleSOsOfType(_activeType);
            var blockCommandPlacement = _shipHasCommandModule && _activeType == ModuleType.Command;

            foreach (var moduleSO in moduleSOs)
            {
                if (!moduleSO)
                {
                    Debug.LogWarning(
                        $"[ShipFactory] Null prefab entry found in library for type '{_activeType}' — skipping.");
                    continue;
                }

                BuildModuleCard(moduleSO, blockCommandPlacement);
            }
        }

        private void BuildModuleCard(ShipModuleSO moduleSO, bool isPlacementBlocked)
        {
            var card = ModulePaletteCard.Create();
            card.Bind(
                moduleSO,
                isPlacementBlocked,
                () => _isInputLocked,
                () => _draggingCard != null,
                OnCardDragStarted,
                OnCardDragFinished,
                module => OnModuleHoverStarted?.Invoke(module),
                module => OnModuleHoverEnded?.Invoke(module),
                () => OnBlockedPlacementClicked?.Invoke(CommandLimitTitle, CommandLimitDescription));
            _paletteContent.Add(card);
        }

        private void OnCardDragStarted(ModulePaletteCard card, ShipModuleSO moduleSO, Vector2 position)
        {
            _draggingCard = card;
            OnModuleDragStarted?.Invoke(moduleSO, position);
        }

        private void OnCardDragFinished(ModulePaletteCard card)
        {
            if (_draggingCard != card)
                return;

            OnModuleDragFinished?.Invoke();
        }

        public void FinishModuleDrag()
        {
            _draggingCard?.ClearDraggingVisual();
            _draggingCard = null;
        }

        public void SetInputLocked(bool isLocked)
        {
            _isInputLocked = isLocked;
        }

        public class Factory : PlaceholderFactory<VisualElement, ModulePaletteController>
        {
        }
    }
}