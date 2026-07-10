using Core.Services;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UI.MainGame
{
    public class MissionResultUIController : MonoBehaviour
    {
        private bool _isBound;

        [Inject]
        private IMissionService _missionService;

        private VisualElement _overlay;
        private PanelRenderer _panelRenderer;
        private Label _resultLabel;
        private int _uiVersion = -1;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            Debug.Assert(_panelRenderer != null, "[MissionResultUIController] PanelRenderer is required!", this);
        }

        private void OnEnable()
        {
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
            _missionService.OnVictory += ShowVictory;
            _missionService.OnDefeat += ShowDefeat;
        }

        private void OnDisable()
        {
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            UnbindUi();
            _missionService.OnVictory -= ShowVictory;
            _missionService.OnDefeat -= ShowDefeat;
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

            _overlay = root.Q<VisualElement>("mission-result-overlay");
            _resultLabel = root.Q<Label>("result-label");

            Debug.Assert(_overlay != null, "[MissionResultUIController] 'mission-result-overlay' element not found!",
                this);
            Debug.Assert(_resultLabel != null, "[MissionResultUIController] 'result-label' element not found!", this);
            _isBound = true;
        }

        private void UnbindUi()
        {
            if (!_isBound)
                return;

            _overlay = null;
            _resultLabel = null;
            _isBound = false;
        }

        private void ShowVictory()
        {
            if (!_isBound)
                return;

            _resultLabel.text = "VICTORY";
            _resultLabel.RemoveFromClassList("defeat");
            _resultLabel.AddToClassList("victory");
            _overlay.RemoveFromClassList("hidden");
        }

        private void ShowDefeat()
        {
            if (!_isBound)
                return;

            _resultLabel.text = "DEFEAT";
            _resultLabel.RemoveFromClassList("victory");
            _resultLabel.AddToClassList("defeat");
            _overlay.RemoveFromClassList("hidden");
        }
    }
}