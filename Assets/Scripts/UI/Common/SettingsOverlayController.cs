using System;
using System.Collections.Generic;
using Core.Constants;
using DesignSystem.Runtime;
using DesignSystem.Showcase.Runtime;
using Events.UI;
using UI.Stack;
using UI.Tools;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace UI.Common
{
    public class SettingsOverlayController : PanelRendererBase
    {
        private VisualElement _backdrop;
        private Button _closeButton;
        private int _codigrateFetchGeneration;
        private Slider _effectsSlider;

        [Inject] private IGameUi _gameUi;
        private Slider _masterSlider;
        private Slider _musicSlider;
        [Inject] private PointerOverUiEventChannel _pointerOverUiChannel;
        private bool _suppressPersist;
        private DropdownField _themeProviderDropdown;
        private Toggle _themeToggle;
        private UiPointerTracker _uiPointerTracker;

        protected override void BindUiCore(VisualElement root)
        {
            _backdrop = root.Q<VisualElement>(SharedUiElementNames.Settings.Overlay);
            var titleLabel = root.Q<Label>(SharedUiElementNames.Settings.Title);
            _masterSlider = root.Q<Slider>(SharedUiElementNames.Settings.MasterSlider);
            _musicSlider = root.Q<Slider>(SharedUiElementNames.Settings.MusicSlider);
            _effectsSlider = root.Q<Slider>(SharedUiElementNames.Settings.EffectsSlider);
            _closeButton = root.Q<Button>(SharedUiElementNames.Settings.CloseButton);
            _themeToggle = root.Q<Toggle>("theme-toggle");
            _themeProviderDropdown = root.Q<DropdownField>("theme-provider-dropdown");

            if (titleLabel == null || _masterSlider == null || _musicSlider == null || _effectsSlider == null ||
                _closeButton == null || _backdrop == null)
                throw new InvalidOperationException(
                    "[SettingsOverlayController] Required settings elements are missing in UXML.");

            if (_gameUi == null)
                throw new InvalidOperationException("[SettingsOverlayController] IGameUi is not injected.");

            titleLabel.text = "Settings";
            DesignSystemThemeService.RegisterVisualTree(root);
            DesignSystemRuntime.EnsureToggleKnobs(root);

            _closeButton.clicked += OnCloseClicked;
            _masterSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
            _musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
            _effectsSlider.RegisterValueChangedCallback(OnEffectsVolumeChanged);
            _themeToggle?.RegisterValueChangedCallback(OnThemeToggleChanged);

            WireThemeProvider();
            LoadFromPlayerPrefs();

            _uiPointerTracker = new UiPointerTracker(_pointerOverUiChannel);
            _uiPointerTracker.Track(_backdrop);
        }

        protected override void UnbindUiCore()
        {
            _codigrateFetchGeneration++;
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseClicked;
            _masterSlider?.UnregisterValueChangedCallback(OnMasterVolumeChanged);
            _musicSlider?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
            _effectsSlider?.UnregisterValueChangedCallback(OnEffectsVolumeChanged);
            _themeToggle?.UnregisterValueChangedCallback(OnThemeToggleChanged);
            _themeProviderDropdown?.UnregisterValueChangedCallback(OnThemeProviderChanged);
            _uiPointerTracker?.Release(_backdrop);
        }

        public override void Show()
        {
            base.Show();
            LoadFromPlayerPrefs();
        }

        private void OnCloseClicked()
        {
            _gameUi.Pop();
        }

        private void SyncThemeToggleState()
        {
            if (_themeToggle == null)
                return;

            _themeToggle.SetValueWithoutNotify(DesignSystemThemeService.EffectiveIsLightTheme);
            _themeToggle.SetEnabled(!DesignSystemThemeService.IsCodigrateActive);
        }

        private void WireThemeProvider()
        {
            if (_themeProviderDropdown == null)
                return;

            _themeProviderDropdown.choices = new List<string> { DesignSystemThemeService.DefaultProvider };
            SyncThemeProviderDropdownSelection();

            var fetchGeneration = ++_codigrateFetchGeneration;
            CodigrateThemeProvider.FetchList((list, error) =>
            {
                if (fetchGeneration != _codigrateFetchGeneration)
                    return;

                if (error != null || list == null)
                {
                    Debug.LogWarning($"[SettingsOverlayController] Codigrate list load failed: {error}");
                    return;
                }

                var choices = new List<string> { DesignSystemThemeService.DefaultProvider };
                foreach (var listing in list)
                    choices.Add(listing.Name);

                _themeProviderDropdown.choices = choices;
                SyncThemeProviderDropdownSelection();
                SyncThemeToggleState();
            });

            _themeProviderDropdown.RegisterValueChangedCallback(OnThemeProviderChanged);
        }

        private void OnThemeProviderChanged(ChangeEvent<string> evt)
        {
            if (_suppressPersist)
                return;

            DesignSystemThemeService.SetThemeProvider(evt.newValue);
            SyncThemeToggleState();
        }

        private void LoadFromPlayerPrefs()
        {
            if (_masterSlider == null)
                return;

            _suppressPersist = true;
            _masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(PlayerPrefsKeys.MasterVolume, 1f));
            _musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(PlayerPrefsKeys.MusicVolume, 1f));
            _effectsSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(PlayerPrefsKeys.EffectsVolume, 1f));
            SyncThemeProviderDropdownSelection();
            SyncThemeToggleState();
            _suppressPersist = false;
        }

        private void SyncThemeProviderDropdownSelection()
        {
            if (_themeProviderDropdown == null)
                return;

            var savedProvider = DesignSystemThemeService.ThemeProviderName;
            if (_themeProviderDropdown.choices == null ||
                _themeProviderDropdown.choices.Count == 0 ||
                !_themeProviderDropdown.choices.Contains(savedProvider))
            {
                _themeProviderDropdown.SetValueWithoutNotify(DesignSystemThemeService.DefaultProvider);
                return;
            }

            _themeProviderDropdown.SetValueWithoutNotify(savedProvider);
        }

        private void OnThemeToggleChanged(ChangeEvent<bool> evt)
        {
            if (_suppressPersist)
                return;

            DesignSystemThemeService.SetLightTheme(evt.newValue);
            SyncThemeToggleState();
        }

        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistFloat01(PlayerPrefsKeys.MasterVolume, evt.newValue);
        }

        private void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistFloat01(PlayerPrefsKeys.MusicVolume, evt.newValue);
        }

        private void OnEffectsVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistFloat01(PlayerPrefsKeys.EffectsVolume, evt.newValue);
        }

        private static void PersistFloat01(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}