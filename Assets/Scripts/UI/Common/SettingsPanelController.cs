using System;
using System.Collections.Generic;
using Core.Constants;
using DesignSystem.Showcase.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Common
{
    public class SettingsPanelController
    {
        private const string DefaultOption = "Design System default";
        private static List<CodigrateThemeProvider.ThemeListing> _codigrateListings;

        // Active third-party / generated palette, when set. While non-null the
        // day/night toggle is suppressed (codigrate carries its own appearance
        // signal; randomize honours the toggle's last value at generation time
        // but doesn't re-apply on subsequent toggle flips).
        private static CodigrateThemeApplier.ColorMap _activeOverride;
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
            WireThemeProvider(parent);
            WireThemeToggle(parent);

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

// Wire the day/night toggle in the COLORS section header. Adds /
        // removes the `theme-light` class on .ds-root; ShowcaseTheme.uss
        // redefines every colour token under that class, the universal
        // transition rule animates the swap across the whole tree, and the
        // hex labels in the COLORS section are rewritten to match.
        //
        // The class is ALSO applied to `panel.visualTree` because Unity's
        // BasePopupField adds the dropdown popup as a SIBLING of root,
        // under panel.visualTree. Without the class on that ancestor the
        // popup never sees the .theme-light token overrides and stays dark
        // while the rest of the showcase flips to light mode.
        //
        // While an override palette is active (codigrate / randomize) the
        // toggle is `SetEnabled(false)` by WireThemeProvider, so this handler
        // only fires for legitimate user-driven swaps between the two
        // first-party token sets.
        private static void WireThemeToggle(VisualElement root)
        {
            var toggle = root.Q<Toggle>("theme-toggle");
            toggle.RegisterValueChangedCallback(evt =>
            {
                var light = evt.newValue;
                ApplyThemeClass(root, light);
            });
        }

        private static void ApplyThemeClass(VisualElement root, bool light)
        {
            if (light) root.AddToClassList("theme-light");
            else root.RemoveFromClassList("theme-light");

            var panelRoot = root.panel?.visualTree;
            if (panelRoot != null && panelRoot != root)
            {
                if (light) panelRoot.AddToClassList("theme-light");
                else panelRoot.RemoveFromClassList("theme-light");
            }
        }

        private static void WireThemeProvider(VisualElement root)
        {
            if (root == null) return;
            var dropdown = root.Q<DropdownField>("theme-provider-dropdown");
            if (dropdown == null) return;
            var status = root.Q<Label>("theme-provider-status");

            // Default state: two stock entries until the network fetch returns.
            // Selecting "Random palette" works immediately; the codigrate
            // entries land in between once the list loads.
            dropdown.choices = new List<string> { DefaultOption };
            dropdown.index = 0;

            if (status != null) status.text = "Loading codigrate themes…";

            CodigrateThemeProvider.FetchList((list, error) =>
            {
                if (error != null || list == null)
                {
                    if (status != null) status.text = "Codigrate themes unavailable. Random palette still works.";
                    Debug.LogWarning($"[ShowcaseBootstrap] Codigrate list fetch failed: {error}");
                    return;
                }

                _codigrateListings = list;
                var choices = new List<string> { DefaultOption };
                foreach (var l in list) choices.Add(l.Name);
                dropdown.choices = choices;
                // Preserve current value across the choices swap. If the user
                // had Random selected at fetch time and we wiped it, the field
                // would render empty even though _activeOverride is still set.
                if (status != null) status.text = $"{list.Count} themes by Codigrate available.";
            });

            dropdown.RegisterValueChangedCallback(evt =>
            {
                var name = evt.newValue;
                if (name == DefaultOption)
                {
                    ClearOverride(root);
                    return;
                }

                if (_codigrateListings == null) return;
                var listing = _codigrateListings.Find(l => l.Name == name);
                if (listing == null) return;

                if (status != null) status.text = $"Loading {listing.Name}…";
                CodigrateThemeProvider.FetchPalette(listing, (palette, paletteError) =>
                {
                    if (paletteError != null || palette == null)
                    {
                        if (status != null) status.text = $"Failed to load {listing.Name}.";
                        Debug.LogWarning(
                            $"[ShowcaseBootstrap] Codigrate palette fetch failed for {listing.Name}: {paletteError}");
                        return;
                    }

                    ApplyCodigratePalette(root, palette);
                    if (status != null) status.text = $"{palette.Name} · {palette.Appearance}";
                });
            });
        }

        private static void ApplyCodigratePalette(VisualElement root, CodigrateThemeProvider.ThemePalette palette)
        {
            var map = CodigrateThemeApplier.FromCodigrate(palette);
            _activeOverride = map;

            // Mirror the palette's reported appearance onto the day/night
            // toggle so any leftover USS state (e.g. the `.theme-light`
            // re-routes for the notification dot) is consistent — then
            // disable the toggle while the override is active.
            var isLight = string.Equals(palette.Appearance, "light", StringComparison.OrdinalIgnoreCase);
            var toggle = root.Q<Toggle>("theme-toggle");
            if (toggle != null)
            {
                toggle.SetValueWithoutNotify(isLight);
                toggle.SetEnabled(false);
            }

            ApplyThemeClass(root, isLight);

            CodigrateThemeApplier.Apply(root, map);
            //UpdateHexLabels(root, isLight);
        }

        private static void ClearOverride(VisualElement root)
        {
            _activeOverride = null;
            CodigrateThemeApplier.Revert(root);

            var toggle = root.Q<Toggle>("theme-toggle");
            toggle?.SetEnabled(true);

            var status = root.Q<Label>("theme-provider-status");
            if (status != null && _codigrateListings != null)
                status.text = $"{_codigrateListings.Count} themes by Codigrate available.";
        }

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