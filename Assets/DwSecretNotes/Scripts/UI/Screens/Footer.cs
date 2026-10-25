using System.Collections.Generic;
using DwSecretNotes.Business;
using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public static class Footer
    {
        static readonly List<VisualElement> rateHosts = new List<VisualElement>();
        public static VisualElement Build(AppShell shell)
        {
            var f = Kit.Col("dw-footer");
            f.Add(Kit.Link(L10n.T("footer_website"), () => Native.I.OpenUrl(AppInfo.BaseUrl)));
            f.Add(Kit.Text(L10n.T("footer_copyright"), "dw-caption", p => p.OnSurfaceVariant));
            f.Add(Kit.Spacer(8));
            var rate = Kit.Col("dw-footer__rate"); rate.userData = shell.Preferences; f.Add(rate); rateHosts.Add(rate); Fill(rate, shell.Preferences);
            return f;
        }
        static void Fill(VisualElement rate, IAppPreferences preferences)
        {
            rate.Clear();
            if (preferences.HasRatedApp)
            {
                rate.Add(Kit.Text(L10n.T("footer_rate_thanks"), "dw-caption", p => p.OnSurfaceVariant));
                var disabledStars = Kit.Row(); disabledStars.AddToClassList("dw-stars"); disabledStars.AddToClassList("disabled");
                for (int i = 0; i < 5; i++)
                {
                    var star = Kit.Icon(IconKind.Star, 30, p => ThemeCatalog.Gold); star.AddToClassList("dw-star"); star.tooltip = L10n.T("footer_rate_stars", i + 1); star.pickingMode = PickingMode.Ignore; disabledStars.Add(star);
                }
                rate.Add(disabledStars); return;
            }
            rate.Add(Kit.Text(L10n.T("footer_rate_prompt"), "dw-caption", p => p.OnSurfaceVariant));
            var stars = Kit.Row(); stars.AddToClassList("dw-stars");
            for (int i = 0; i < 5; i++)
            {
                var s = Kit.Icon(IconKind.Star, 30, p => ThemeCatalog.Gold); s.pickingMode = PickingMode.Position; s.AddToClassList("dw-star");
                s.tooltip = L10n.T("footer_rate_stars", i + 1); s.viewDataKey = "star-rate-" + (i + 1);
                Kit.Action(s, () => { Native.I.Haptic(Haptic.LongPress); Native.I.RequestReview(() => { preferences.HasRatedApp = true; RefreshAll(); }); });
                stars.Add(s);
            }
            rate.Add(stars);
        }
        public static void RefreshAll()
        {
            rateHosts.RemoveAll(h => h.panel == null);
            foreach (var host in rateHosts) if (host.userData is IAppPreferences preferences) Fill(host, preferences);
        }
    }
}
