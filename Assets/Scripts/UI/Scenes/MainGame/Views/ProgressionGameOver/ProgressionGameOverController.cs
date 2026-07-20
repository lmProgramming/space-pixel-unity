using System;
using Core.Constants;
using UI.Common;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.Scenes.MainGame.Views.ProgressionGameOver
{
    public class ProgressionGameOverController : PanelRendererBase
    {
        private Label _creditsLabel;
        private Label _enemiesKilledLabel;
        private Button _mainMenuButton;
        private VisualElement _overlay;
        private Label _titleLabel;

        protected override void BindUiCore(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("progression-game-over-overlay");
            _titleLabel = root.Q<Label>("progression-game-over-title");
            _creditsLabel = root.Q<Label>("progression-game-over-credits");
            _enemiesKilledLabel = root.Q<Label>("progression-game-over-enemies-killed");
            _mainMenuButton = root.Q<Button>("progression-game-over-main-menu-button");

            if (_overlay == null || _titleLabel == null || _creditsLabel == null || _enemiesKilledLabel == null ||
                _mainMenuButton == null)
                throw new InvalidOperationException(
                    "[ProgressionGameOverController] Required UI elements are missing in UXML.");

            _mainMenuButton.clicked += GoToMainMenu;
            _overlay.style.display = DisplayStyle.None;
        }

        protected override void UnbindUiCore()
        {
            _mainMenuButton.clicked -= GoToMainMenu;
        }

        public void Show(string campaignName, string credits, int enemiesKilled)
        {
            _titleLabel.text = string.IsNullOrWhiteSpace(campaignName) ? "Game Over" : $"Game Over — {campaignName}";
            _creditsLabel.text = $"Credits: {credits ?? "0"}";
            _enemiesKilledLabel.text = $"Enemies killed: {enemiesKilled}";
            _overlay.style.display = DisplayStyle.Flex;
        }

        private static void GoToMainMenu()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}