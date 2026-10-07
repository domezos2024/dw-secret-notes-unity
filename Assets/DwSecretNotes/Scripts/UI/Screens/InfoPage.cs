using DwSecretNotes.Core;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public sealed class InfoPage : Page
    {
        public override string Route => Routes.Info;
        protected override void Build(VisualElement root)
        {
            var c = Scroll(root).contentContainer;
            var head = Kit.Col("dw-infohead");
            var logo = new VisualElement(); logo.AddToClassList("dw-welcome__logo"); logo.style.backgroundImage = new StyleBackground(UnityEngine.Resources.Load<UnityEngine.Texture2D>("UI/app_icon")); head.Add(logo);
            head.Add(Kit.Text(L10n.T("app_name"), "dw-h1", p => p.OnSurface));
            head.Add(Kit.Text("v" + AppInfo.Version, "dw-small", p => p.OnSurfaceVariant));
            c.Add(head); c.Add(Kit.Spacer(16));
            c.Add(Kit.Text(L10n.T("info_title"), "dw-h2", p => p.OnSurface)); c.Add(Kit.Spacer(10));
            var card = Kit.Card();
            Row(card, "info_version_label", AppInfo.Version, true); Row(card, "info_developer_label", AppInfo.Developer, true);
            Row(card, "info_website_label", AppInfo.Host, true); Row(card, "info_license_label", AppInfo.License, false);
            c.Add(card); c.Add(Kit.Spacer(12));
            var crypto = Kit.Card(); var h = Kit.Row(); h.AddToClassList("dw-gap-row");
            h.Add(Kit.Icon(IconKind.Shield, 22, p => p.Primary)); h.Add(Kit.Text("AES-256-GCM · PBKDF2-SHA256", "dw-title", p => p.Primary));
            crypto.Add(h); crypto.Add(Kit.Spacer(8)); crypto.Add(Kit.Text(L10n.T("info_crypto_note"), "dw-body", p => ThemeBinder.A(p.OnSurface, 0.85f)));
            c.Add(crypto); c.Add(Kit.Spacer(16)); c.Add(Footer.Build(Shell));
        }
        static void Row(VisualElement card, string key, string value, bool divider)
        {
            var r = Kit.Row(); r.AddToClassList("dw-inforow");
            var l = Kit.Text(L10n.T(key), "dw-body", p => p.OnSurfaceVariant); l.style.flexGrow = 1; r.Add(l);
            r.Add(Kit.Text(value, "dw-body dw-bold", p => p.OnSurface)); card.Add(r);
            if (divider) { var d = new VisualElement(); d.AddToClassList("dw-divider"); ThemeBinder.On(d, (e, p) => e.style.backgroundColor = ThemeBinder.A(p.Outline, 0.3f)); card.Add(d); }
        }
    }
}
