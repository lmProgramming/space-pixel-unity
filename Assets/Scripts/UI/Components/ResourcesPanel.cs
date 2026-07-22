using System;
using Core.Ships;
using LMPro.External.IsAlive;
using UnityEngine;
using UnityEngine.UIElements;
using Resources = UnityEngine.Resources;

namespace UI.Components
{
    [UxmlElement]
    public partial class ResourcesPanel : VisualElement
    {
        private VisualElement _shipResourceCrewBufferFill;
        private VisualElement _shipResourceCrewFill;
        private Label _shipResourceCrewValueLabel;
        private VisualElement _shipResourceEnergyBufferFill;
        private VisualElement _shipResourceEnergyFill;
        private Label _shipResourceEnergyValueLabel;
        private Label _shipResourcesEnergyStorageLabel;
        private VisualElement _shipResourcesPanel;

        public ResourcesPanel()
        {
            var asset = Resources.Load<VisualTreeAsset>("UI/ResourcesPanel");
            if (!asset)
                throw new InvalidOperationException(
                    "[ResourcesPanel] VisualTreeAsset 'UI/ResourcesPanel' was not found in Resources.");

            asset.CloneTree(this);

            CacheElements();
        }

        public void Refresh(IShip ship)
        {
            if (!ship.IsAlive() || !ship.ResourceManager.IsAlive())
            {
                _shipResourcesPanel.visible = false;
                return;
            }

            _shipResourcesPanel.visible = true;

            var rm = ship.ResourceManager;

            var netEnergy = rm.EnergyProduction - rm.EnergyDraw;
            var netEnergyFormatted = netEnergy >= 0 ? $"+{netEnergy:0.#}" : $"{netEnergy:0.#}";

            _shipResourcesEnergyStorageLabel.text = $"{rm.EnergyCapacity:0.#} cap";
            _shipResourceEnergyValueLabel.text = $"{netEnergyFormatted} net";
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

        private void CacheElements()
        {
            _shipResourcesPanel = this.Q<VisualElement>("ship-resources-panel");
            _shipResourcesEnergyStorageLabel = this.Q<Label>("ship-resource-energy-storage");
            _shipResourceEnergyValueLabel = this.Q<Label>("ship-resource-energy-value");
            _shipResourceCrewValueLabel = this.Q<Label>("ship-resource-crew-value");
            _shipResourceEnergyFill = this.Q<VisualElement>("ship-resource-energy-fill");
            _shipResourceEnergyBufferFill = this.Q<VisualElement>("ship-resource-energy-buffer-fill");
            _shipResourceCrewFill = this.Q<VisualElement>("ship-resource-crew-fill");
            _shipResourceCrewBufferFill = this.Q<VisualElement>("ship-resource-crew-buffer-fill");

            if (_shipResourcesPanel == null || _shipResourcesEnergyStorageLabel == null ||
                _shipResourceEnergyValueLabel == null || _shipResourceCrewValueLabel == null ||
                _shipResourceEnergyFill == null || _shipResourceEnergyBufferFill == null ||
                _shipResourceCrewFill == null || _shipResourceCrewBufferFill == null)
                throw new InvalidOperationException(
                    "[ResourcesPanel] Required resources panel elements are missing in UXML.");
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