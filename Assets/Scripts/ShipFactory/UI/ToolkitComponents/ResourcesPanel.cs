using System;
using LMPro.External.IsAlive;
using Ships;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShipFactory.UI.ToolkitComponents
{
    public class ResourcesPanel
    {
        private readonly VisualElement _shipResourceCrewBufferFill;
        private readonly VisualElement _shipResourceCrewFill;
        private readonly Label _shipResourceCrewValueLabel;
        private readonly VisualElement _shipResourceEnergyBufferFill;
        private readonly VisualElement _shipResourceEnergyFill;
        private readonly Label _shipResourceEnergyValueLabel;
        private readonly Label _shipResourcesEnergyStorageLabel;
        private readonly VisualElement _shipResourcesPanel;

        public ResourcesPanel(VisualElement root)
        {
            _shipResourcesPanel = root.Q<VisualElement>("ship-resources-panel");
            _shipResourcesEnergyStorageLabel = root.Q<Label>("ship-resource-energy-storage");
            _shipResourceEnergyValueLabel = root.Q<Label>("ship-resource-energy-value");
            _shipResourceCrewValueLabel = root.Q<Label>("ship-resource-crew-value");
            _shipResourceEnergyFill = root.Q<VisualElement>("ship-resource-energy-fill");
            _shipResourceEnergyBufferFill = root.Q<VisualElement>("ship-resource-energy-buffer-fill");
            _shipResourceCrewFill = root.Q<VisualElement>("ship-resource-crew-fill");
            _shipResourceCrewBufferFill = root.Q<VisualElement>("ship-resource-crew-buffer-fill");

            if (_shipResourcesPanel == null || _shipResourcesEnergyStorageLabel == null ||
                _shipResourceEnergyValueLabel == null || _shipResourceCrewValueLabel == null ||
                _shipResourceEnergyFill == null || _shipResourceEnergyBufferFill == null ||
                _shipResourceCrewFill == null || _shipResourceCrewBufferFill == null)
                throw new InvalidOperationException(
                    "[ShipFactoryResourcesPanel] Required resources panel elements are missing in UXML!");
        }

        public void Refresh(Ship ship)
        {
            if (!ship || !ship.ResourceManager.IsAlive())
            {
                _shipResourcesPanel.style.display = DisplayStyle.None;
                return;
            }

            _shipResourcesPanel.style.display = DisplayStyle.Flex;

            var rm = ship.ResourceManager;

            var netEnergy = rm.EnergyProduction - rm.EnergyDraw;
            var netEnergyFormatted = netEnergy >= 0 ? $"+{netEnergy:0.#}" : $"{netEnergy:0.#}";

            _shipResourcesEnergyStorageLabel.text = $"{rm.EnergyCapacity:0.#} cap";

            _shipResourceEnergyValueLabel.text =
                $"{netEnergyFormatted} net";
            _shipResourceCrewValueLabel.text = $"{rm.Crew}/{rm.CrewCapacity}";

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
    }
}