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
        static readonly string[][] Families =
        {
            new[] { "Roboto", "Regular" }, new[] { "Segoe UI", "Regular" }, new[] { "Noto Sans", "Regular" },
            new[] { "Noto Sans CJK JP", "Regular" }, new[] { "Noto Sans CJK SC", "Regular" }, new[] { "Microsoft YaHei", "Regular" }, new[] { "Yu Gothic", "Regular" }, new[] { "Malgun Gothic", "Regular" },
            new[] { "Noto Naskh Arabic", "Regular" }, new[] { "Noto Sans Arabic", "Regular" }, new[] { "Noto Nastaliq Urdu", "Regular" }, new[] { "Arial", "Regular" },
            new[] { "Noto Sans Devanagari", "Regular" }, new[] { "Noto Sans Bengali", "Regular" }, new[] { "Nirmala UI", "Regular" },
            new[] { "Noto Sans Symbols", "Regular" }, new[] { "Noto Sans Symbols 2", "Regular" }, new[] { "Segoe UI Symbol", "Regular" }
        };
        static FontAsset primary;
        static bool done;
        public static void Apply(VisualElement root, PanelSettings ps)
        {
            if (!done)
            {
                done = true;
                var list = new List<FontAsset>();
                foreach (var f in Families)
                {
                    try
                    {
                        var fa = FontAsset.CreateFontAsset(f[0], f[1], 90);
                        if (fa != null) { list.Add(fa); DwLog.I("Fonts", "loaded " + f[0]); }
                    }
                    catch (Exception e) { DwLog.I("Fonts", "skip " + f[0] + ": " + e.Message); }
                }
                if (list.Count > 0)
                {
                    primary = list[0];
                    primary.fallbackFontAssetTable ??= new List<FontAsset>();
                    for (int i = 1; i < list.Count; i++) primary.fallbackFontAssetTable.Add(list[i]);
                }
                DwLog.Var("Fonts", "count", list.Count);
            }
            if (primary != null) root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(primary));
        }
    }
}
