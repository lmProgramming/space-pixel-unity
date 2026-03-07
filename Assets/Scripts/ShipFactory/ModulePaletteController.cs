using System;
using Core.Pixelation;
using Core.Ship;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShipFactory
{
    public class ModulePaletteController
    {
        private const string ActiveTabClass = "palette-tab--active";
        private const string DraggingCardClass = "module-card--dragging";
        private readonly ModulePrefabLibrary _library;

        private readonly VisualElement _paletteContent;
        private VisualElement _activeTabButton;

        private ModuleType _activeType = ModuleType.Command;
        private VisualElement _draggingCard;

        public ModulePaletteController(VisualElement root, ModulePrefabLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library),
                    "[ModulePaletteController] ModulePrefabLibrary must be assigned!");

            _library = library;

            var paletteScroll = root.Q<ScrollView>("palette-scroll");
            if (paletteScroll == null)
                throw new InvalidOperationException(
                    "[ModulePaletteController] 'palette-scroll' ScrollView not found in UXML!");

            _paletteContent = paletteScroll.contentContainer;

            BindTabButtons(root);
            SelectTab(ModuleType.Command, root.Q<Button>("tab-Command"));
        }

        public event Action<ShipModuleSO, Vector2> OnModuleDragStarted;

        private void BindTabButtons(VisualElement root)
        {
            BindTab(root, "tab-Command", ModuleType.Command);
            BindTab(root, "tab-Production", ModuleType.Production);
            BindTab(root, "tab-Storage", ModuleType.Storage);
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
            Debug.Log(
                $"[ShipFactory] Populating palette for type '{_activeType}': {moduleSOs.Count} prefab(s) found in library.");

            var built = 0;
            foreach (var moduleSO in moduleSOs)
            {
                if (moduleSO == null)
                {
                    Debug.LogWarning(
                        $"[ShipFactory] Null prefab entry found in library for type '{_activeType}' — skipping.");
                    continue;
                }

                _paletteContent.Add(BuildModuleCard(moduleSO));
                built++;
            }

            Debug.Log($"[ShipFactory] Built {built} module card(s) for type '{_activeType}'.");
        }

        private VisualElement BuildModuleCard(ShipModuleSO moduleSO)
        {
            var card = new VisualElement();
            card.AddToClassList("module-card");
            card.tooltip = moduleSO.Description;

            var prefab = moduleSO.Prefab;

            var icon = new VisualElement();
            icon.AddToClassList("module-card-icon");

            var pixelatedRigidbody = prefab.GetComponent<IPixelatedSprite>();
            var sprite = pixelatedRigidbody?.GetSprite();
            if (sprite != null)
                icon.style.backgroundImage = Background.FromSprite(sprite);

            var label = new Label(moduleSO.Name);
            label.AddToClassList("module-card-label");

            card.Add(icon);
            card.Add(label);

            RegisterCardDragEvents(card, moduleSO);
            return card;
        }

        private void RegisterCardDragEvents(VisualElement card, ShipModuleSO prefab)
        {
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                _draggingCard = card;
                card.AddToClassList(DraggingCardClass);
                // Do NOT capture the pointer — the root needs PointerMove/Up for ghost tracking and drop.
                OnModuleDragStarted?.Invoke(prefab, evt.position);
                evt.StopPropagation();
            });

            card.RegisterCallback<PointerUpEvent>(_ =>
            {
                if (_draggingCard != card) return;
                card.RemoveFromClassList(DraggingCardClass);
                _draggingCard = null;
            });
        }
    }
}