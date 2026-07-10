using System;
using System.Collections.Generic;
using Core.Constants;
using DesignSystem.Runtime;
using DesignSystem.Showcase.Runtime;
using UI.Tools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Common
{
    public class SettingsPanelController
    {
        private readonly VisualElement _backdrop;

        private readonly Button _closeButton;
        private readonly Slider _effectsSlider;
        private readonly Slider _masterSlider;
        private readonly Slider _musicSlider;
        private readonly VisualElement _overlayHost;
        private readonly DropdownField _themeProviderDropdown;
        private readonly Toggle _themeToggle;
        private int _codigrateFetchGeneration;
        private bool _isBound;
        private bool _suppressPersist;

        public SettingsPanelController(VisualElement parent, bool isMainMenu, string title = "Settings")
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            _overlayHost = parent.Q<VisualElement>(SharedUiElementNames.Settings.OverlayHost);
            _backdrop = parent.Q<VisualElement>(SharedUiElementNames.Settings.Overlay);
            var titleLabel = parent.Q<Label>(SharedUiElementNames.Settings.Title);
            _masterSlider = parent.Q<Slider>(SharedUiElementNames.Settings.MasterSlider);
            _musicSlider = parent.Q<Slider>(SharedUiElementNames.Settings.MusicSlider);
            _effectsSlider = parent.Q<Slider>(SharedUiElementNames.Settings.EffectsSlider);
            _closeButton = parent.Q<Button>(SharedUiElementNames.Settings.CloseButton);
            _themeToggle = parent.Q<Toggle>("theme-toggle");
            _themeProviderDropdown = parent.Q<DropdownField>("theme-provider-dropdown");

            if (titleLabel == null || _masterSlider == null || _musicSlider == null || _effectsSlider == null ||
                _closeButton == null || _backdrop == null)
                throw new InvalidOperationException(
                    "[SettingsPanelController] Required settings elements are missing in UIDocument.");

            titleLabel.text = isMainMenu ? $"{title} (Main Menu)" : title;
            Bind(parent);
        }

        public bool IsOpen => _backdrop.style.display == DisplayStyle.Flex;

        public void Unbind()
        {
            if (!_isBound)
                return;

            _codigrateFetchGeneration++;
            _closeButton.clicked -= Hide;
            _masterSlider.UnregisterValueChangedCallback(OnMasterVolumeChanged);
            _musicSlider.UnregisterValueChangedCallback(OnMusicVolumeChanged);
            _effectsSlider.UnregisterValueChangedCallback(OnEffectsVolumeChanged);
            _themeToggle?.UnregisterValueChangedCallback(OnThemeToggleChanged);
            _themeProviderDropdown?.UnregisterValueChangedCallback(OnThemeProviderChanged);
            Hide();
            _isBound = false;
        }

        private void Bind(VisualElement parent)
        {
            if (_isBound)
                return;

            DesignSystemThemeService.RegisterVisualTree(parent);
            DesignSystemRuntime.EnsureToggleKnobs(parent);

            if (_overlayHost != null)
                _overlayHost.style.display = DisplayStyle.None;
            _backdrop.style.display = DisplayStyle.None;

            _closeButton.clicked += Hide;
            _masterSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
            _musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
            _effectsSlider.RegisterValueChangedCallback(OnEffectsVolumeChanged);
            WireThemeToggle();
            WireThemeProvider();
            LoadFromPlayerPrefs();
            _isBound = true;
        }

        private void SyncThemeToggleState()
        {
            if (_themeToggle == null)
                return;

            _themeToggle.SetValueWithoutNotify(DesignSystemThemeService.EffectiveIsLightTheme);
            _themeToggle.SetEnabled(!DesignSystemThemeService.IsCodigrateActive);
        }

        private void WireThemeToggle()
        {
            _themeToggle?.RegisterValueChangedCallback(OnThemeToggleChanged);
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
                    Debug.LogWarning($"[SettingsPanelController] Codigrate list load failed: {error}");
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

        private void Show()
        {
            LoadFromPlayerPrefs();
            DesignSystemRuntime.EnsureToggleKnobs(_backdrop);
            if (_overlayHost != null)
                _overlayHost.style.display = DisplayStyle.Flex;
            _backdrop.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _backdrop.style.display = DisplayStyle.None;
            if (_overlayHost != null)
                _overlayHost.style.display = DisplayStyle.None;
        }

        public void Toggle()
        {
            if (IsOpen)
                Hide();
            else
                Show();
        }

        private void LoadFromPlayerPrefs()
        {
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