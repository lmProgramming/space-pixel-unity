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
    [RequireComponent(typeof(PanelRenderer))]
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private PointerOverUiEventChannel pointerOverUiChannel;

        [SerializeField] private PauseStateEventChannel pauseStateChannel;

        private bool _isBound;
        private bool _isPaused;
        private PanelRenderer _panelRenderer;
        private VisualElement _pauseOverlay;
        private VisualElement _pauseOverlayHost;
        private Button _quitButton;
        private Button _resumeButton;
        private VisualElement _root;
        private Button _settingsButton;
        private SettingsPanelController _settingsPanelController;
        private UiPointerTracker _uiPointerTracker;
        private int _uiVersion = -1;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
            if (_panelRenderer == null)
                throw new UnityException("[PauseMenuController] PanelRenderer is required.");
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
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            UnbindUi();
            SetPaused(false);
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (version == _uiVersion && _isBound)
                return;

            if (version != _uiVersion)
                UnbindUi();

            _uiVersion = version;
            BindUi(root);
        }

        private void BindUi(VisualElement root)
        {
            if (_isBound || root == null)
                return;

            _root = root;
            _pauseOverlay = root.Q<VisualElement>(SharedUiElementNames.Pause.Overlay);
            _pauseOverlayHost = root.Q<VisualElement>(SharedUiElementNames.Pause.OverlayHost);
            var title = root.Q<Label>(SharedUiElementNames.Pause.Title);
            _resumeButton = root.Q<Button>(SharedUiElementNames.Pause.ResumeButton);
            _settingsButton = root.Q<Button>(SharedUiElementNames.Pause.SettingsButton);
            _quitButton = root.Q<Button>(SharedUiElementNames.Pause.QuitButton);

            if (title == null || _resumeButton == null || _settingsButton == null || _quitButton == null ||
                _pauseOverlay == null)
                throw new InvalidOperationException("[PauseMenuController] Pause elements missing in PauseMenu UXML.");

            title.text = "Paused";
            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = DisplayStyle.None;
            _pauseOverlay.style.display = DisplayStyle.None;

            _resumeButton.clicked += OnResumeClicked;
            _settingsButton.clicked += OnSettingsClicked;
            _quitButton.clicked += QuitToMainMenu;

            _settingsPanelController = new SettingsPanelController(root, false);
            RegisterUiPointerBlockers();
            _isBound = true;
        }

        private void UnbindUi()
        {
            if (!_isBound)
                return;

            _settingsPanelController?.Unbind();
            _settingsPanelController = null;

            _resumeButton.clicked -= OnResumeClicked;
            _settingsButton.clicked -= OnSettingsClicked;
            _quitButton.clicked -= QuitToMainMenu;

            _pauseOverlay = null;
            _pauseOverlayHost = null;
            _resumeButton = null;
            _settingsButton = null;
            _quitButton = null;
            _root = null;
            _uiPointerTracker = null;
            _isBound = false;
        }

        private void RegisterUiPointerBlockers()
        {
            if (pointerOverUiChannel == null)
                throw new InvalidOperationException(
                    "[PauseMenuController] Pointer Over UI event channel is not assigned. " +
                    "Assign the SAME PointerOverUiEventChannel asset that is set on GameProjectInstaller.");

            _uiPointerTracker = new UiPointerTracker(pointerOverUiChannel);
            TrackPointerBlocker(SharedUiElementNames.Pause.OverlayHost);
            TrackPointerBlocker(SharedUiElementNames.Settings.OverlayHost);
        }

        private void TrackPointerBlocker(string elementName)
        {
            var element = _root.Q<VisualElement>(elementName);
            _uiPointerTracker.Track(element);
        }

        private void OnResumeClicked()
        {
            SetPaused(false);
        }

        private void OnSettingsClicked()
        {
            _settingsPanelController?.Toggle();
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