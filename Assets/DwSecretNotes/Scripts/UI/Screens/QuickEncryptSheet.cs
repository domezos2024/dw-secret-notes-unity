using DwSecretNotes.Core;
using DwSecretNotes.Net;
using DwSecretNotes.Platform;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public static class QuickEncryptSheet
    {
        public static void Open(AppShell shell, int widgetId)
        {
            DwLog.Var("QuickEncrypt", "widgetId", widgetId);
            var s = Kit.Col(); s.Add(AppShell.SheetHandle());
            var head = Kit.Row(); head.AddToClassList("dw-gap-row");
            head.Add(Kit.Icon(IconKind.Lock, 22, p => p.Secondary)); head.Add(Kit.Text(L10n.T("btn_encrypt"), "dw-h2", p => p.OnSurface)); s.Add(head); s.Add(Kit.Spacer(12));
            var field = new DwField(L10n.T("hint_enter_text"), true); s.Add(field); s.Add(Kit.Spacer(12));
            var result = Kit.Col(); result.style.display = DisplayStyle.None;
            var link = Kit.Text("", "dw-linkpill__text", p => p.OnSurface);
            var pill = Kit.Row(); pill.AddToClassList("dw-linkpill"); pill.Add(Kit.Icon(IconKind.Link, 18, p => p.Primary)); pill.Add(link); link.style.flexGrow = 1; link.style.flexShrink = 1;
            ThemeBinder.On(pill, (e, p) => { e.style.backgroundColor = p.SurfaceVariant; ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.3f)); });
            result.Add(Kit.Banner(L10n.T("success_encrypted"), true)); result.Add(Kit.Spacer(10)); result.Add(pill); result.Add(Kit.Spacer(12));
            string generated = null;
            var share = new DwButton(L10n.T("btn_share"), IconKind.Share, BtnKind.Primary, () => { if (generated != null) Native.I.ShareText(L10n.T("share_message", generated), L10n.T("share_title"), ok => { if (!ok) shell.Toast(L10n.T("error_share_unavailable")); }); });
            var copy = new DwButton(L10n.T("btn_copy"), IconKind.Copy, BtnKind.Tonal, () => { if (generated != null) { Native.I.CopyText(generated); shell.Toast(L10n.T("snackbar_link_copied")); } });
            var row = Kit.Row(); row.AddToClassList("dw-gap-row"); copy.style.flexGrow = 1; copy.style.flexBasis = 0; share.style.flexGrow = 1; share.style.flexBasis = 0; row.Add(copy); row.Add(share); result.Add(row);
            var error = Kit.Col();
            DwButton encrypt = null;
            encrypt = new DwButton(L10n.T("btn_encrypt"), IconKind.Lock, BtnKind.Primary, async () =>
            {
                var text = field.Value.Trim(); if (text.Length == 0) return;
                encrypt.Busy = true; error.Clear();
                var r = await NoteApi.EncryptAndStore(text, null);
                encrypt.Busy = false;
                if (!r.Ok) { var b = Kit.Banner(r.Error, false); b.style.marginTop = 10; error.Add(b); return; }
                generated = r.Link; link.text = r.Link;
                if (widgetId >= 0) Native.I.SetWidgetLink(widgetId, r.Link);
                field.style.display = DisplayStyle.None; encrypt.style.display = DisplayStyle.None; result.style.display = DisplayStyle.Flex; Kit.Enter(result);
            });
            encrypt.AddToClassList("dw-btn--lg");
            s.Add(encrypt); s.Add(error); s.Add(result);
            ThemeBinder.On(s, (e, p) => e.style.backgroundColor = p.Surface);
            shell.OpenOverlay(s, null, true);
            s.schedule.Execute(() => field.Input.Focus()).StartingIn(300);
        }
    }
}
