using System;
using Core.Services;
using UnityEngine.UIElements;
using Zenject;

namespace UI.MainGame
{
    public class MissionResultUIController : PanelRendererBase
    {
        [Inject]
        private IMissionService _missionService;

        private VisualElement _overlay;
        private Label _resultLabel;

        protected override void OnEnable()
        {
            base.OnEnable();
            _missionService.OnVictory += ShowVictory;
            _missionService.OnDefeat += ShowDefeat;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _missionService.OnVictory -= ShowVictory;
            _missionService.OnDefeat -= ShowDefeat;
        }

        protected override void BindUiCore(
            VisualElement root)
        {
            _overlay = root.Q<VisualElement>("mission-result-overlay");
            _resultLabel = root.Q<Label>("result-label");

            if (_overlay == null || _resultLabel == null)
                throw new InvalidOperationException(
                    "[MissionResultUIController] Mission result elements missing in UXML.");
        }

        protected override void UnbindUiCore()
        {
            _overlay = null;
            _resultLabel = null;
        }

        private void ShowVictory()
        {
            if (!IsUiBound)
                return;

            _resultLabel.text = "VICTORY";
            _resultLabel.RemoveFromClassList("defeat");
            _resultLabel.AddToClassList("victory");
            _overlay.RemoveFromClassList("hidden");
        }

        private void ShowDefeat()
        {
            if (!IsUiBound)
                return;

            _resultLabel.text = "DEFEAT";
            _resultLabel.RemoveFromClassList("victory");
            _resultLabel.AddToClassList("defeat");
            _overlay.RemoveFromClassList("hidden");
        }
    }
}