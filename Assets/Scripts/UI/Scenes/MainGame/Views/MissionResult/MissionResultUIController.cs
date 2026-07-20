using System;
using UI.Common;
using UnityEngine.UIElements;

namespace UI.Scenes.MainGame.Views.MissionResult
{
    public class MissionResultUIController : PanelRendererBase
    {
        private VisualElement _overlay;
        private Label _resultLabel;

        protected override void BindUiCore(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("mission-result-overlay");
            _resultLabel = root.Q<Label>("result-label");

            if (_overlay == null || _resultLabel == null)
                throw new InvalidOperationException(
                    "[MissionResultUIController] Mission result elements missing in UXML.");
        }

        public void ShowVictory()
        {
            _resultLabel.text = "VICTORY";
            _resultLabel.RemoveFromClassList("defeat");
            _resultLabel.AddToClassList("victory");
            _overlay.RemoveFromClassList("hidden");
        }

        public void ShowDefeat()
        {
            _resultLabel.text = "DEFEAT";
            _resultLabel.RemoveFromClassList("victory");
            _resultLabel.AddToClassList("defeat");
            _overlay.RemoveFromClassList("hidden");
        }
    }
}