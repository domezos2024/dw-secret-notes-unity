using System;
using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public sealed class TinyUrlPage : Page
    {
        public override string Route => Routes.TinyUrl;
        protected override void Build(VisualElement root)
        {
            root.AddToClassList("dw-tiny");
            root.EnableInClassList("dw-rtl", L10n.IsRtl);
            ThemeBinder.On(root, (e, p) => e.style.backgroundColor = p.Background);

            var bg = new VisualElement { pickingMode = PickingMode.Ignore }; bg.AddToClassList("dw-tiny__bg");
            ThemeBinder.On(bg, (e, p) => e.style.backgroundImage = new StyleBackground(Textures.Gradient(p.SurfaceVariant, p.Background)));
            root.Add(bg);
            var g1 = Glow(p => ThemeBinder.A(p.Primary, 0.16f)); g1.AddToClassList("dw-tiny__glow1"); root.Add(g1);
            var g2 = Glow(p => ThemeBinder.A(p.Secondary, 0.12f)); g2.AddToClassList("dw-tiny__glow2"); root.Add(g2);

            var c = Scroll(root).contentContainer; c.style.alignItems = Align.Stretch;
            c.Add(T(AppInfo.Host, "dw-tiny__host", p => p.OnSurface));
            c.Add(T(L10n.T("tinyurl2_headline"), "dw-tiny__headline", p => p.AccentText));
            c.Add(T(L10n.T("tinyurl2_intro"), "dw-tiny__intro", p => p.OnSurfaceVariant));

            var open = Kit.Row(); open.AddToClassList("dw-tiny__open");
            var oi = new Icon(IconKind.External, 20); open.Add(oi);
            open.Add(T(L10n.T("tinyurl2_open_button"), "dw-tiny__opentext", p => p.OnPrimary));
            ThemeBinder.On(open, (e, p) => { e.style.backgroundColor = p.Primary; oi.Tint = p.OnPrimary; });
            Kit.Action(open, () => { Native.I.Haptic(Haptic.LongPress); Native.I.OpenUrl(AppInfo.TinyUrlPage); });
            c.Add(open);

            Tier(c, "tinyurl2_tier_free", 3, IconKind.Globe, false);
            Tier(c, "tinyurl2_tier_reg", 4, IconKind.Unlock, false);
            Tier(c, "tinyurl2_tier_premium", 6, IconKind.Star, true);

            var how = Kit.Col("dw-tiny__card");
            ThemeBinder.On(how, (e, p) => { e.style.backgroundColor = ThemeBinder.A(p.Surface, p.IsLight ? 0.95f : 0.82f); ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.25f)); });
            how.Add(T(L10n.T("tinyurl2_how_it_works_title"), "dw-tiny__howtitle", p => p.AccentText));
            for (int i = 1; i <= 3; i++)
            {
                var r = Kit.Row(); r.AddToClassList("dw-tiny__step");
                var n = new Label(i.ToString()); n.AddToClassList("dw-tiny__num");
                ThemeBinder.On(n, (e, p) => { e.style.backgroundColor = p.Primary; e.style.color = p.OnPrimary; });
                r.Add(n);
                var t = T(L10n.T("tinyurl2_step" + i), "dw-tiny__steptext", p => p.OnSurfaceVariant); t.style.flexShrink = 1;
                r.Add(t); how.Add(r);
            }
            c.Add(how);
        }

        static VisualElement Glow(Func<ThemePalette, Color> tint)
        {
            var g = new VisualElement { pickingMode = PickingMode.Ignore }; g.style.backgroundImage = new StyleBackground(Textures.Glow);
            ThemeBinder.On(g, (e, p) => e.style.unityBackgroundImageTintColor = tint(p));
            return g;
        }
        static Label T(string text, string cls, Func<ThemePalette, Color> color)
        {
            var l = new Label(text); l.AddToClassList(cls);
            return ThemeBinder.On(l, (e, p) => e.style.color = color(p));
        }
        static void Tier(VisualElement c, string key, int items, IconKind icon, bool premium)
        {
            Color Accent(ThemePalette p) => premium ? p.Secondary : p.Primary;
            var card = Kit.Col("dw-tiny__card");
            ThemeBinder.On(card, (e, p) => { e.style.backgroundColor = ThemeBinder.A(p.Surface, p.IsLight ? 0.95f : 0.82f); ThemeBinder.Border(e, ThemeBinder.A(Accent(p), 0.35f)); });
            var badge = T(L10n.T(key + "_badge").ToUpperInvariant(), "dw-tiny__badge", p => ColorMath.EnsureReadable(Accent(p), p.SurfaceVariant, 4.5, p.OnSurface));
            ThemeBinder.On(badge, (e, p) => e.style.backgroundColor = ThemeBinder.A(Accent(p), 0.16f)); card.Add(badge);
            var ic = new Icon(icon, 26); ic.style.marginTop = ThemeBinder.GetSpacing(2); ic.style.marginBottom = ThemeBinder.GetSpacing(2);
            ThemeBinder.On(ic, (e, p) => e.Tint = Accent(p)); card.Add(ic);
            card.Add(T(L10n.T(key + "_title"), "dw-tiny__tiertitle", p => premium ? ColorMath.EnsureReadable(p.Secondary, p.Surface, 4.5, p.OnSurface) : p.OnSurface));
            for (int i = 1; i <= items; i++)
            {
                var r = Kit.Row(); r.AddToClassList("dw-tiny__item"); r.style.alignItems = Align.FlexStart;
                var dot = new VisualElement(); dot.AddToClassList("dw-tiny__dot"); ThemeBinder.On(dot, (e, p) => e.style.backgroundColor = Accent(p)); r.Add(dot);
                var t = T(L10n.T(key + "_item" + i), "dw-tiny__itemtext", p => p.OnSurfaceVariant); t.style.flexShrink = 1;
                r.Add(t); card.Add(r);
            }
            c.Add(card);
        }
    }
}
