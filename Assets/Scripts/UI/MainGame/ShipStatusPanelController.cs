using System;
using Core;
using Events.Game;
using Events.UI;
using Ships;
using Ships.Systems.Resources;
using UI.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.MainGame
{
    public class ShipStatusPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Ship playerShip;

        [SerializeField] private UIDocument uiDocument;

        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;

        [SerializeField] private PauseStateEventChannel pauseStateChannel;

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
        private bool _pointerBlockersRegistered;

        private VisualElement _root;
        private VisualElement _sasCluster;
        private bool _sasHandlersRegistered;
        private bool _sasSyncing;
        private Toggle _sasToggle;
        private SettingsPanelController _settingsPanelController;
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
                InitializePauseUi();
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

        private void LateUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsPanelController is { IsOpen: true })
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
            RegisterSasToggle();
            InitializePauseUi();
            RegisterUiPointerBlockers();
        }

        private void OnDisable()
        {
            UnregisterSasToggle();
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
            TrackPointerBlocker("pause-overlay-host");
            TrackPointerBlocker("settings-overlay-host");

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
            if (playerShipTyped.SasEnabled == evt.newValue)
                return;
            playerShipTyped.ToggleSas();
        }

        private void InitializePauseUi()
        {
            if (_pauseUiInitialized || _root == null)
                return;

            _pauseOverlay = _root.Q<VisualElement>(SharedUiElementNames.Pause.Overlay);
            _pauseOverlayHost = _root.Q<VisualElement>(SharedUiElementNames.Pause.OverlayHost);
            var title = _root.Q<Label>(SharedUiElementNames.Pause.Title);
            var resumeButton = _root.Q<Button>(SharedUiElementNames.Pause.ResumeButton);
            var settingsButton = _root.Q<Button>(SharedUiElementNames.Pause.SettingsButton);
            var quitButton = _root.Q<Button>(SharedUiElementNames.Pause.QuitButton);

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

            if (pauseStateChannel != null)
                pauseStateChannel.Raise(paused);

            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
            if (_pauseOverlay != null)
                _pauseOverlay.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;

            if (!paused && _settingsPanelController != null && _settingsPanelController.IsOpen)
                _settingsPanelController.Hide();

            // A full-screen overlay that hides under a stationary pointer never emits PointerLeave,
            // so explicitly clear its pointer-over-UI state when leaving the paused state.
            if (!paused && _uiPointerTracker != null)
            {
                _uiPointerTracker.Release(_pauseOverlayHost);
                _uiPointerTracker.Release(_root?.Q<VisualElement>(SharedUiElementNames.Settings.OverlayHost));
            }
        }

        private static void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
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
            _sasToggle.SetValueWithoutNotify(playerShipTyped.SasEnabled);
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