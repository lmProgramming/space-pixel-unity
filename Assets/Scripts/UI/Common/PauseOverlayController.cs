using System;
using Core.Constants;
using Events.Game;
using Events.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Zenject;

namespace UI.Common
{
    public class PauseOverlayController : PanelRendererBase
    {
        [SerializeField] private string pauseTitle = "Paused";

        private VisualElement _pauseOverlay;
        [Inject] private PauseStateEventChannel _pauseStateChannel;
        [Inject] private PointerOverUiEventChannel _pointerOverUiChannel;
        private Button _quitButton;
        private Button _resumeButton;
        private Button _settingsButton;
        private UiPointerTracker _uiPointerTracker;

        public bool IsPaused { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnterPausedState();
        }

        protected override void OnDisable()
        {
            ExitPausedState();
            base.OnDisable();
        }

        public event Action<bool> PauseChanged;

        protected override void BindUiCore(VisualElement root)
        {
            _pauseOverlay = root.Q<VisualElement>(SharedUiElementNames.Pause.Overlay);
            var title = root.Q<Label>(SharedUiElementNames.Pause.Title);
            _resumeButton = root.Q<Button>(SharedUiElementNames.Pause.ResumeButton);
            _settingsButton = root.Q<Button>(SharedUiElementNames.Pause.SettingsButton);
            _quitButton = root.Q<Button>(SharedUiElementNames.Pause.QuitButton);

            if (title == null || _resumeButton == null || _settingsButton == null || _quitButton == null ||
                _pauseOverlay == null)
                throw new InvalidOperationException("[PauseOverlayController] Pause elements missing in UXML.");

            if (GameUi == null)
                throw new InvalidOperationException("[PauseOverlayController] IGameUi is not injected.");

            if (_pauseStateChannel == null)
                throw new InvalidOperationException(
                    "[PauseOverlayController] PauseStateEventChannel is not injected.");

            if (_pointerOverUiChannel == null)
                throw new InvalidOperationException(
                    "[PauseOverlayController] PointerOverUiEventChannel is not injected.");

            title.text = pauseTitle;

            _resumeButton.clicked += OnResumeClicked;
            _settingsButton.clicked += OnSettingsClicked;
            _quitButton.clicked += QuitToMainMenu;

            _uiPointerTracker = new UiPointerTracker(_pointerOverUiChannel);
            _uiPointerTracker.Track(_pauseOverlay);
        }

        protected override void UnbindUiCore()
        {
            if (_resumeButton != null)
                _resumeButton.clicked -= OnResumeClicked;
            if (_settingsButton != null)
                _settingsButton.clicked -= OnSettingsClicked;
            if (_quitButton != null)
                _quitButton.clicked -= QuitToMainMenu;

            _uiPointerTracker?.Release(_pauseOverlay);
        }

        private void OnResumeClicked()
        {
            GameUi.Pop();
        }

        private void OnSettingsClicked()
        {
            GameUi.PushById<SettingsOverlayController>(UIPanelPrefabConstants.Settings);
        }

        private void EnterPausedState()
        {
            if (IsPaused)
                return;

            IsPaused = true;
            Time.timeScale = 0f;
            _pauseStateChannel?.Raise(true);
            PauseChanged?.Invoke(true);
        }

        private void ExitPausedState()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            Time.timeScale = 1f;
            _pauseStateChannel?.Raise(false);
            PauseChanged?.Invoke(false);
        }

        private static void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}