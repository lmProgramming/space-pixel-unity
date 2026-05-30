using System;
using Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Common
{
    public class SettingsPanelController
    {
        private readonly VisualElement _backdrop;
        private readonly Slider _effectsSlider;
        private readonly Slider _masterSlider;
        private readonly Slider _musicSlider;
        private readonly VisualElement _overlayHost;

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
            var closeButton = parent.Q<Button>(SharedUiElementNames.Settings.CloseButton);

            if (titleLabel == null || _masterSlider == null || _musicSlider == null || _effectsSlider == null ||
                closeButton == null || _backdrop == null)
                throw new InvalidOperationException(
                    "[SettingsPanelController] Required settings elements are missing in UIDocument.");

            titleLabel.text = isMainMenu ? $"{title} (Main Menu)" : title;
            closeButton.clicked += Hide;
            if (_overlayHost != null)
                _overlayHost.style.display = DisplayStyle.None;
            _backdrop.style.display = DisplayStyle.None;

            _masterSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
            _musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
            _effectsSlider.RegisterValueChangedCallback(OnEffectsVolumeChanged);

            LoadFromPlayerPrefs();
        }

        public bool IsOpen => _backdrop.style.display == DisplayStyle.Flex;

        private void Show()
        {
            LoadFromPlayerPrefs();
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
            _suppressPersist = false;
        }

        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistVolume(PlayerPrefsKeys.MasterVolume, evt.newValue);
        }

        private void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistVolume(PlayerPrefsKeys.MusicVolume, evt.newValue);
        }

        private void OnEffectsVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistVolume(PlayerPrefsKeys.EffectsVolume, evt.newValue);
        }

        private static void PersistVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}