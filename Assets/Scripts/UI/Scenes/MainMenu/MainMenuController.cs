using System;
using Core.Constants;
using Core.Gameplay;
using Core.Services;
using Core.State;
using UI.Common;
using UI.Scenes.MainMenu.Views.FreeMode;
using UI.Scenes.MainMenu.Views.Progression;
using UI.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Zenject;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Scenes.MainMenu
{
    public class MainMenuController : PanelRendererBase
    {
        private Button _progressionButton;
        [Inject] private IProgressionRepository _progressionRepository;
        private Button _quitButton;
        private Button _settingsButton;
        private Button _shipFactoryButton;
        [Inject] private IShipSnapshotRepository _snapshotRepository;
        private Button _startButton;

        private void Start()
        {
            if (_snapshotRepository == null)
                throw new InvalidOperationException("[MainMenuController] Snapshot repository is not initialized.");

            if (_progressionRepository == null)
                throw new InvalidOperationException("[MainMenuController] Progression repository is not initialized.");

            if (GameUi == null)
                throw new InvalidOperationException("[MainMenuController] IGameUi is not injected.");

            GameUi.SetRoot(this);
        }

        protected override void BindUiCore(VisualElement root)
        {
            _startButton = root.Q<Button>("start-button");
            _progressionButton = root.Q<Button>("progression-button");
            _shipFactoryButton = root.Q<Button>("ship-factory-button");
            _settingsButton = root.Q<Button>("settings-button");
            _quitButton = root.Q<Button>("quit-button");

            if (_startButton == null || _progressionButton == null || _shipFactoryButton == null ||
                _settingsButton == null || _quitButton == null)
                throw new InvalidOperationException(
                    "[MainMenuController] Required UI elements are missing in template.");

            _progressionButton.clicked += OpenProgressionSlots;
            _startButton.clicked += OpenFreeModeSetup;
            _shipFactoryButton.clicked += OpenShipFactory;
            _settingsButton.clicked += OpenSettings;
            _quitButton.clicked += QuitGame;

            DesignSystemThemeService.RegisterVisualTree(root);
        }

        protected override void UnbindUiCore()
        {
            if (_progressionButton != null)
                _progressionButton.clicked -= OpenProgressionSlots;
            if (_startButton != null)
                _startButton.clicked -= OpenFreeModeSetup;
            if (_shipFactoryButton != null)
                _shipFactoryButton.clicked -= OpenShipFactory;
            if (_settingsButton != null)
                _settingsButton.clicked -= OpenSettings;
            if (_quitButton != null)
                _quitButton.clicked -= QuitGame;
        }

        private void OpenProgressionSlots()
        {
            var slots = GameUi.PushById<ProgressionSlotsController>(UIPanelPrefabConstants.ProgressionSlots);
            slots.NewGameRequested += OnNewGameRequested;
            slots.LoadRequested += OnLoadRequested;
        }

        private void OnNewGameRequested(int slotIndex)
        {
            GameUi.Pop();
            Hide();
            var campaign = GameUi.PushById<NewCampaignController>(UIPanelPrefabConstants.NewCampaign);
            campaign.CloseSelected += OnNewCampaignClosed;
            campaign.OpenForSlot(slotIndex);
        }

        private void OnNewCampaignClosed()
        {
            Show();
        }

        private static void OnLoadRequested(int slotIndex)
        {
            SaveState.Mode = GameSessionMode.Progression;
            SaveState.ProgressionSlotIndex = slotIndex;
            SceneManager.LoadScene(SceneNames.NextBattle);
        }

        private void OpenFreeModeSetup()
        {
            GameUi.PushById<FreeModeSetupController>(UIPanelPrefabConstants.FreeModeSetup);
        }

        private static void OpenShipFactory()
        {
            SceneManager.LoadScene(SceneNames.ShipFactory);
        }

        private void OpenSettings()
        {
            GameUi.PushById<SettingsOverlayController>(UIPanelPrefabConstants.Settings);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}