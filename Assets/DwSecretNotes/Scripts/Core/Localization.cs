using System;
using System.Collections.Generic;
using UnityEngine;
namespace DwSecretNotes.Core
{
    public sealed class LanguageOption
    {
        public readonly string Tag, NativeName, EnglishName;
        public LanguageOption(string tag, string nativeName, string englishName) { Tag = tag; NativeName = nativeName; EnglishName = englishName; }
    }
    public sealed class StringTable
    {
        [Serializable] class Raw { public string[] keys; public string[] values; }
        readonly Dictionary<string, string> map = new Dictionary<string, string>();
        readonly StringTable fallback;
        public readonly string Tag;
        StringTable(string tag, StringTable fallback) { Tag = tag; this.fallback = fallback; }
        public static StringTable Load(string tag, StringTable fallback)
        {
            var t = new StringTable(tag, fallback);
            var ta = Resources.Load<TextAsset>("i18n/" + tag);
            if (ta == null) { DwLog.W("L10n", "missing table " + tag); return t; }
            var raw = JsonUtility.FromJson<Raw>(ta.text);
            for (int i = 0; i < raw.keys.Length && i < raw.values.Length; i++) t.map[raw.keys[i]] = raw.values[i];
            return t;
        }
        public string Get(string key) => map.TryGetValue(key, out var v) ? v : fallback != null ? fallback.Get(key) : key;
        public bool Has(string key) => map.ContainsKey(key);
    }
    public static class L10n
    {
        public static readonly LanguageOption[] Languages =
        {
            new LanguageOption("en", "English", "English"), new LanguageOption("de", "Deutsch", "German"),
            new LanguageOption("es", "Español", "Spanish"), new LanguageOption("zh-CN", "中文", "Chinese (Simplified)"),
            new LanguageOption("hi", "हिन्दी", "Hindi"), new LanguageOption("ar", "العربية", "Arabic"),
            new LanguageOption("pt", "Português", "Portuguese"), new LanguageOption("bn", "বাংলা", "Bengali"),
            new LanguageOption("ru", "Русский", "Russian"), new LanguageOption("ja", "日本語", "Japanese"),
            new LanguageOption("fr", "Français", "French"), new LanguageOption("ur", "اردو", "Urdu"),
            new LanguageOption("id", "Indonesia", "Indonesian"), new LanguageOption("ko", "한국어", "Korean"),
            new LanguageOption("it", "Italiano", "Italian")
        };
        static StringTable english, current;
        public static event Action Changed;
        public static string CurrentTag => current?.Tag ?? "en";
        public static bool IsRtl => IsRtlTag(CurrentTag);
        public static bool IsRtlTag(string tag) => tag == "ar" || tag == "ur";
        public static StringTable English => english ??= StringTable.Load("en", null);
        public static StringTable TableFor(string tag) => tag == "en" ? English : StringTable.Load(tag, English);
        public static void Init(string tag)
        {
            current = TableFor(Normalize(tag));
            DwLog.Var("L10n", "language", current.Tag);
            Changed?.Invoke();
        }
        public static string Normalize(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return "en";
            if (tag == "in") return "id";
            foreach (var l in Languages) if (l.Tag == tag) return tag;
            var two = tag.Split('-', '_')[0];
            if (two == "zh") return "zh-CN";
            foreach (var l in Languages) if (l.Tag == two) return two;
            return "en";
        }
        public static string DeviceLanguageTag()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.German: return "de"; case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Chinese: case SystemLanguage.ChineseSimplified: case SystemLanguage.ChineseTraditional: return "zh-CN";
                case SystemLanguage.Hindi: return "hi"; case SystemLanguage.Arabic: return "ar"; case SystemLanguage.Portuguese: return "pt";
                case SystemLanguage.Russian: return "ru"; case SystemLanguage.Japanese: return "ja"; case SystemLanguage.French: return "fr";
                case SystemLanguage.Indonesian: return "id"; case SystemLanguage.Korean: return "ko"; case SystemLanguage.Italian: return "it";
                default: return "en";
            }
        }
        public static string T(string key) => (current ?? English).Get(key);
        public static string T(string key, params object[] args) { try { return string.Format(T(key), args); } catch (FormatException) { return T(key); } }
        public static string Plural(string key, int n) => Plural(current ?? English, key, n);
        public static string Plural(StringTable table, string key, int n)
        {
            var q = PluralCategory(table.Tag, n);
            var k = key + "_" + q;
            var fmt = table.Has(k) ? table.Get(k) : table.Get(key + "_other");
            try { return string.Format(fmt, n); } catch (FormatException) { return fmt; }
        }
        public static string PluralCategory(string tag, int n)
        {
            int m10 = n % 10, m100 = n % 100;
            switch (tag)
            {
                case "ja": case "ko": case "zh-CN": case "id": return "other";
                case "fr": case "pt": return n == 0 || n == 1 ? "one" : "other";
                case "hi": case "bn": return n == 0 || n == 1 ? "one" : "other";
                case "ru": return m10 == 1 && m100 != 11 ? "one" : m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14) ? "few" : "many";
                case "ar": return n == 0 ? "zero" : n == 1 ? "one" : n == 2 ? "two" : m100 >= 3 && m100 <= 10 ? "few" : m100 >= 11 ? "many" : "other";
                default: return n == 1 ? "one" : "other";
            }
        }
    }
}
