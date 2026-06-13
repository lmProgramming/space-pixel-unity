using System;
using Core.Ship;
using UnityEngine.UIElements;

namespace ShipFactory.UI.ToolkitComponents
{
    public class ModuleInfoPanel
    {
        private const string RemoveButtonHiddenClassName = "remove-module-button--hidden";
        private const string RotationButtonsHiddenClassName = "module-rotation-buttons--hidden";
        private readonly Label _moduleDescriptionLabel;

        private readonly Label _moduleNameLabel;
        private readonly Label _moduleSizeValueLabel;
        private readonly Label _moduleTypeValueLabel;
        private readonly Button _removeModuleButton;
        private readonly Label _resourceCrewNeededValueLabel;
        private readonly Label _resourceCrewQuartersValueLabel;
        private readonly Label _resourceEnergyCapacityValueLabel;
        private readonly Label _resourceEnergyDrawValueLabel;
        private readonly Label _resourceEnergyProductionValueLabel;
        private readonly Button _rotateClockwiseButton;
        private readonly Button _rotateCounterButton;
        private readonly VisualElement _rotationButtonsContainer;

        public ModuleInfoPanel(VisualElement root)
        {
            _moduleNameLabel = root.Q<Label>("module-info-name");
            _moduleTypeValueLabel = root.Q<Label>("module-info-type-value");
            _moduleSizeValueLabel = root.Q<Label>("module-info-size-value");
            _moduleDescriptionLabel = root.Q<Label>("module-info-description");
            _resourceEnergyProductionValueLabel = root.Q<Label>("module-info-resource-energy-production-value");
            _resourceEnergyDrawValueLabel = root.Q<Label>("module-info-resource-energy-draw-value");
            _resourceEnergyCapacityValueLabel = root.Q<Label>("module-info-resource-energy-capacity-value");
            _resourceCrewNeededValueLabel = root.Q<Label>("module-info-resource-crew-needed-value");
            _resourceCrewQuartersValueLabel = root.Q<Label>("module-info-resource-crew-quarters-value");
            _removeModuleButton = root.Q<Button>("remove-module-button");
            _rotationButtonsContainer = root.Q<VisualElement>("module-rotation-buttons");
            _rotateCounterButton = root.Q<Button>("rotate-counter-button");
            _rotateClockwiseButton = root.Q<Button>("rotate-clockwise-button");

            if (_moduleNameLabel == null || _moduleTypeValueLabel == null || _moduleSizeValueLabel == null ||
                _moduleDescriptionLabel == null || _resourceEnergyProductionValueLabel == null ||
                _resourceEnergyDrawValueLabel == null || _resourceEnergyCapacityValueLabel == null ||
                _resourceCrewNeededValueLabel == null || _resourceCrewQuartersValueLabel == null ||
                _removeModuleButton == null ||
                _rotationButtonsContainer == null || _rotateCounterButton == null || _rotateClockwiseButton == null)
                throw new InvalidOperationException(
                    "[ShipFactoryModuleInfoPanel] Required details panel elements are missing in UXML!");

            _removeModuleButton.clicked += () => OnRemoveModuleClicked?.Invoke();
            _rotateClockwiseButton.clicked += () => OnRotateClockwiseClicked?.Invoke();
            _rotateCounterButton.clicked += () => OnRotateCounterClockwiseClicked?.Invoke();
        }

        public event Action OnRemoveModuleClicked;
        public event Action OnRotateClockwiseClicked;
        public event Action OnRotateCounterClockwiseClicked;

        public void ApplyPaletteInfo(ShipModuleSO moduleSO, bool isNewModuleContext, bool isInputLocked,
            bool isDraggingModule)
        {
            var module = moduleSO.Prefab.GetComponent<IModule>();
            if (module == null)
                throw new InvalidOperationException(
                    $"[ShipFactoryModuleInfoPanel] Prefab '{moduleSO.Prefab.name}' is missing IModule component.");

            _moduleNameLabel.text = moduleSO.Name;
            _moduleTypeValueLabel.text = module.Type.ToString();
            _moduleSizeValueLabel.text = $"{moduleSO.Dimensions.x}x{moduleSO.Dimensions.y}";
            _moduleDescriptionLabel.text = string.IsNullOrWhiteSpace(moduleSO.Description)
                ? "No description."
                : moduleSO.Description;

            ApplyResources(module.Resources);
            UpdateRemoveButton(isNewModuleContext, isInputLocked, isDraggingModule);
            UpdateRotationButtons(isNewModuleContext, isInputLocked, isDraggingModule);
        }

        public void ApplyEmptyInfo()
        {
            _moduleNameLabel.text = "No module selected";
            _moduleTypeValueLabel.text = "-";
            _moduleSizeValueLabel.text = "-";
            _moduleDescriptionLabel.text = "Hover or drag a module to inspect it.";

            _resourceEnergyProductionValueLabel.text = "-";
            _resourceEnergyDrawValueLabel.text = "-";
            _resourceEnergyCapacityValueLabel.text = "-";
            _resourceCrewNeededValueLabel.text = "-";
            _resourceCrewQuartersValueLabel.text = "-";

            _removeModuleButton.SetEnabled(false);
            _removeModuleButton.AddToClassList(RemoveButtonHiddenClassName);

            _rotateClockwiseButton.SetEnabled(false);
            _rotateCounterButton.SetEnabled(false);
            _rotationButtonsContainer.AddToClassList(RotationButtonsHiddenClassName);
        }

        private void ApplyResources(Resources resources)
        {
            _resourceEnergyProductionValueLabel.text = $"{resources.energyProduction:0.##}";
            _resourceEnergyDrawValueLabel.text = $"{resources.energyDraw:0.##}";
            _resourceEnergyCapacityValueLabel.text = $"{resources.energyCapacity:0.##}";
            _resourceCrewNeededValueLabel.text = $"{resources.crewNeeded}";
            _resourceCrewQuartersValueLabel.text = $"{resources.crewQuarters}";
        }

        private void UpdateRemoveButton(bool isNewModuleContext, bool isInputLocked, bool isDraggingModule)
        {
            if (isNewModuleContext)
            {
                _removeModuleButton.SetEnabled(false);
                _removeModuleButton.AddToClassList(RemoveButtonHiddenClassName);
                return;
            }

            _removeModuleButton.RemoveFromClassList(RemoveButtonHiddenClassName);
            _removeModuleButton.SetEnabled(!isInputLocked && !isDraggingModule);
        }

        private void UpdateRotationButtons(bool isNewModuleContext, bool isInputLocked, bool isDraggingModule)
        {
            var showRotationButtons = isDraggingModule || !isNewModuleContext;
            if (!showRotationButtons)
            {
                _rotateClockwiseButton.SetEnabled(false);
                _rotateCounterButton.SetEnabled(false);
                _rotationButtonsContainer.AddToClassList(RotationButtonsHiddenClassName);
                return;
            }

            _rotationButtonsContainer.RemoveFromClassList(RotationButtonsHiddenClassName);
            var isEnabled = !isInputLocked;
            _rotateClockwiseButton.SetEnabled(isEnabled);
            _rotateCounterButton.SetEnabled(isEnabled);
        }
    }
}