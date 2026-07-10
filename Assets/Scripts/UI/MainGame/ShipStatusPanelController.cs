using System;
using System.Resources;
using Core.Constants;
using Core.Ships;
using Events.UI;
using LMPro.External.IsAlive;
using UI.Common;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UI.MainGame
{
    [RequireComponent(typeof(PanelRenderer))]
    public class ShipStatusPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;

        [Header("Animation Settings")]
        [SerializeField] private float barAnimationSpeed = 8f;

        [SerializeField] private float criticalEnergyThreshold = 0.2f;
        private Label _crewCountLabel;

        private float _currentEnergyBarHeight;
        private VisualElement _energyBarFill;
        private VisualElement _energyBarGlow;
        private Label _energyFlowLabel;
        private bool _isBound;
        private VisualElement _mainHudRoot;

        private PanelRenderer _panelRenderer;

        [Inject(Id = Constants.PlayerShipId)] private IShip _playerShip;
        private VisualElement _root;
        private VisualElement _sasCluster;
        private bool _sasSyncing;
        private Toggle _sasToggle;
        private VisualElement _shipStatusPanel;
        private Label _speedValueLabel;
        private float _targetEnergyBarHeight;
        private UiPointerTracker _uiPointerTracker;
        private int _uiVersion = -1;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            if (_panelRenderer == null)
                throw new UnityException("[ShipStatusPanelController] PanelRenderer is required.");
        }

        private void Update()
        {
            if (!_isBound)
                return;

            if (!_playerShip.IsAlive())
            {
                SetMainHudVisible(false);
                return;
            }

            SetMainHudVisible(true);

            UpdateSpeedDisplay();
            UpdateSasDisplay();

            if (!_playerShip.ResourceManager)
            {
                SetShipStatusBlockVisible(false);
                return;
            }

            SetShipStatusBlockVisible(true);

            var resourceManager = _playerShip.ResourceManager;
            UpdateEnergyDisplay(resourceManager);
            UpdateCrewDisplay(resourceManager);
            AnimateBars();
        }

        private void OnEnable()
        {
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            UnbindUi();
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (version == _uiVersion && _isBound)
                return;

            if (version != _uiVersion)
                UnbindUi();

            _uiVersion = version;
            BindUi(root);
        }

        private void BindUi(VisualElement root)
        {
            if (_isBound || root == null)
                return;

            _root = root;
            CacheUIReferences();
            RegisterSasToggle();
            RegisterUiPointerBlockers();
            _isBound = true;
        }

        private void UnbindUi()
        {
            if (!_isBound)
                return;

            UnregisterSasToggle();

            _mainHudRoot = null;
            _sasCluster = null;
            _shipStatusPanel = null;
            _energyBarFill = null;
            _energyBarGlow = null;
            _energyFlowLabel = null;
            _crewCountLabel = null;
            _speedValueLabel = null;
            _sasToggle = null;
            _root = null;
            _uiPointerTracker = null;
            _isBound = false;
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

        private void RegisterSasToggle()
        {
            if (_sasToggle == null)
                return;

            _sasToggle.RegisterValueChangedCallback(OnSasToggleChanged);
        }

        private void UnregisterSasToggle()
        {
            if (_sasToggle == null)
                return;

            _sasToggle.UnregisterValueChangedCallback(OnSasToggleChanged);
        }

        private void OnSasToggleChanged(ChangeEvent<bool> evt)
        {
            if (_sasSyncing || _playerShip is not PlayerShip playerShipTyped)
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

            var rb = _playerShip.CommandModule?.PixelatedRigidbody?.Rigidbody;
            var magnitude = rb ? rb.linearVelocity.magnitude : 0f;
            _speedValueLabel.text = magnitude.ToString("F1");
        }

        private void UpdateSasDisplay()
        {
            var playerShipTyped = _playerShip as PlayerShip;

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
            _playerShip = ship;
        }
    }
}