using Ships;
using Ships.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Main
{
    public class ShipStatusPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Ship playerShip;

        [SerializeField] private UIDocument uiDocument;

        [Header("Animation Settings")]
        [SerializeField] private float barAnimationSpeed = 8f;

        [SerializeField] private float criticalEnergyThreshold = 0.2f;
        private Label _crewCountLabel;

        private float _currentEnergyBarHeight;
        private VisualElement _energyBarFill;
        private VisualElement _energyBarGlow;
        private Label _energyFlowLabel;
        private VisualElement _mainHudRoot;
        private VisualElement _sasCluster;
        private bool _sasClickRegistered;
        private Label _sasStatusLabel;
        private Button _sasToggleButton;
        private Label _speedValueLabel;

        private VisualElement _root;
        private VisualElement _shipStatusPanel;
        private float _targetEnergyBarHeight;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void Update()
        {
            if (_mainHudRoot == null && uiDocument != null && uiDocument.rootVisualElement != null)
            {
                _root = uiDocument.rootVisualElement;
                CacheUIReferences();
                RegisterSasButton();
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
            RegisterSasButton();
        }

        private void OnDisable()
        {
            UnregisterSasButton();
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
            _sasStatusLabel = _root.Q<Label>("sas-status-label");
            _sasToggleButton = _root.Q<Button>("sas-toggle-button");
        }

        private void RegisterSasButton()
        {
            if (_sasClickRegistered || _sasToggleButton == null)
                return;

            _sasToggleButton.clicked += OnSasToggleClicked;
            _sasClickRegistered = true;
        }

        private void UnregisterSasButton()
        {
            if (!_sasClickRegistered || _sasToggleButton == null)
                return;

            _sasToggleButton.clicked -= OnSasToggleClicked;
            _sasClickRegistered = false;
        }

        private void OnSasToggleClicked()
        {
            if (playerShip is PlayerShip playerShipTyped)
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
            var magnitude = rb != null ? rb.linearVelocity.magnitude : 0f;
            _speedValueLabel.text = magnitude.ToString("F1");
        }

        private void UpdateSasDisplay()
        {
            var playerShipTyped = playerShip as PlayerShip;

            if (playerShipTyped == null)
            {
                if (_sasCluster != null)
                    _sasCluster.style.display = DisplayStyle.None;
                return;
            }

            if (_sasCluster != null)
                _sasCluster.style.display = DisplayStyle.Flex;

            var on = playerShipTyped.SasEnabled;
            if (_sasStatusLabel != null)
                _sasStatusLabel.text = on ? "SAS · ON" : "SAS · OFF";

            if (_sasToggleButton != null)
                _sasToggleButton.text = on ? "Turn off" : "Turn on";
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
