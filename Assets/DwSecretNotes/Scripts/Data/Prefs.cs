using DwSecretNotes.Core;
using DwSecretNotes.Business;
using UnityEngine;
namespace DwSecretNotes.Data
{
    public sealed class PlayerPrefsRepository : IAppPreferences
    {
        const string KLang = "selected_language", KTheme = "selected_theme", KRated = "has_rated_app", KRuns = "run_count", KSeenRate = "has_seen_rate_prompt";
        public string Language { get { var v = PlayerPrefs.GetString(KLang, ""); return v.Length == 0 ? null : v; } set { PlayerPrefs.SetString(KLang, value); PlayerPrefs.Save(); DwLog.Var("Prefs", "language", value); } }
        public string Theme { get => PlayerPrefs.GetString(KTheme, "classic"); set { PlayerPrefs.SetString(KTheme, value); PlayerPrefs.Save(); DwLog.Var("Prefs", "theme", value); } }
        public bool HasRatedApp { get => PlayerPrefs.GetInt(KRated, 0) == 1; set { PlayerPrefs.SetInt(KRated, value ? 1 : 0); PlayerPrefs.Save(); } }
        public bool HasSeenRatePrompt { get => PlayerPrefs.GetInt(KSeenRate, 0) == 1; set { PlayerPrefs.SetInt(KSeenRate, value ? 1 : 0); PlayerPrefs.Save(); } }
        int RunCount => PlayerPrefs.GetInt(KRuns, 0);
        public int IncrementRunCount() { int n = RunCount + 1; PlayerPrefs.SetInt(KRuns, n); PlayerPrefs.Save(); DwLog.Var("Prefs", "runCount", n); return n; }
    }
}
