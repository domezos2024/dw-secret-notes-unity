using System;
using System.Text.RegularExpressions;
namespace DwSecretNotes.Core
{
    public readonly struct ParsedLink
    {
        public readonly string Alias, Pass;
        public ParsedLink(string alias, string pass) { Alias = alias; Pass = pass ?? ""; }
    }
    public static class LinkParser
    {
        static readonly Regex UrlRx = new Regex(@"(https?://\S+)"), AliasRx = new Regex(@"\b(\d{2}\.\d{2}\.\d{4}_\d{2}-\d{2}-\d{2}-\d{3}|[A-Za-z0-9]{5})\b");
        public static string Query(string url, string key)
        {
            int q = url.IndexOf('?'); if (q < 0) return null;
            var qs = url.Substring(q + 1); int h = qs.IndexOf('#'); if (h >= 0) qs = qs.Substring(0, h);
            foreach (var part in qs.Split('&'))
            {
                int e = part.IndexOf('='); var k = e < 0 ? part : part.Substring(0, e);
                if (Uri.UnescapeDataString(k.Replace('+', ' ')) == key) return e < 0 ? "" : Uri.UnescapeDataString(part.Substring(e + 1).Replace('+', ' '));
            }
            return null;
        }
        public static string Host(string url) { try { return new Uri(url).Host; } catch { return null; } }
        public static string LastPathSegment(string url)
        {
            try { var segs = new Uri(url).AbsolutePath.Trim('/').Split('/'); var last = segs[segs.Length - 1]; return last.Length == 0 ? null : Uri.UnescapeDataString(last); } catch { return null; }
        }
        public static ParsedLink ParseUri(string url) => new ParsedLink(Query(url, "com") ?? LastPathSegment(url) ?? "", Query(url, "pass") ?? "");
        public static string AliasFromDeepLink(string url)
        {
            if (!string.Equals(Host(url), AppInfo.Host, StringComparison.OrdinalIgnoreCase)) return null;
            var com = Query(url, "com"); if (com == null) return null;
            var pass = Query(url, "pass");
            return string.IsNullOrEmpty(pass) ? com : com + "|" + pass;
        }
        public static string AliasFromShareText(string text)
        {
            var m = UrlRx.Match(text ?? "");
            if (m.Success) return AliasFromDeepLink(m.Value) ?? m.Value;
            var a = AliasRx.Match(text ?? "");
            return a.Success ? a.Value : text;
        }
        public static string FormatGeneratedAlias(string result)
        {
            if (result.Contains("com=")) { var s = After(result, "com="); s = Before(s, "&"); return (s.Length > 12 ? s.Substring(0, 12) : s) + "..."; }
            if (result.Contains("link=")) { var s = After(result, "link="); return s.Length > 8 ? s.Substring(0, 8) : s; }
            return "Link Ready";
        }
        public static ParsedLink? ParseLocal(string input, out string normalizedUrl)
        {
            var cur = (input ?? "").Trim(); normalizedUrl = null;
            if (cur.Length == 5 && !cur.Contains("://") && !cur.Contains(".")) return new ParsedLink(cur, "");
            if (!cur.StartsWith("http") && cur.Contains(AppInfo.Host)) cur = "https://" + cur;
            if (!cur.StartsWith("http")) return cur.Length >= 5 && cur.Length <= 100 ? new ParsedLink(cur, "") : (ParsedLink?)null;
            normalizedUrl = cur;
            var pass = Query(cur, "pass") ?? (cur.Contains("pass=") ? Before(After(cur, "pass="), "&") : "");
            var alias = Query(cur, "com") ?? (cur.Contains("com=") ? Before(After(cur, "com="), "&") : null);
            if (alias != null && alias.Length >= 5) return new ParsedLink(alias, pass);
            return null;
        }
        public static string After(string s, string d) { int i = s.IndexOf(d, StringComparison.Ordinal); return i < 0 ? s : s.Substring(i + d.Length); }
        public static string Before(string s, string d) { int i = s.IndexOf(d, StringComparison.Ordinal); return i < 0 ? s : s.Substring(0, i); }
    }
}
