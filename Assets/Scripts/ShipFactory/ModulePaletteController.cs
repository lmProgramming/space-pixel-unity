using System;
using Core.Pixelation;
using Core.Ships;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShipFactory
{
    public class ModulePaletteController
    {
        private const string ActiveTabClass = "is-active";
        private const string DraggingCardClass = "palette-card--dragging";
        private const string CardClass = "ds-card";
        private const string CardImageClass = "ds-card__image";
        private const string CardTitleRowClass = "ds-card__title-row";
        private const string CardTitleClass = "ds-body-2";
        private const string CardDimensionsClass = "palette-card__dimensions";
        private const string CardDimensionsTextClass = "ds-body-2";
        private const string CardLabelsClass = "palette-card__labels";
        private const string CardSpriteClass = "ds-card__sprite";
        private readonly ShipModuleCatalog _library;

        private readonly VisualElement _paletteContent;
        private VisualElement _activeTabButton;

        private ModuleType _activeType = ModuleType.Command;
        private VisualElement _draggingCard;

        private bool _isInputLocked;

        public ModulePaletteController(VisualElement root, ShipModuleCatalog library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library),
                    "[ModulePaletteController] ShipModuleCatalog library must be assigned!");

            _library = library;

            _paletteContent = root.Q<VisualElement>("palette-content");
            if (_paletteContent == null)
                throw new InvalidOperationException(
                    "[ModulePaletteController] 'palette-content' container not found in UXML!");

            BindTabButtons(root);
            SelectTab(ModuleType.Command, root.Q<Button>("tab-Command"));
        }

        public event Action<ShipModuleSO, Vector2> OnModuleDragStarted;
        public event Action OnModuleDragFinished;
        public event Action<ShipModuleSO> OnModuleHoverStarted;
        public event Action<ShipModuleSO> OnModuleHoverEnded;

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
            if (button == null)
                throw new InvalidOperationException(
                    $"[ModulePaletteController] Tab button '{buttonName}' not found in UXML!");

            button.clicked += () => SelectTab(type, button);
        }

        private void SelectTab(ModuleType type, VisualElement tabButton)
        {
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

            foreach (var moduleSO in moduleSOs)
            {
                if (moduleSO == null)
                {
                    Debug.LogWarning(
                        $"[ShipFactory] Null prefab entry found in library for type '{_activeType}' — skipping.");
                    continue;
                }

                _paletteContent.Add(BuildModuleCard(moduleSO));
            }
        }

        private VisualElement BuildModuleCard(ShipModuleSO moduleSO)
        {
            var card = new VisualElement();
            card.AddToClassList(CardClass);
            card.tooltip = moduleSO.Description;
            card.style.width = 132;
            card.style.flexShrink = 0;
            card.style.alignSelf = Align.Stretch;
            card.style.flexDirection = FlexDirection.Column;

            var image = new VisualElement();
            image.AddToClassList(CardImageClass);

            var pixelatedRigidbody = moduleSO.Prefab.GetComponent<IPixelatedSprite>();
            var sprite = pixelatedRigidbody?.GetSprite();
            if (sprite != null)
            {
                var spriteImage = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit };
                spriteImage.AddToClassList(CardSpriteClass);
                image.Add(spriteImage);
            }

            var titleRow = new VisualElement();
            titleRow.AddToClassList(CardTitleRowClass);
            titleRow.style.width = Length.Percent(100);

            var titleClip = new VisualElement
            {
                style =
                {
                    width = Length.Percent(100)
                }
            };

            var title = new Label(moduleSO.Name);
            title.AddToClassList(CardTitleClass);

            titleClip.Add(title);
            titleRow.Add(titleClip);

            var dimensions = new Label($"{moduleSO.Dimensions.x}x{moduleSO.Dimensions.y}");
            dimensions.AddToClassList(CardDimensionsClass);
            dimensions.AddToClassList(CardDimensionsTextClass);

            var labels = new VisualElement();
            labels.AddToClassList(CardLabelsClass);
            labels.Add(titleRow);
            labels.Add(dimensions);

            card.Add(image);
            card.Add(labels);
            _ = new HoverMarqueeLabel(card, titleClip, title);

            RegisterCardDragEvents(card, moduleSO);
            return card;
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

        private void RegisterCardDragEvents(VisualElement card, ShipModuleSO moduleSO)
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
    }
}