using System;
using Core.Constants;
using Events.Game;
using Events.UI;
using UI.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UI.MainGame
{
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;

        [SerializeField] private PauseStateEventChannel pauseStateChannel;

        private bool _isPaused;
        private VisualElement _pauseOverlay;
        private VisualElement _pauseOverlayHost;
        private bool _pauseUiInitialized;
        private bool _pointerBlockersRegistered;
        private VisualElement _root;
        private SettingsPanelController _settingsPanelController;
        private UiPointerTracker _uiPointerTracker;

        private void Awake()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
        }

        private void LateUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsPanelController is { IsOpen: true })
            {
                _settingsPanelController.Hide();
                return;
            }

            SetPaused(!_isPaused);
        }

        private void OnEnable()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
                return;

            _root = uiDocument.rootVisualElement;
            InitializePauseUi();
            RegisterUiPointerBlockers();
        }

        private void OnDisable()
        {
            SetPaused(false);
        }

        private void InitializePauseUi()
        {
            if (_pauseUiInitialized || _root == null)
                return;

            _pauseOverlay = _root.Q<VisualElement>(SharedUiElementNames.Pause.Overlay);
            _pauseOverlayHost = _root.Q<VisualElement>(SharedUiElementNames.Pause.OverlayHost);
            var title = _root.Q<Label>(SharedUiElementNames.Pause.Title);
            var resumeButton = _root.Q<Button>(SharedUiElementNames.Pause.ResumeButton);
            var settingsButton = _root.Q<Button>(SharedUiElementNames.Pause.SettingsButton);
            var quitButton = _root.Q<Button>(SharedUiElementNames.Pause.QuitButton);

            if (title == null || resumeButton == null || settingsButton == null || quitButton == null)
                throw new InvalidOperationException("[PauseMenuController] Pause elements missing in PauseMenu UXML.");

            title.text = "Paused";
            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = DisplayStyle.None;
            _pauseOverlay.style.display = DisplayStyle.None;
            resumeButton.clicked += () => { SetPaused(false); };
            settingsButton.clicked += () => { _settingsPanelController.Toggle(); };
            quitButton.clicked += QuitToMainMenu;
            _settingsPanelController = new SettingsPanelController(_root, false);
            _pauseUiInitialized = true;
        }

        private void RegisterUiPointerBlockers()
        {
            if (_pointerBlockersRegistered || _root == null)
                return;

            if (pointerOverUiChannel == null)
                throw new InvalidOperationException(
                    "[PauseMenuController] Pointer Over UI event channel is not assigned. " +
                    "Assign the SAME PointerOverUiEventChannel asset that is set on GameProjectInstaller.");

            _uiPointerTracker = new UiPointerTracker(pointerOverUiChannel);
            TrackPointerBlocker(SharedUiElementNames.Pause.OverlayHost);
            TrackPointerBlocker(SharedUiElementNames.Settings.OverlayHost);
            _pointerBlockersRegistered = true;
        }

        private void TrackPointerBlocker(string elementName)
        {
            var element = _root.Q<VisualElement>(elementName);
            _uiPointerTracker.Track(element);
        }

        private void SetPaused(bool paused)
        {
            if (_isPaused == paused)
                return;

            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (pauseStateChannel)
                pauseStateChannel.Raise(paused);

            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
            if (_pauseOverlay != null)
                _pauseOverlay.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;

            if (!paused && _settingsPanelController != null && _settingsPanelController.IsOpen)
                _settingsPanelController.Hide();

            // A full-screen overlay that hides under a stationary pointer never emits PointerLeave,
            // so explicitly clear its pointer-over-UI state when leaving the paused state.
            if (!paused && _uiPointerTracker != null)
            {
                _uiPointerTracker.Release(_pauseOverlayHost);
                _uiPointerTracker.Release(_root?.Q<VisualElement>(SharedUiElementNames.Settings.OverlayHost));
            }
        }

        private static void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}