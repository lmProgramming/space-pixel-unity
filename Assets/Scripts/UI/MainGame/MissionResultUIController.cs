using Core.Services;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UI.MainGame
{
    public class MissionResultUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        [Inject]
        private IMissionService _missionService;

        private VisualElement _overlay;
        private Label _resultLabel;

        private void Awake()
        {
            Debug.Assert(uiDocument != null, "[MissionResultUIController] UIDocument is required!", this);
        }

        private void Start()
        {
            var root = uiDocument.rootVisualElement;
            _overlay = root.Q<VisualElement>("mission-result-overlay");
            _resultLabel = root.Q<Label>("result-label");

            Debug.Assert(_overlay != null, "[MissionResultUIController] 'mission-result-overlay' element not found!",
                this);
            Debug.Assert(_resultLabel != null, "[MissionResultUIController] 'result-label' element not found!", this);
        }

        private void OnEnable()
        {
            _missionService.OnVictory += ShowVictory;
            _missionService.OnDefeat += ShowDefeat;
        }

        private void OnDisable()
        {
            _missionService.OnVictory -= ShowVictory;
            _missionService.OnDefeat -= ShowDefeat;
        }

        private void ShowVictory()
        {
            _resultLabel.text = "VICTORY";
            _resultLabel.RemoveFromClassList("defeat");
            _resultLabel.AddToClassList("victory");
            _overlay.RemoveFromClassList("hidden");
        }

        private void ShowDefeat()
        {
            _resultLabel.text = "DEFEAT";
            _resultLabel.RemoveFromClassList("victory");
            _resultLabel.AddToClassList("defeat");
            _overlay.RemoveFromClassList("hidden");
        }
    }
}