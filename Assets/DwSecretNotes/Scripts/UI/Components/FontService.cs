using System;
using System.Collections.Generic;
using DwSecretNotes.Core;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public static class FontService
    {
        static FontAsset primary, arabic;
        static readonly Dictionary<string, FontAsset> fallbackCache = new Dictionary<string, FontAsset>();
        public static void Apply(VisualElement root, FontAsset primaryAsset, FontAsset arabicAsset, string languageTag)
        {
            primary = primaryAsset;
            arabic = arabicAsset;
            ApplyLanguage(root, languageTag);
        }
        public static void ApplyLanguage(VisualElement root, string languageTag)
        {
            if (primary == null)
            {
                DwLog.W("Fonts", "Noto Sans font asset is not assigned to AppBootstrap");
                return;
            }
            primary.fallbackFontAssetTable = new List<FontAsset>();
            if (arabic != null) primary.fallbackFontAssetTable.Add(arabic);
            foreach (var family in FallbackFamilies(languageTag))
            {
                try
                {
                    if (!fallbackCache.TryGetValue(family, out var fallback))
                    {
                        fallback = FontAsset.CreateFontAsset(family, "Regular", 90);
                        if (fallback != null) fallbackCache.Add(family, fallback);
                    }
                    if (fallback == null) { DwLog.I("Fonts", "system fallback unavailable: " + family); continue; }
                    fallback.isMultiAtlasTexturesEnabled = true;
                    primary.fallbackFontAssetTable.Add(fallback);
                }
                catch (Exception e) { DwLog.I("Fonts", "skip system fallback " + family + ": " + e.Message); }
            }
            root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(primary));
        }
        static string[] FallbackFamilies(string languageTag)
        {
            switch (languageTag)
            {
                case "bn": return new[] { "Noto Sans Bengali" };
                case "hi": return new[] { "Noto Sans Devanagari", "Nirmala UI" };
                case "ja": return new[] { "Noto Sans CJK JP", "Yu Gothic" };
                case "ko": return new[] { "Malgun Gothic" };
                case "ur": return new[] { "Noto Nastaliq Urdu", "Noto Naskh Arabic" };
                case "zh-CN": return new[] { "Noto Sans CJK SC", "Microsoft YaHei" };
                default: return Array.Empty<string>();
            }
        }
    }
}
