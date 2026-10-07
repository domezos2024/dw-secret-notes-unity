using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public sealed class LanguagePage : Page
    {
        readonly bool firstRun; string selected; Label title, subtitle; DwButton confirm; ScrollView list;
        public LanguagePage(bool firstRun) { this.firstRun = firstRun; selected = Prefs.Language ?? L10n.Normalize(L10n.DeviceLanguageTag()); }
        public override string Route => Routes.Language;
        public override bool ShowBack => !firstRun;
        public override bool ShowTopBar => !firstRun;
        protected override void Build(VisualElement root)
        {
            var head = Kit.Col("dw-pagehead");
            if (firstRun)
            {
                var logo = new VisualElement(); logo.AddToClassList("dw-welcome__logo"); logo.style.backgroundImage = new StyleBackground(UnityEngine.Resources.Load<UnityEngine.Texture2D>("UI/app_icon")); head.Add(logo);
            }
            title = Kit.Text("", "dw-h1", p => p.OnSurface); subtitle = Kit.Text("", "dw-body", p => p.OnSurfaceVariant);
            head.Add(title); head.Add(Kit.Spacer(6)); head.Add(subtitle); root.Add(head);
            list = Scroll(root);
            foreach (var l in L10n.Languages)
            {
                var lang = l; var card = Kit.Row(); card.AddToClassList("dw-langitem");
                var col = Kit.Col(); col.style.flexGrow = 1;
                var n = Kit.Text(lang.NativeName, "dw-title", null); var e = Kit.Text(lang.EnglishName, "dw-small", p => p.OnSurfaceVariant);
                col.Add(n); col.Add(e); card.Add(col);
                var chk = new Icon(IconKind.Check, 22); card.Add(chk);
                ThemeBinder.On(card, (el, p) => { bool sel = selected == lang.Tag; el.style.backgroundColor = sel ? ThemeBinder.A(p.PrimaryContainer, 0.8f) : ThemeBinder.A(p.Surface, 0.82f); ThemeBinder.Border(el, sel ? ThemeBinder.A(p.Primary, 0.6f) : ThemeBinder.A(p.Outline, 0.3f)); n.style.color = sel ? (p.IsLight ? p.OnPrimaryContainer : p.Primary) : p.OnSurface; n.EnableInClassList("dw-bold", sel); chk.Tint = p.Primary; chk.style.display = sel ? DisplayStyle.Flex : DisplayStyle.None; });
                card.AddManipulator(new Clickable(() => { Native.I.Haptic(Haptic.LongPress); selected = lang.Tag; ThemeBinder.Set(ThemeBinder.Current); Refresh(); }));
                list.contentContainer.Add(card);
            }
            var bar = Kit.Col("dw-bottombar");
            confirm = new DwButton("OK", IconKind.Check, BtnKind.Primary, () => App.SetLanguage(selected)); confirm.AddToClassList("dw-btn--lg");
            bar.Add(confirm); root.Add(bar);
            Refresh();
        }
        void Refresh()
        {
            var t = L10n.TableFor(selected);
            title.text = t.Get(firstRun ? "language_first_run" : "language_title"); subtitle.text = t.Get("language_subtitle");
            string native = null; foreach (var l in L10n.Languages) if (l.Tag == selected) native = l.NativeName;
            confirm.Text.text = firstRun && native != null ? t.Get("language_btn_confirm") + " · " + native : t.Get("language_btn_confirm");
            Root?.EnableInClassList("dw-rtl", L10n.IsRtlTag(selected));
        }
        public override bool HandleBack() => firstRun;
    }
}
