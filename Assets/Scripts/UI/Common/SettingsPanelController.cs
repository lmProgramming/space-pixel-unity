using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Common
{
    public class SettingsPanelController
    {
        private const string MasterVolumeKey = "masterVolume";
        private const string MusicVolumeKey = "musicVolume";
        private const string EffectsVolumeKey = "effectVolume";

        private readonly VisualElement _backdrop;
        private readonly Slider _effectsSlider;
        private readonly bool _isMainMenu;
        private readonly Slider _masterSlider;
        private readonly Slider _musicSlider;

        private bool _suppressPersist;

        public SettingsPanelController(VisualElement parent, bool isMainMenu, string title = "Settings")
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            _isMainMenu = isMainMenu;
            _backdrop = parent.Q<VisualElement>("settings-overlay");
            var titleLabel = parent.Q<Label>("settings-title");
            _masterSlider = parent.Q<Slider>("settings-master-slider");
            _musicSlider = parent.Q<Slider>("settings-music-slider");
            _effectsSlider = parent.Q<Slider>("settings-effects-slider");
            var closeButton = parent.Q<Button>("settings-close-button");

            if (titleLabel == null || _masterSlider == null || _musicSlider == null || _effectsSlider == null ||
                closeButton == null || _backdrop == null)
                throw new InvalidOperationException(
                    "[SoundSettingsPanelController] Required settings elements are missing in UIDocument.");

            titleLabel.text = _isMainMenu ? $"{title} (Main Menu)" : title;
            _masterSlider.showInputField = true;
            _musicSlider.showInputField = true;
            _effectsSlider.showInputField = true;
            closeButton.clicked += Hide;
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
            _backdrop.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _backdrop.style.display = DisplayStyle.None;
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
            _masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            _musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            _effectsSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(EffectsVolumeKey, 1f));
            _suppressPersist = false;
        }

        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistVolume(MasterVolumeKey, evt.newValue);
        }

        private void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistVolume(MusicVolumeKey, evt.newValue);
        }

        private void OnEffectsVolumeChanged(ChangeEvent<float> evt)
        {
            if (_suppressPersist)
                return;

            PersistVolume(EffectsVolumeKey, evt.newValue);
        }

        private static void PersistVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
}