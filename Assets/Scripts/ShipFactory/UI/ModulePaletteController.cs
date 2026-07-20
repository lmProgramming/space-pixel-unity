using System;
using System.Collections.Generic;
using Core.Pixelation;
using Core.Services;
using Core.ShipFactory;
using Core.Ships;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace ShipFactory.UI
{
    public class ModulePaletteController
    {
        private const string ModuleCardTemplatePath = "UI/ModuleCardTemplate";
        private const string ActiveTabClass = "is-active";
        private const string DraggingCardClass = "palette-card--dragging";
        private const string DisabledCardClass = "palette-card--disabled";
        private const string CommandLimitTitle = "Command module";
        private const string CommandLimitDescription = "Ship can only have 1 command module";

        private readonly IShipModuleCatalog _library;
        private readonly VisualTreeAsset _moduleCardTemplate;
        private readonly VisualElement _paletteContent;
        private readonly Dictionary<ModuleType, Button> _tabButtons = new();

        private VisualElement _activeTabButton;
        private ModuleType _activeType = ModuleType.Command;
        private VisualElement _draggingCard;
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

            _moduleCardTemplate = Resources.Load<VisualTreeAsset>(ModuleCardTemplatePath);
            if (!_moduleCardTemplate)
                throw new InvalidOperationException(
                    $"[ModulePaletteController] VisualTreeAsset '{ModuleCardTemplatePath}' was not found in Resources.");

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
            var cardIndex = _paletteContent.childCount;
            _moduleCardTemplate.CloneTree(_paletteContent);
            var card = _paletteContent[cardIndex];

            if (isPlacementBlocked)
                card.AddToClassList(DisabledCardClass);

            var spriteImage = card.Q<Image>("module-card-sprite")
                              ?? throw new InvalidOperationException(
                                  "[ModulePaletteController] 'module-card-sprite' is missing in ModuleCardTemplate.uxml.");
            var titleClip = card.Q<VisualElement>("module-card-title-clip")
                            ?? throw new InvalidOperationException(
                                "[ModulePaletteController] 'module-card-title-clip' is missing in ModuleCardTemplate.uxml.");
            var title = card.Q<Label>("module-card-title")
                        ?? throw new InvalidOperationException(
                            "[ModulePaletteController] 'module-card-title' is missing in ModuleCardTemplate.uxml.");
            var dimensions = card.Q<Label>("module-card-dimensions")
                             ?? throw new InvalidOperationException(
                                 "[ModulePaletteController] 'module-card-dimensions' is missing in ModuleCardTemplate.uxml.");

            var pixelatedRigidbody = moduleSO.Prefab.GetComponent<IPixelatedSprite>();
            var sprite = pixelatedRigidbody?.GetSprite();
            if (sprite)
            {
                spriteImage.sprite = sprite;
                spriteImage.scaleMode = ScaleMode.ScaleToFit;
            }
            else
            {
                spriteImage.style.display = DisplayStyle.None;
            }

            title.text = moduleSO.Name;
            dimensions.text = $"{moduleSO.Dimensions.x}x{moduleSO.Dimensions.y}";

            _ = new HoverMarqueeLabel(card, titleClip, title);
            RegisterCardDragEvents(card, moduleSO, isPlacementBlocked);
        }

        public void FinishModuleDrag()
        {
            _draggingCard?.RemoveFromClassList(DraggingCardClass);
            _draggingCard = null;
        }

        public void SetInputLocked(bool isLocked)
        {
            _isInputLocked = isLocked;
        }

        private void RegisterCardDragEvents(VisualElement card, ShipModuleSO moduleSO, bool isPlacementBlocked)
        {
            card.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (_isInputLocked || _draggingCard != null) return;
                OnModuleHoverStarted?.Invoke(moduleSO);
            });

            card.RegisterCallback<PointerLeaveEvent>(_ => { OnModuleHoverEnded?.Invoke(moduleSO); });

            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || _isInputLocked) return;

                if (isPlacementBlocked)
                {
                    OnBlockedPlacementClicked?.Invoke(CommandLimitTitle, CommandLimitDescription);
                    evt.StopPropagation();
                    return;
                }

                _draggingCard = card;
                card.AddToClassList(DraggingCardClass);
                OnModuleDragStarted?.Invoke(moduleSO, evt.position);
                evt.StopPropagation();
            });

            card.RegisterCallback<PointerUpEvent>(_ =>
            {
                if (_draggingCard != card) return;
                OnModuleDragFinished?.Invoke();
            });
        }

        public class Factory : PlaceholderFactory<VisualElement, ModulePaletteController>
        {
        }
    }
}