using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public sealed class HelpPage : Page
    {
        public override string Route => Routes.Help;
        protected override void Build(VisualElement root)
        {
            var c = Scroll(root).contentContainer;
            c.Add(Kit.Text(L10n.T("help_title"), "dw-h1", p => p.OnSurface)); c.Add(Kit.Spacer(16));
            Section(c, IconKind.Sparkle, "help_how_title", "help_how_body", true);
            Section(c, IconKind.Shield, "help_security_title", "help_security_body", true);
            Section(c, IconKind.Help, "help_faq_q1", "help_faq_a1", false);
            Section(c, IconKind.Help, "help_faq_q2", "help_faq_a2", false);
            c.Add(Kit.Spacer(16)); c.Add(Footer.Build(Shell));
        }
        static void Section(VisualElement c, IconKind icon, string titleKey, string bodyKey, bool open)
        {
            var card = Kit.Card(); card.AddToClassList("dw-accordion"); card.EnableInClassList("dw-accordion--open", open);
            var head = Kit.Row(); head.AddToClassList("dw-gap-row");
            head.Add(Kit.Icon(icon, 22, p => p.Primary));
            var t = Kit.Text(L10n.T(titleKey), "dw-title", p => p.Primary); t.style.flexGrow = 1; t.style.flexShrink = 1; head.Add(t);
            var chev = Kit.Icon(IconKind.Plus, 18, p => p.OnSurfaceVariant); chev.AddToClassList("dw-accordion__chev"); head.Add(chev);
            var body = Kit.Text(L10n.T(bodyKey), "dw-body dw-accordion__body", p => p.OnSurface);
            card.Add(head); card.Add(body);
            head.AddManipulator(new Clickable(() => { Native.I.Haptic(Haptic.LongPress); card.ToggleInClassList("dw-accordion--open"); }));
            c.Add(card); c.Add(Kit.Spacer(12));
        }
    }
}
