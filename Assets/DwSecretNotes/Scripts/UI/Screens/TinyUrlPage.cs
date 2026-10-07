using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public sealed class TinyUrlPage : Page
    {
        static readonly Color Navy900 = ColorMath.Hex(0xFF050D1F), Cyan = ColorMath.Hex(0xFF00D4FF), Gold = ColorMath.Hex(0xFFF0C040), White = ColorMath.Hex(0xFFF0F4FF), Muted = ColorMath.Hex(0xFF8899BB), Glass = ColorMath.Hex(0xA6091428);
        public override string Route => Routes.TinyUrl;
        protected override void Build(VisualElement root)
        {
            root.AddToClassList("dw-tiny");
            var bg = new VisualElement { pickingMode = PickingMode.Ignore }; bg.AddToClassList("dw-tiny__bg"); bg.style.backgroundImage = new StyleBackground(Textures.Gradient(ColorMath.Hex(0xFF0D1E3A), ColorMath.Hex(0xFF02080F))); root.Add(bg);
            var g1 = Glow(ColorMath.Hex(0x40006AC8)); g1.AddToClassList("dw-tiny__glow1"); root.Add(g1);
            var g2 = Glow(ColorMath.Hex(0x4D003278)); g2.AddToClassList("dw-tiny__glow2"); root.Add(g2);
            var c = Scroll(root).contentContainer; c.style.alignItems = Align.Stretch;
            c.Add(T(AppInfo.Host, "dw-tiny__host", White));
            c.Add(T(L10n.T("tinyurl2_headline"), "dw-tiny__headline", Cyan));
            c.Add(T(L10n.T("tinyurl2_intro"), "dw-tiny__intro", Muted));
            var open = Kit.Row(); open.AddToClassList("dw-tiny__open"); open.style.backgroundColor = Cyan;
            var oi = new Icon(IconKind.External, 20) { Tint = Navy900 }; open.Add(oi); open.Add(T(L10n.T("tinyurl2_open_button"), "dw-tiny__opentext", Navy900));
            open.AddManipulator(new Clickable(() => { Native.I.Haptic(Haptic.LongPress); Native.I.OpenUrl(AppInfo.TinyUrlPage); }));
            c.Add(open);
            Tier(c, "tinyurl2_tier_free", 3, IconKind.Globe, ColorMath.Hex(0x4000D4FF), ColorMath.Hex(0x1F00D4FF), Cyan, White);
            Tier(c, "tinyurl2_tier_reg", 4, IconKind.Unlock, ColorMath.Hex(0x8000D4FF), ColorMath.Hex(0x2E00D4FF), Cyan, White);
            Tier(c, "tinyurl2_tier_premium", 6, IconKind.Star, ColorMath.Hex(0x73F0C040), ColorMath.Hex(0x2EF0C040), Gold, Gold);
            var how = Kit.Col("dw-tiny__card"); how.style.backgroundColor = Glass; ThemeBinder.Border(how, ColorMath.Hex(0x2E00D4FF));
            how.Add(T(L10n.T("tinyurl2_how_it_works_title"), "dw-tiny__howtitle", Cyan));
            for (int i = 1; i <= 3; i++)
            {
                var r = Kit.Row(); r.AddToClassList("dw-tiny__step");
                var n = new Label(i.ToString()); n.AddToClassList("dw-tiny__num"); n.style.backgroundColor = Cyan; n.style.color = Navy900; r.Add(n);
                var t = T(L10n.T("tinyurl2_step" + i), "dw-tiny__steptext", Muted); t.style.flexShrink = 1; r.Add(t); how.Add(r);
            }
            c.Add(how);
        }
        static VisualElement Glow(Color c) { var g = new VisualElement { pickingMode = PickingMode.Ignore }; g.style.backgroundImage = new StyleBackground(Textures.Glow); g.style.unityBackgroundImageTintColor = c; return g; }
        static Label T(string text, string cls, Color color) { var l = new Label(text); l.AddToClassList(cls); l.style.color = color; return l; }
        static void Tier(VisualElement c, string key, int items, IconKind icon, Color border, Color badgeBg, Color accent, Color titleColor)
        {
            var card = Kit.Col("dw-tiny__card"); card.style.backgroundColor = Glass; ThemeBinder.Border(card, border);
            var badge = T(L10n.T(key + "_badge").ToUpperInvariant(), "dw-tiny__badge", accent); badge.style.backgroundColor = badgeBg; card.Add(badge);
            var ic = new Icon(icon, 26) { Tint = accent }; ic.style.marginTop = 10; ic.style.marginBottom = 8; card.Add(ic);
            card.Add(T(L10n.T(key + "_title"), "dw-tiny__tiertitle", titleColor));
            for (int i = 1; i <= items; i++)
            {
                var r = Kit.Row(); r.AddToClassList("dw-tiny__item"); r.style.alignItems = Align.FlexStart;
                var dot = new VisualElement(); dot.AddToClassList("dw-tiny__dot"); dot.style.backgroundColor = accent; r.Add(dot);
                var t = T(L10n.T(key + "_item" + i), "dw-tiny__itemtext", Muted); t.style.flexShrink = 1; r.Add(t); card.Add(r);
            }
            c.Add(card);
        }
    }
}
