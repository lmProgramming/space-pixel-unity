using System;
using Ships;
using Ships.Internal;
using UI.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.MainGame
{
    public class ShipStatusPanelController : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";

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
        private bool _isPaused;
        private VisualElement _mainHudRoot;
        private VisualElement _pauseOverlay;
        private VisualElement _pauseOverlayHost;
        private bool _pauseUiInitialized;

        private VisualElement _root;
        private bool _sasClickRegistered;
        private VisualElement _sasCluster;
        private Label _sasStatusLabel;
        private Button _sasToggleButton;
        private SettingsPanelController _settingsPanelController;
        private VisualElement _shipStatusPanel;
        private Label _speedValueLabel;
        private float _targetEnergyBarHeight;

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
                RegisterSasButton();
                InitializePauseUi();
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

        private void LateUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsPanelController != null && _settingsPanelController.IsOpen)
            {
                _settingsPanelController.Hide();
                return;
            }

            SetPaused(!_isPaused);
        }

        private void OnEnable()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
                return;

            _root = uiDocument.rootVisualElement;
            CacheUIReferences();
            RegisterSasButton();
            InitializePauseUi();
        }

        private void OnDisable()
        {
            UnregisterSasButton();
            SetPaused(false);
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

        private void InitializePauseUi()
        {
            if (_pauseUiInitialized || _root == null)
                return;

            _pauseOverlay = _root.Q<VisualElement>("pause-overlay");
            _pauseOverlayHost = _root.Q<VisualElement>("pause-overlay-host");
            var title = _root.Q<Label>("pause-title");
            var resumeButton = _root.Q<Button>("pause-resume-button");
            var settingsButton = _root.Q<Button>("pause-settings-button");
            var quitButton = _root.Q<Button>("pause-quit-button");

            if (title == null || resumeButton == null || settingsButton == null || quitButton == null)
                throw new InvalidOperationException("[ShipStatusPanelController] Pause elements missing in HUD UXML.");

            title.text = "Paused";
            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = DisplayStyle.None;
            _pauseOverlay.style.display = DisplayStyle.None;
            resumeButton.clicked += () => { SetPaused(false); };
            settingsButton.clicked += () => { _settingsPanelController.Toggle(); };
            quitButton.clicked += QuitToMainMenu;
            _settingsPanelController = new SettingsPanelController(_root, false);
            _pauseUiInitialized = true;
        }

        private void SetMainHudVisible(bool visible)
        {
            if (_mainHudRoot != null)
                _mainHudRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetPaused(bool paused)
        {
            if (_isPaused == paused)
                return;

            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
            if (_pauseOverlay != null)
                _pauseOverlay.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;

            if (!paused && _settingsPanelController != null && _settingsPanelController.IsOpen)
                _settingsPanelController.Hide();
        }

        private static void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuSceneName);
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