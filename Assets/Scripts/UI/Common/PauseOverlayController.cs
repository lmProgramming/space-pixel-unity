using System;
using Core.Constants;
using Events.Game;
using Events.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.Common
{
    public class PauseOverlayController : PanelRendererBase
    {
        [SerializeField] private string pauseTitle = "Paused";
        [SerializeField] private SettingsOverlayController settingsOverlay;
        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;
        [SerializeField] private PauseStateEventChannel pauseStateChannel;
        [SerializeField] private bool handleEscapeInput = true;

        private VisualElement _pauseOverlay;
        private Button _quitButton;
        private Button _resumeButton;
        private Button _settingsButton;
        private UiPointerTracker _uiPointerTracker;

        public bool IsPaused { get; private set; }

        private void LateUpdate()
        {
            if (!handleEscapeInput || !Input.GetKeyDown(KeyCode.Escape))
                return;

            if (settingsOverlay && settingsOverlay.IsOpen)
            {
                settingsOverlay.Hide();
                return;
            }

            SetPaused(!IsPaused);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetPaused(false);
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

            title.text = pauseTitle;

            _resumeButton.clicked += OnResumeClicked;
            _settingsButton.clicked += OnSettingsClicked;
            _quitButton.clicked += QuitToMainMenu;

            if (pointerOverUiChannel)
            {
                _uiPointerTracker = new UiPointerTracker(pointerOverUiChannel);
                _uiPointerTracker.Track(_pauseOverlay);
            }
        }

        protected override void UnbindUiCore()
        {
            _resumeButton.clicked -= OnResumeClicked;
            _settingsButton.clicked -= OnSettingsClicked;
            _quitButton.clicked -= QuitToMainMenu;
        }

        private void OnResumeClicked()
        {
            SetPaused(false);
        }

        private void OnSettingsClicked()
        {
            settingsOverlay?.Toggle();
        }

        private void SetPaused(bool paused)
        {
            if (IsPaused == paused)
                return;

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (pauseStateChannel)
                pauseStateChannel.Raise(paused);

            if (paused) Show();
            else Hide();

            if (!paused)
            {
                if (settingsOverlay && settingsOverlay.IsOpen)
                    settingsOverlay.Hide();

                _uiPointerTracker?.Release(_pauseOverlay);
            }

            PauseChanged?.Invoke(paused);
        }

        private static void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}