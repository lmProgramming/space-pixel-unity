using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesignSystem.Showcase.Runtime
{
    // Loads Codigrate theme metadata + palette JSON from bundled Resources only.
    public static class CodigrateThemeProvider
    {
        public const string ShowcaseURL = "https://codigrate.com";

        private const string BundledListRes = "CodigrateThemes/list";
        private const string BundledThemeDir = "CodigrateThemes/";

        private static readonly Dictionary<string, ThemePalette> CachedPalettes = new();

        public static List<ThemeListing> CachedList { get; private set; }

        public static void FetchList(Action<List<ThemeListing>, string> done)
        {
            try
            {
                if (CachedList == null)
                    CachedList = LoadBundledList();
                done?.Invoke(CachedList, null);
            }
            catch (Exception e)
            {
                done?.Invoke(null, e.Message);
            }
        }

        public static void FetchPalette(ThemeListing listing, Action<ThemePalette, string> done)
        {
            if (listing == null)
            {
                done?.Invoke(null, "listing was null");
                return;
            }

            try
            {
                var cacheKey = listing.PaletteResource ?? listing.Name;
                if (cacheKey != null && CachedPalettes.TryGetValue(cacheKey, out var hit))
                {
                    done?.Invoke(hit, null);
                    return;
                }

                var bundled = LoadBundledPalette(listing);
                if (bundled == null)
                {
                    done?.Invoke(null, "bundled palette missing: " + listing.PaletteResource);
                    return;
                }

                if (cacheKey != null)
                    CachedPalettes[cacheKey] = bundled;
                done?.Invoke(bundled, null);
            }
            catch (Exception e)
            {
                done?.Invoke(null, e.Message);
            }
        }

        private static List<ThemeListing> LoadBundledList()
        {
            var ta = Resources.Load<TextAsset>(BundledListRes);
            if (ta == null)
            {
                Debug.LogWarning($"[CodigrateThemeProvider] Bundled list missing at Resources/{BundledListRes}");
                return new List<ThemeListing>();
            }

            return ParseList(ta.text);
        }

        private static ThemePalette LoadBundledPalette(ThemeListing listing)
        {
            if (string.IsNullOrEmpty(listing.PaletteResource))
                return null;

            var ta = Resources.Load<TextAsset>(listing.PaletteResource);
            if (ta == null)
                return null;

            return ParsePalette(ta.text);
        }

        private static List<ThemeListing> ParseList(string raw)
        {
            var wrapped = "{\"items\":" + raw + "}";
            var dto = JsonUtility.FromJson<ListWrapper>(wrapped);
            var result = new List<ThemeListing>();
            if (dto?.items == null)
                return result;

            foreach (var item in dto.items)
            {
                if (string.IsNullOrEmpty(item?.name) || string.IsNullOrEmpty(item.json))
                    continue;

                result.Add(new ThemeListing
                {
                    Name = item.name,
                    PaletteResource = BundledResourceFor(item.json)
                });
            }

            return result;
        }

        private static string BundledResourceFor(string upstreamJsonPath)
        {
            if (string.IsNullOrEmpty(upstreamJsonPath))
                return null;

            var lastSlash = upstreamJsonPath.LastIndexOf('/');
            var file = lastSlash >= 0 ? upstreamJsonPath.Substring(lastSlash + 1) : upstreamJsonPath;
            const string suffix = ".palette.json";
            if (file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                file = file.Substring(0, file.Length - suffix.Length);
            return BundledThemeDir + file;
        }

        private static ThemePalette ParsePalette(string raw)
        {
            var dto = JsonUtility.FromJson<PaletteDto>(raw);
            if (dto?.tokens?.@interface == null)
                throw new Exception("palette JSON missing tokens.interface");

            var t = dto.tokens.@interface;
            return new ThemePalette
            {
                Name = dto.metadata?.name,
                Key = dto.metadata?.key,
                Appearance = (dto.metadata?.appearance ?? "dark").ToLowerInvariant(),
                Interface = new InterfaceTokens
                {
                    Surface = ToColor(t.surface),
                    WindowBackground = ToColor(t.windowBackground),
                    AlternateBackground = ToColor(t.alternateBackground),
                    EditorBackground = ToColor(t.editorBackground),
                    AccentColor = ToColor(t.accentColor),
                    PrimaryForeground = ToColor(t.primaryForeground),
                    SecondaryForeground = ToColor(t.secondaryForeground),
                    Error = ToColor(t.error),
                    Warning = ToColor(t.warning),
                    WarningFocused = ToColor(t.warningFocused),
                    Info = ToColor(t.info),
                    Success = ToColor(t.success)
                }
            };
        }

        private static Color ToColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Color.magenta;

            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        public sealed class ThemeListing
        {
            public string Name;
            public string PaletteResource;
        }

        public sealed class ThemePalette
        {
            public string Appearance;
            public InterfaceTokens Interface;
            public string Key;
            public string Name;
        }

        public sealed class InterfaceTokens
        {
            public Color AccentColor;
            public Color AlternateBackground;
            public Color EditorBackground;
            public Color Error;
            public Color Info;
            public Color PrimaryForeground;
            public Color SecondaryForeground;
            public Color Success;
            public Color Surface;
            public Color Warning;
            public Color WarningFocused;
            public Color WindowBackground;
        }

        [Serializable]
        private class ListWrapper
        {
            public List<ListItemDto> items;
        }

        [Serializable]
        private class ListItemDto
        {
            public string name;
            public string icon;
            public string html;
            public string json;
        }

        [Serializable]
        private class PaletteDto
        {
            public string version;
            public MetadataDto metadata;
            public TokensDto tokens;
        }

        [Serializable]
        private class MetadataDto
        {
            public string id;
            public string name;
            public string key;
            public string category;
            public string appearance;
        }

        [Serializable]
        private class TokensDto
        {
            public InterfaceDto @interface;
        }

        [Serializable]
        private class InterfaceDto
        {
            public string surface;
            public string windowBackground;
            public string alternateBackground;
            public string editorBackground;
            public string accentColor;
            public string primaryForeground;
            public string secondaryForeground;
            public string error;
            public string warning;
            public string warningFocused;
            public string info;
            public string success;
        }
    }
}
