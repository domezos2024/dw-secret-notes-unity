using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public static class DonateCard
    {
        const string Module = "Donate";
        public static VisualElement Build(AppShell shell)
        {
            var card = Kit.Card(p => ThemeBinder.A(p.Secondary, 0.4f)); card.AddToClassList("dw-donate");
            var head = Kit.Row(); head.AddToClassList("dw-gap-row"); head.style.alignItems = Align.Center;
            head.Add(Kit.Icon(IconKind.Heart, 22, p => p.Secondary));
            var title = Kit.Text(L10n.T("donate_title"), "dw-h2", p => p.OnSurface); title.style.flexShrink = 1; head.Add(title);
            card.Add(head); card.Add(Kit.Spacer(8));
            card.Add(Kit.Text(L10n.T("donate_text"), "dw-body", p => p.OnSurfaceVariant)); card.Add(Kit.Spacer(14));
            if (!Native.I.UsesPlayBilling)
            {
                card.Add(new DwButton(L10n.T("donate_paypal"), IconKind.Heart, BtnKind.Primary, () => Native.I.Donate("paypal", null)));
                return card;
            }
            var host = Kit.Col(); card.Add(host);
            var note = Kit.Text(L10n.T("donate_choose"), "dw-small", p => p.OnSurfaceVariant); host.Add(note); host.Add(Kit.Spacer(8));
            var row = Kit.Row(); row.AddToClassList("dw-gap-row"); host.Add(row);
            host.style.display = DisplayStyle.None;
            Native.I.QueryDonations(offers =>
            {
                DwLog.Var(Module, "offers", offers.Length);
                host.style.display = DisplayStyle.Flex;
                if (offers.Length == 0) { note.text = L10n.T("donate_unavailable"); return; }
                foreach (var o in offers)
                {
                    var id = o.Id;
                    var b = new DwButton(o.Price, IconKind.Heart, BtnKind.Primary, null); b.style.flexGrow = 1; b.style.flexBasis = 0;
                    b.SetAction(() => Native.I.Donate(id, r =>
                    {
                        DwLog.Var(Module, "result", r);
                        if (r == "ok") shell.Toast(L10n.T("donate_thanks")); else if (r == "error") shell.Toast(L10n.T("donate_unavailable"));
                    }));
                    row.Add(b);
                }
            });
            return card;
        }
    }
}
