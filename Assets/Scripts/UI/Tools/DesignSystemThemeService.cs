using System;
using System.Collections.Generic;
using Core.Constants;
using DesignSystem.Runtime;
using DesignSystem.Showcase.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UI.Tools
{
    /// <summary>
    ///     Loads theme override stylesheets, restores theme prefs from
    ///     PlayerPrefs, and applies light/dark or Codigrate palettes to every
    ///     registered visual tree (<c>UIDocument</c> or <c>PanelRenderer</c>).
    /// </summary>
    public static class DesignSystemThemeService
    {
        public const string DefaultProvider = "Design System default";

        private const string ThemeLightClass = "theme-light";
        private const string DsRootClass = "ds-root";
        private const string ThemeSheetResource = "ShowcaseTheme";
        private const string PopupSheetResource = "ShowcaseDropdownPopup";

        private static readonly HashSet<VisualElement> TrackedVisualTreeRoots = new();

        private static StyleSheet _themeSheet;
        private static StyleSheet _popupSheet;
        private static bool _globalRestoreStarted;
        private static CodigrateThemeApplier.ColorMap _activeCodigrateMap;

        public static bool IsLightTheme => PlayerPrefs.GetInt(PlayerPrefsKeys.ThemeLight, 0) == 1;

        public static bool EffectiveIsLightTheme { get; private set; }

        public static string ThemeProviderName =>
            PlayerPrefs.GetString(PlayerPrefsKeys.ThemeProvider, DefaultProvider);

        public static bool IsCodigrateActive { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _globalRestoreStarted = false;
            IsCodigrateActive = false;
            _activeCodigrateMap = null;
            TrackedVisualTreeRoots.Clear();
            CodigrateThemeApplier.DiscardAllTracking();
            RefreshAll();
        }

        public static void ApplyToDocument(UIDocument doc)
        {
            if (doc == null)
                return;

            RegisterVisualTree(doc.rootVisualElement);
        }

        public static void RegisterVisualTree(VisualElement root)
        {
            if (root == null)
                return;

            TrackedVisualTreeRoots.Add(root);
            root.schedule.Execute(() =>
            {
                EnsureStylesheetsOnRoot(root);
                DesignSystemRuntime.EnsureToggleKnobs(root);

                if (!_globalRestoreStarted)
                    RestoreGlobalThemeIfNeeded();
                else
                    ApplyCurrentThemeToTree(root);
            }).StartingIn(0);
        }

        public static void RefreshAll()
        {
            var docs = Object.FindObjectsByType<UIDocument>();
            foreach (var doc in docs)
                RegisterVisualTree(doc.rootVisualElement);
        }

        public static void SetLightTheme(bool light)
        {
            if (IsCodigrateActive)
                return;

            PlayerPrefs.SetInt(PlayerPrefsKeys.ThemeLight, light ? 1 : 0);
            PlayerPrefs.Save();
            ApplyLightClassEverywhere(light);
        }

        public static void SetThemeProvider(string providerName)
        {
            var name = string.IsNullOrEmpty(providerName) ? DefaultProvider : providerName;
            PlayerPrefs.SetString(PlayerPrefsKeys.ThemeProvider, name);
            PlayerPrefs.Save();

            if (string.Equals(name, DefaultProvider, StringComparison.Ordinal))
            {
                IsCodigrateActive = false;
                _activeCodigrateMap = null;
                CodigrateThemeApplier.RevertAll();
                ApplyLightClassEverywhere(IsLightTheme);
                return;
            }

            FetchAndApplyCodigrateProvider(name);
        }

        private static void RestoreGlobalThemeIfNeeded()
        {
            if (_globalRestoreStarted)
                return;

            _globalRestoreStarted = true;

            if (!HasSavedCodigrateProvider())
            {
                ApplyLightClassEverywhere(IsLightTheme);
                return;
            }

            FetchAndApplyCodigrateProvider(ThemeProviderName);
        }

        private static bool HasSavedCodigrateProvider()
        {
            return !string.Equals(ThemeProviderName, DefaultProvider, StringComparison.Ordinal);
        }

        private static void FetchAndApplyCodigrateProvider(string providerName)
        {
            CodigrateThemeProvider.FetchList((list, error) =>
            {
                if (error != null || list == null)
                {
                    Debug.LogWarning($"[DesignSystemThemeService] Codigrate list load failed: {error}");
                    IsCodigrateActive = false;
                    _activeCodigrateMap = null;
                    ApplyLightClassEverywhere(IsLightTheme);
                    return;
                }

                var listing = list.Find(l => string.Equals(l.Name, providerName, StringComparison.Ordinal));
                if (listing == null)
                {
                    Debug.LogWarning(
                        $"[DesignSystemThemeService] Saved theme provider '{providerName}' was not found.");
                    IsCodigrateActive = false;
                    _activeCodigrateMap = null;
                    ApplyLightClassEverywhere(IsLightTheme);
                    return;
                }

                CodigrateThemeProvider.FetchPalette(listing, (palette, paletteError) =>
                {
                    if (paletteError != null || palette == null)
                    {
                        Debug.LogWarning(
                            $"[DesignSystemThemeService] Codigrate palette load failed for {providerName}: {paletteError}");
                        IsCodigrateActive = false;
                        _activeCodigrateMap = null;
                        ApplyLightClassEverywhere(IsLightTheme);
                        return;
                    }

                    ApplyCodigratePalette(palette);
                });
            });
        }

        private static void ApplyCodigratePalette(CodigrateThemeProvider.ThemePalette palette)
        {
            var map = CodigrateThemeApplier.FromCodigrate(palette);
            _activeCodigrateMap = map;
            IsCodigrateActive = true;

            var isLight = string.Equals(palette.Appearance, "light", StringComparison.OrdinalIgnoreCase);
            ApplyLightClassEverywhere(isLight);
            CodigrateThemeApplier.ApplyToAll(CollectAllThemeRoots(), map);
        }

        private static void ApplyCurrentThemeToTree(VisualElement documentRoot)
        {
            foreach (var themeRoot in CollectThemeRootsInTree(documentRoot))
                SetLightClass(themeRoot, EffectiveIsLightTheme);

            var panelRoot = documentRoot.panel?.visualTree;
            if (panelRoot != null && panelRoot != documentRoot)
                SetLightClass(panelRoot, EffectiveIsLightTheme);

            if (IsCodigrateActive && _activeCodigrateMap != null)
                foreach (var themeRoot in CollectThemeRootsInTree(documentRoot))
                    CodigrateThemeApplier.ApplyWithoutRevert(themeRoot, _activeCodigrateMap);
        }

        private static void EnsureStylesheetsOnRoot(VisualElement root)
        {
            _themeSheet ??= Resources.Load<StyleSheet>(ThemeSheetResource);
            if (_themeSheet == null)
            {
                Debug.LogError(
                    "[DesignSystemThemeService] Could not load ShowcaseTheme from Resources. " +
                    "Theme toggling will have no effect.");
                return;
            }

            if (!root.styleSheets.Contains(_themeSheet))
                root.styleSheets.Add(_themeSheet);

            var panelRoot = root.panel?.visualTree ?? root.parent;
            if (panelRoot == null)
                return;

            _popupSheet ??= Resources.Load<StyleSheet>(PopupSheetResource);
            if (_popupSheet != null && !panelRoot.styleSheets.Contains(_popupSheet))
                panelRoot.styleSheets.Add(_popupSheet);
        }

        private static void ApplyLightClassEverywhere(bool light)
        {
            EffectiveIsLightTheme = light;
            foreach (var themeRoot in CollectAllThemeRoots())
                SetLightClass(themeRoot, light);

            foreach (var panelRoot in CollectPanelRoots())
                SetLightClass(panelRoot, light);
        }

        private static List<VisualElement> CollectAllThemeRoots()
        {
            TrackedVisualTreeRoots.RemoveWhere(root => root == null);

            var roots = new List<VisualElement>();
            foreach (var documentRoot in TrackedVisualTreeRoots)
            {
                if (documentRoot == null)
                    continue;

                AppendThemeRootsFromDocument(documentRoot, roots);
            }

            return roots;
        }

        private static List<VisualElement> CollectThemeRootsInTree(VisualElement documentRoot)
        {
            var roots = new List<VisualElement>();
            AppendThemeRootsFromDocument(documentRoot, roots);
            return roots;
        }

        private static void AppendThemeRootsFromDocument(VisualElement documentRoot, List<VisualElement> results)
        {
            var foundInDocument = new List<VisualElement>();
            documentRoot.Query(className: DsRootClass).ForEach(foundInDocument.Add);
            if (foundInDocument.Count == 0)
                results.Add(documentRoot);
            else
                results.AddRange(foundInDocument);
        }

        private static IEnumerable<VisualElement> CollectPanelRoots()
        {
            var seen = new HashSet<VisualElement>();
            foreach (var root in TrackedVisualTreeRoots)
            {
                if (root == null)
                    continue;

                var panelRoot = root.panel?.visualTree;
                if (panelRoot != null && panelRoot != root && seen.Add(panelRoot))
                    yield return panelRoot;
            }
        }

        private static void SetLightClass(VisualElement element, bool light)
        {
            if (element == null)
                return;

            if (light)
                element.AddToClassList(ThemeLightClass);
            else
                element.RemoveFromClassList(ThemeLightClass);
        }
    }
}