using System;
using Core.Services;
using Core.Ships;
using Events.UI;
using LMPro.External.IsAlive;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UI.Scenes.MainGame.Views.ShipStatus
{
    public class ShipStatusPanelController : PanelRendererBase
    {
        [Header("References")]
        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;

        [Header("Animation Settings")]
        [SerializeField] private float barAnimationSpeed = 8f;

        [SerializeField] private float criticalEnergyThreshold = 0.2f;

        [Inject] private IActivePlayerShipProvider _activePlayerShipProvider;
        private Label _crewCountLabel;

        private float _currentEnergyBarHeight;
        private VisualElement _energyBarFill;
        private VisualElement _energyBarGlow;
        private Label _energyFlowLabel;
        private VisualElement _mainHudRoot;
        private VisualElement _root;
        private VisualElement _sasCluster;
        private bool _sasSyncing;
        private Toggle _sasToggle;
        private VisualElement _shipStatusPanel;
        private Label _speedValueLabel;
        private float _targetEnergyBarHeight;
        private UiPointerTracker _uiPointerTracker;

        private void Update()
        {
            if (!IsUiBound)
                return;

            if (_activePlayerShipProvider.ActiveShip == null || !_activePlayerShipProvider.ActiveShip.IsAlive())
            {
                SetMainHudVisible(false);
                return;
            }

            SetMainHudVisible(true);

            UpdateSpeedDisplay();
            UpdateSASDisplay();

            if (!_activePlayerShipProvider.ActiveShip.ResourceManager.IsAlive())
            {
                SetShipStatusBlockVisible(false);
                return;
            }

            SetShipStatusBlockVisible(true);

            var resourceManager = _activePlayerShipProvider.ActiveShip.ResourceManager;
            UpdateEnergyDisplay(resourceManager);
            UpdateCrewDisplay(resourceManager);
            AnimateBars();
        }

        protected override void BindUiCore(
            VisualElement root)
        {
            _root = root;
            CacheUIReferences();
            RegisterSASToggle();
            RegisterUiPointerBlockers();
        }

        protected override void UnbindUiCore()
        {
            UnregisterSASToggle();
        }

        private void CacheUIReferences()
        {
            _mainHudRoot = _root.Q<VisualElement>("main-hud-root");
            _sasCluster = _root.Q<VisualElement>("hud-sas-cluster");
            _shipStatusPanel = _root.Q<VisualElement>("ship-status-panel");
            _energyBarFill = _root.Q<VisualElement>("energy-bar-fill");
            _energyBarGlow = _root.Q<VisualElement>("energy-bar-glow");
            _energyFlowLabel = _root.Q<Label>("energy-flow-label");
            _crewCountLabel = _root.Q<Label>("crew-count-label");
            _speedValueLabel = _root.Q<Label>("speed-value-label");
            _sasToggle = _root.Q<Toggle>("sas-status-toggle");
        }

        private void RegisterUiPointerBlockers()
        {
            if (!pointerOverUiChannel)
                throw new InvalidOperationException(
                    "[ShipStatusPanelController] Pointer Over UI event channel is not assigned. " +
                    "Assign the SAME PointerOverUiEventChannel asset that is set on GameProjectInstaller.");

            _uiPointerTracker = new UiPointerTracker(pointerOverUiChannel);

            TrackPointerBlocker("hud-weapons-cluster");
            TrackPointerBlocker("hud-sas-cluster");
            TrackPointerBlocker("ship-status-panel");
            TrackPointerBlocker("speed-readout");
        }

        private void TrackPointerBlocker(string elementName)
        {
            var element = _root.Q<VisualElement>(elementName);
            _uiPointerTracker.Track(element);
        }

        private void RegisterSASToggle()
        {
            if (_sasToggle == null)
                return;

            _sasToggle.RegisterValueChangedCallback(OnSASToggleChanged);
        }

        private void UnregisterSASToggle()
        {
            _sasToggle?.UnregisterValueChangedCallback(OnSASToggleChanged);
        }

        private void OnSASToggleChanged(ChangeEvent<bool> evt)
        {
            if (_sasSyncing || _activePlayerShipProvider.ActiveShip is not ISAS playerShipTyped)
                return;
            if (playerShipTyped.IsSASOn == evt.newValue)
                return;
            playerShipTyped.ToggleSAS();
        }

        private void SetMainHudVisible(bool visible)
        {
            if (_mainHudRoot != null)
                _mainHudRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetShipStatusBlockVisible(bool visible)
        {
            if (_shipStatusPanel != null)
                _shipStatusPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateSpeedDisplay()
        {
            if (_speedValueLabel == null)
                return;

            var rb = _activePlayerShipProvider.ActiveShip.CommandModule?.PixelatedRigidbody?.Rigidbody;
            var magnitude = rb ? rb.linearVelocity.magnitude : 0f;
            _speedValueLabel.text = magnitude.ToString("F1");
        }

        private void UpdateSASDisplay()
        {
            if (_activePlayerShipProvider.ActiveShip is not ISAS playerShipTyped)
            {
                if (_sasCluster != null)
                    _sasCluster.style.display = DisplayStyle.None;
                return;
            }

            if (_sasCluster != null)
                _sasCluster.style.display = DisplayStyle.Flex;

            if (_sasToggle == null)
                return;

            _sasSyncing = true;
            _sasToggle.SetValueWithoutNotify(playerShipTyped!.IsSASOn);
            _sasSyncing = false;
        }

        private void UpdateEnergyDisplay(IResourceManager resourceManager)
        {
            var energy = resourceManager.Energy;
            var energyCapacity = resourceManager.EnergyCapacity;
            var netEnergy = resourceManager.EnergyProduction - resourceManager.EnergyDraw;

            _targetEnergyBarHeight = energyCapacity > 0 ? energy / energyCapacity * 100f : 0f;

            UpdateFlowIndicator(netEnergy);

            UpdateEnergyStates(energy, energyCapacity, netEnergy);
        }

        private void UpdateFlowIndicator(float netEnergy)
        {
            if (_energyFlowLabel == null) return;

            _energyFlowLabel.RemoveFromClassList("negative");
            _energyFlowLabel.RemoveFromClassList("neutral");

            switch (netEnergy)
            {
                case > 0.1f:
                    _energyFlowLabel.text = "+";
                    break;
                case < -0.1f:
                    _energyFlowLabel.text = "−";
                    _energyFlowLabel.AddToClassList("negative");
                    break;
                default:
                    _energyFlowLabel.text = "=";
                    _energyFlowLabel.AddToClassList("neutral");
                    break;
            }
        }

        private void UpdateEnergyStates(float energy, float energyCapacity, float netEnergy)
        {
            if (_shipStatusPanel == null) return;

            var isCritical = energyCapacity > 0 && energy / energyCapacity < criticalEnergyThreshold;

            _shipStatusPanel.EnableInClassList("energy-critical", isCritical);
            _shipStatusPanel.EnableInClassList("energy-gaining", !isCritical && netEnergy > 0.1f);
            _shipStatusPanel.EnableInClassList("energy-draining", !isCritical && netEnergy < -0.1f);
        }

        private void UpdateCrewDisplay(IResourceManager resourceManager)
        {
            if (_crewCountLabel != null)
                _crewCountLabel.text = resourceManager.Crew.ToString();
        }

        private void AnimateBars()
        {
            var deltaTime = Time.deltaTime;

            _currentEnergyBarHeight = Mathf.Lerp(_currentEnergyBarHeight, _targetEnergyBarHeight,
                deltaTime * barAnimationSpeed);

            if (_energyBarFill != null)
                _energyBarFill.style.height = Length.Percent(_currentEnergyBarHeight);

            if (_energyBarGlow != null)
                _energyBarGlow.style.height = Length.Percent(_currentEnergyBarHeight);
        }
    }
}