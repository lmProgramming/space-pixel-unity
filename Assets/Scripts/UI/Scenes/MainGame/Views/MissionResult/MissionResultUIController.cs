using System;
using Core.UI;
using UI.Common;
using UnityEngine.UIElements;

namespace UI.Scenes.MainGame.Views.MissionResult
{
    public class MissionResultUIController : PanelRendererBase, IMissionResultUIController
    {
        private VisualElement _overlay;
        private PendingMissionResult _pendingResult = PendingMissionResult.None;
        private Label _resultLabel;

        public void ShowVictory()
        {
            _pendingResult = PendingMissionResult.Victory;
        }

        public void ShowDefeat()
        {
            _pendingResult = PendingMissionResult.Defeat;
        }

        protected override void BindUiCore(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("mission-result-overlay");
            _resultLabel = root.Q<Label>("result-label");

            if (_overlay == null || _resultLabel == null)
                throw new InvalidOperationException(
                    "[MissionResultUIController] Mission result elements missing in UXML.");

            ApplyPendingRender();
        }

        private void ApplyPendingRender()
        {
            switch (_pendingResult)
            {
                case PendingMissionResult.Victory:
                    _resultLabel.text = "VICTORY";
                    _resultLabel.RemoveFromClassList("defeat");
                    _resultLabel.AddToClassList("victory");
                    break;
                case PendingMissionResult.Defeat:
                    _resultLabel.text = "DEFEAT";
                    _resultLabel.RemoveFromClassList("victory");
                    _resultLabel.AddToClassList("defeat");
                    break;
                case PendingMissionResult.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private enum PendingMissionResult
        {
            None = 0,
            Victory = 1,
            Defeat = 2
        }
    }
}