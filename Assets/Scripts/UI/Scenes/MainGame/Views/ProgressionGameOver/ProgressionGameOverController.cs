using System;
using Core.Constants;
using Core.UI;
using UI.Common;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.Scenes.MainGame.Views.ProgressionGameOver
{
    public class ProgressionGameOverController : PanelRendererBase, IProgressionGameOverController
    {
        private Label _creditsLabel;
        private Label _enemiesKilledLabel;
        private Button _mainMenuButton;
        private string _pendingCampaignName;
        private string _pendingCredits;
        private int _pendingEnemiesKilled;
        private Label _titleLabel;

        public void Render(string campaignName, string credits, int enemiesKilled)
        {
            _pendingCampaignName = campaignName;
            _pendingCredits = credits;
            _pendingEnemiesKilled = enemiesKilled;
        }

        protected override void BindUiCore(VisualElement root)
        {
            var overlay = root.Q<VisualElement>("progression-game-over-overlay");
            _titleLabel = root.Q<Label>("progression-game-over-title");
            _creditsLabel = root.Q<Label>("progression-game-over-credits");
            _enemiesKilledLabel = root.Q<Label>("progression-game-over-enemies-killed");
            _mainMenuButton = root.Q<Button>("progression-game-over-main-menu-button");

            if (overlay == null || _titleLabel == null || _creditsLabel == null || _enemiesKilledLabel == null ||
                _mainMenuButton == null)
                throw new InvalidOperationException(
                    "[ProgressionGameOverController] Required UI elements are missing in UXML.");

            _mainMenuButton.clicked += GoToMainMenu;
            ApplyPendingRender();
        }

        protected override void UnbindUiCore()
        {
            if (_mainMenuButton != null)
                _mainMenuButton.clicked -= GoToMainMenu;
        }

        private void ApplyPendingRender()
        {
            _titleLabel.text = string.IsNullOrWhiteSpace(_pendingCampaignName)
                ? "Game Over"
                : $"Game Over — {_pendingCampaignName}";
            _creditsLabel.text = $"Credits: {_pendingCredits ?? "0"}";
            _enemiesKilledLabel.text = $"Enemies killed: {_pendingEnemiesKilled}";
        }

        private static void GoToMainMenu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}