using System;
using Core.Ship;
using UnityEngine.UIElements;

namespace ShipFactory.UI.ToolkitComponents
{
    public class ModuleInfoPanel
    {
        private const string RemoveButtonHiddenClassName = "remove-module-button--hidden";
        private readonly Label _moduleDescriptionLabel;

        private readonly Label _moduleNameLabel;
        private readonly Label _moduleSizeLabel;
        private readonly Label _moduleTypeLabel;
        private readonly Button _removeModuleButton;
        private readonly Label _resourceCrewNeededLabel;
        private readonly Label _resourceCrewQuartersLabel;
        private readonly Label _resourceEnergyCapacityLabel;
        private readonly Label _resourceEnergyDrawLabel;
        private readonly Label _resourceEnergyProductionLabel;

        public ModuleInfoPanel(VisualElement root)
        {
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

            if (_moduleNameLabel == null || _moduleTypeLabel == null || _moduleSizeLabel == null ||
                _moduleDescriptionLabel == null || _resourceEnergyProductionLabel == null ||
                _resourceEnergyDrawLabel == null || _resourceEnergyCapacityLabel == null ||
                _resourceCrewNeededLabel == null || _resourceCrewQuartersLabel == null || _removeModuleButton == null)
                throw new InvalidOperationException(
                    "[ShipFactoryModuleInfoPanel] Required details panel elements are missing in UXML!");

            _removeModuleButton.clicked += () => OnRemoveModuleClicked?.Invoke();
        }

        public event Action OnRemoveModuleClicked;

        public void ApplyPaletteInfo(ShipModuleSO moduleSO, bool isNewModuleContext, bool isInputLocked,
            bool isDraggingModule)
        {
            var module = moduleSO.Prefab.GetComponent<IModule>();
            if (module == null)
                throw new InvalidOperationException(
                    $"[ShipFactoryModuleInfoPanel] Prefab '{moduleSO.Prefab.name}' is missing IModule component.");

            _moduleNameLabel.text = moduleSO.Name;
            _moduleTypeLabel.text = $"Type: {module.Type}";
            _moduleSizeLabel.text = $"Dimensions: {moduleSO.Dimensions.x}x{moduleSO.Dimensions.y}";
            _moduleDescriptionLabel.text = string.IsNullOrWhiteSpace(moduleSO.Description)
                ? "No description."
                : moduleSO.Description;

            ApplyResources(module.Resources);
            UpdateRemoveButton(isNewModuleContext, isInputLocked, isDraggingModule);
        }

        public void ApplyEmptyInfo()
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

        private void ApplyResources(Resources resources)
        {
            _resourceEnergyProductionLabel.text = $"Energy Production: {resources.energyProduction:0.##}";
            _resourceEnergyDrawLabel.text = $"Energy Draw: {resources.energyDraw:0.##}";
            _resourceEnergyCapacityLabel.text = $"Energy Capacity: {resources.energyCapacity:0.##}";
            _resourceCrewNeededLabel.text = $"Crew Needed: {resources.crewNeeded}";
            _resourceCrewQuartersLabel.text = $"Crew Quarters: {resources.crewQuarters}";
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
    }
}