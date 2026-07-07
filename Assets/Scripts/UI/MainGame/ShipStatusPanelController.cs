using System;
using Events.UI;
using Ships;
using Ships.Systems.Resources;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.MainGame
{
    public class ShipStatusPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Ship playerShip;

        [SerializeField] private UIDocument uiDocument;

        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;

        [Header("Animation Settings")]
        [SerializeField] private float barAnimationSpeed = 8f;

        [SerializeField] private float criticalEnergyThreshold = 0.2f;
        private Label _crewCountLabel;

        private float _currentEnergyBarHeight;
        private VisualElement _energyBarFill;
        private VisualElement _energyBarGlow;
        private Label _energyFlowLabel;
        private VisualElement _mainHudRoot;
        private bool _pointerBlockersRegistered;

        private VisualElement _root;
        private VisualElement _sasCluster;
        private bool _sasHandlersRegistered;
        private bool _sasSyncing;
        private Toggle _sasToggle;
        private VisualElement _shipStatusPanel;
        private Label _speedValueLabel;
        private float _targetEnergyBarHeight;
        private UiPointerTracker _uiPointerTracker;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void Update()
        {
            if (_mainHudRoot == null && uiDocument && uiDocument.rootVisualElement != null)
            {
                _root = uiDocument.rootVisualElement;
                CacheUIReferences();
                RegisterSasToggle();
                RegisterUiPointerBlockers();
            }

            if (!playerShip)
            {
                SetMainHudVisible(false);
                return;
            }

            SetMainHudVisible(true);

            UpdateSpeedDisplay();
            UpdateSasDisplay();

            if (!playerShip.ResourceManager)
            {
                SetShipStatusBlockVisible(false);
                return;
            }

            SetShipStatusBlockVisible(true);

            var resourceManager = playerShip.ResourceManager;
            UpdateEnergyDisplay(resourceManager);
            UpdateCrewDisplay(resourceManager);
            AnimateBars();
        }

        private void OnEnable()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
                return;

            _root = uiDocument.rootVisualElement;
            CacheUIReferences();
            RegisterSasToggle();
            RegisterUiPointerBlockers();
        }

        private void OnDisable()
        {
            UnregisterSasToggle();
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
            if (_pointerBlockersRegistered || _root == null)
                return;

            if (pointerOverUiChannel == null)
                throw new InvalidOperationException(
                    "[ShipStatusPanelController] Pointer Over UI event channel is not assigned. " +
                    "Assign the SAME PointerOverUiEventChannel asset that is set on GameProjectInstaller.");

            _uiPointerTracker = new UiPointerTracker(pointerOverUiChannel);

            TrackPointerBlocker("hud-weapons-cluster");
            TrackPointerBlocker("hud-sas-cluster");
            TrackPointerBlocker("ship-status-panel");
            TrackPointerBlocker("speed-readout");

            _pointerBlockersRegistered = true;
        }

        private void TrackPointerBlocker(string elementName)
        {
            var element = _root.Q<VisualElement>(elementName);
            _uiPointerTracker.Track(element);
        }

        private void RegisterSasToggle()
        {
            if (_sasHandlersRegistered || _sasToggle == null)
                return;

            _sasToggle.RegisterValueChangedCallback(OnSasToggleChanged);
            _sasHandlersRegistered = true;
        }

        private void UnregisterSasToggle()
        {
            if (!_sasHandlersRegistered || _sasToggle == null)
                return;

            _sasToggle.UnregisterValueChangedCallback(OnSasToggleChanged);
            _sasHandlersRegistered = false;
        }

        private void OnSasToggleChanged(ChangeEvent<bool> evt)
        {
            if (_sasSyncing || playerShip is not PlayerShip playerShipTyped)
                return;
            if (playerShipTyped.IsSasOn == evt.newValue)
                return;
            playerShipTyped.ToggleSas();
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

            var rb = playerShip.CommandModule?.PixelatedRigidbody?.Rigidbody;
            var magnitude = rb ? rb.linearVelocity.magnitude : 0f;
            _speedValueLabel.text = magnitude.ToString("F1");
        }

        private void UpdateSasDisplay()
        {
            var playerShipTyped = playerShip as PlayerShip;

            if (!playerShipTyped)
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
            _sasToggle.SetValueWithoutNotify(playerShipTyped.IsSasOn);
            _sasSyncing = false;
        }

        private void UpdateEnergyDisplay(ResourceManager resourceManager)
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

        private void UpdateCrewDisplay(ResourceManager resourceManager)
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

        public void SetPlayerShip(Ship ship)
        {
            playerShip = ship;
        }
    }
}