using Ships;
using Ships.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Main
{
    /// <summary>
    /// Minimal UI controller for ship energy and crew status.
    /// Shows a vertical energy bar with flow indicator and crew count.
    /// </summary>
    public class ShipStatusPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Ship playerShip;
        [SerializeField] private UIDocument uiDocument;

        [Header("Animation Settings")]
        [SerializeField] private float barAnimationSpeed = 8f;
        [SerializeField] private float criticalEnergyThreshold = 0.2f;

        // UI Element References
        private VisualElement _root;
        private VisualElement _shipStatusPanel;
        private VisualElement _energyBarFill;
        private VisualElement _energyBarGlow;
        private Label _energyFlowLabel;
        private Label _crewCountLabel;

        // Animation state
        private float _currentEnergyBarHeight;
        private float _targetEnergyBarHeight;
        private float _lastNetEnergy;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
                return;

            _root = uiDocument.rootVisualElement;
            CacheUIReferences();
        }

        private void CacheUIReferences()
        {
            _shipStatusPanel = _root.Q<VisualElement>("ship-status-panel");
            _energyBarFill = _root.Q<VisualElement>("energy-bar-fill");
            _energyBarGlow = _root.Q<VisualElement>("energy-bar-glow");
            _energyFlowLabel = _root.Q<Label>("energy-flow-label");
            _crewCountLabel = _root.Q<Label>("crew-count-label");
        }

        private void Update()
        {
            if (playerShip == null || playerShip.ResourceManager == null)
            {
                SetPanelVisible(false);
                return;
            }

            SetPanelVisible(true);
            
            var resourceManager = playerShip.ResourceManager;
            UpdateEnergyDisplay(resourceManager);
            UpdateCrewDisplay(resourceManager);
            AnimateBars();
        }

        private void SetPanelVisible(bool visible)
        {
            if (_shipStatusPanel != null)
                _shipStatusPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateEnergyDisplay(ResourceManager resourceManager)
        {
            var energy = resourceManager.Energy;
            var energyCapacity = resourceManager.EnergyCapacity;
            var netEnergy = resourceManager.EnergyProduction - resourceManager.EnergyDraw;

            // Calculate target bar height (percentage)
            _targetEnergyBarHeight = energyCapacity > 0 ? (energy / energyCapacity) * 100f : 0f;

            // Update flow indicator
            UpdateFlowIndicator(netEnergy);

            // Update visual states
            UpdateEnergyStates(energy, energyCapacity, netEnergy);
        }

        private void UpdateFlowIndicator(float netEnergy)
        {
            if (_energyFlowLabel == null) return;

            _energyFlowLabel.RemoveFromClassList("negative");
            _energyFlowLabel.RemoveFromClassList("neutral");

            if (netEnergy > 0.1f)
            {
                _energyFlowLabel.text = "+";
            }
            else if (netEnergy < -0.1f)
            {
                _energyFlowLabel.text = "−";
                _energyFlowLabel.AddToClassList("negative");
            }
            else
            {
                _energyFlowLabel.text = "=";
                _energyFlowLabel.AddToClassList("neutral");
            }
        }

        private void UpdateEnergyStates(float energy, float energyCapacity, float netEnergy)
        {
            if (_shipStatusPanel == null) return;

            // Critical state
            var isCritical = energyCapacity > 0 && (energy / energyCapacity) < criticalEnergyThreshold;
            
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
            
            // Smooth energy bar animation
            _currentEnergyBarHeight = Mathf.Lerp(_currentEnergyBarHeight, _targetEnergyBarHeight, deltaTime * barAnimationSpeed);
            
            if (_energyBarFill != null)
                _energyBarFill.style.height = Length.Percent(_currentEnergyBarHeight);
            
            if (_energyBarGlow != null)
                _energyBarGlow.style.height = Length.Percent(_currentEnergyBarHeight);
        }

        /// <summary>
        /// Sets the player ship reference at runtime.
        /// </summary>
        public void SetPlayerShip(Ship ship)
        {
            playerShip = ship;
        }
    }
}
