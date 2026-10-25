using System;
using System.Threading;
using System.Threading.Tasks;
using DwSecretNotes.Business;
using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public sealed class MainPage : Page
    {
        const string Module = "MainPage";
        public override string Route => Routes.Main;
        public override bool ShowBack => false;
        public override bool KeepAlive => true;
        ScrollView scroll;
        VisualElement tabIndicator, tabEncrypt, tabDecrypt, encryptIdle, encryptDone, decryptIdle, decryptDone, thumbWrap, thumb, errorHost, decImage, adBox;
        DwField input, decInput; DwButton encryptBtn, decryptBtn, copyBtn, shareBtn; Label linkLabel, decText, countdownText, adText; CountdownRing ring;
        byte[] imageBytes; Texture2D thumbTex, decTex;
        string generatedLink = "", remotePass = "";
        bool decryptTab, isEncrypting, isDecrypting, decryptShown; float decryptUntil; int lastSecond = -1, adIndex;
        CancellationTokenSource encryptCancellation, decryptCancellation;
        IVisualElementScheduledItem adTimer;
        bool adHovered, adFocused;
        readonly SecretNotesService notes;
        public MainPage(SecretNotesService notes) { this.notes = notes; }
        protected override void Build(VisualElement root)
        {
            root.Add(new Aurora());
            scroll = Scroll(root); var c = scroll.contentContainer;
            c.Add(BuildHero()); c.Add(Kit.Spacer(16));
            c.Add(BuildTabs()); c.Add(Kit.Spacer(16));
            BuildEncrypt(c); BuildDecrypt(c);
            c.Add(Kit.Spacer(16)); c.Add(DonateCard.Build(Shell));
            c.Add(Kit.Spacer(24)); c.Add(Footer.Build(Shell));
            SelectTab(false, false);
        }
        VisualElement BuildHero()
        {
            var hero = Kit.Card(); hero.AddToClassList("dw-hero");
            var row = Kit.Row(); row.AddToClassList("dw-hero__row");
            var badge = new VisualElement(); badge.AddToClassList("dw-hero__badge");
            var glow = new VisualElement { pickingMode = PickingMode.Ignore }; glow.AddToClassList("dw-hero__glow"); glow.style.backgroundImage = new StyleBackground(Textures.Glow); badge.Add(glow);
            badge.Add(Kit.Icon(IconKind.Shield, 34, p => p.Primary));
            ThemeBinder.On(badge, (e, p) => { e.style.backgroundColor = ThemeBinder.A(p.PrimaryContainer, 0.55f); ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.4f)); glow.style.unityBackgroundImageTintColor = ThemeBinder.A(p.Primary, 0.35f); });
            row.Add(badge);
            var col = Kit.Col(); col.style.flexGrow = 1; col.style.flexShrink = 1;
            col.Add(Kit.Text(L10n.T("ad_title"), "dw-overline", p => p.Tertiary));
            adBox = new VisualElement { focusable = true, viewDataKey = "hero-ad" }; adBox.AddToClassList("dw-hero__ad");
            adText = Kit.Text(Ad(0), "dw-body", p => p.OnSurface); adBox.Add(adText); col.Add(adBox);
            adBox.RegisterCallback<MouseEnterEvent>(_ => { adHovered = true; adTimer?.Pause(); });
            adBox.RegisterCallback<MouseLeaveEvent>(_ => { adHovered = false; if (!adFocused) adTimer?.Resume(); });
            adBox.RegisterCallback<FocusInEvent>(_ => { adFocused = true; adTimer?.Pause(); });
            adBox.RegisterCallback<FocusOutEvent>(_ => { adFocused = false; if (!adHovered) adTimer?.Resume(); });
            row.Add(col); hero.Add(row);
            adIndex = UnityEngine.Random.Range(0, 10); adText.text = Ad(adIndex);
            adTimer = hero.schedule.Execute(() =>
            {
                adBox.AddToClassList("dw-fade-out");
                adBox.schedule.Execute(() => { adIndex = (adIndex + 1) % 10; adText.text = Ad(adIndex); adBox.RemoveFromClassList("dw-fade-out"); }).StartingIn(320);
            }).Every(5500).StartingIn(5500);
            return hero;
        }
        static string Ad(int i) => L10n.T("ad_" + (i + 1));
        VisualElement BuildTabs()
        {
            var tabs = Kit.Row(); tabs.AddToClassList("dw-tabs");
            tabIndicator = new VisualElement { pickingMode = PickingMode.Ignore }; tabIndicator.AddToClassList("dw-tabs__indicator"); tabs.Add(tabIndicator);
            tabEncrypt = Tab(IconKind.Lock, L10n.T("btn_encrypt"), () => SelectTab(false, true)); tabEncrypt.viewDataKey = "tab-encrypt";
            tabDecrypt = Tab(IconKind.Unlock, L10n.T("btn_decrypt"), () => SelectTab(true, true)); tabDecrypt.viewDataKey = "tab-decrypt";
            tabs.Add(tabEncrypt); tabs.Add(tabDecrypt);
            ThemeBinder.On(tabs, (e, p) => { e.style.backgroundColor = ThemeBinder.A(p.SurfaceVariant, 0.85f); ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.15f)); tabIndicator.style.backgroundColor = p.Primary; PaintTabs(p); });
            return tabs;
        }
        VisualElement Tab(IconKind icon, string text, Action a)
        {
            var t = Kit.Row(); t.AddToClassList("dw-tab"); t.tooltip = text;
            var ic = new Icon(icon, 18); var l = new Label(text) { pickingMode = PickingMode.Ignore }; l.AddToClassList("dw-tab__text");
            t.Add(ic); t.Add(l); t.userData = (ic, l);
            Kit.Action(t, () => { Native.I.Haptic(Haptic.LongPress); a(); });
            return t;
        }
        void PaintTabs(ThemePalette p)
        {
            void Paint(VisualElement t, bool sel) { var (ic, l) = ((Icon, Label))t.userData; var col = sel ? p.OnPrimary : p.OnSurfaceVariant; ic.Tint = col; l.style.color = col; }
            Paint(tabEncrypt, !decryptTab); Paint(tabDecrypt, decryptTab);
        }
        void SelectTab(bool decrypt, bool animate)
        {
            decryptTab = decrypt;
            Shell.RefreshAccessibility();
            tabIndicator.EnableInClassList("dw-tabs__indicator--right", decrypt);
            PaintTabs(ThemeBinder.Current);
            Show(encryptIdle, !decrypt && !encryptDone.ClassListContains("dw-on")); Show(encryptDone, !decrypt && encryptDone.ClassListContains("dw-on"));
            Show(decryptIdle, decrypt && !decryptShown); Show(decryptDone, decrypt && decryptShown);
            if (animate) Kit.Enter(decrypt ? (decryptShown ? decryptDone : decryptIdle) : (encryptDone.ClassListContains("dw-on") ? encryptDone : encryptIdle));
        }
        static void Show(VisualElement e, bool on) => e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
        void BuildEncrypt(VisualElement c)
        {
            encryptIdle = Kit.Card(); encryptIdle.AddToClassList("dw-section");
            input = new DwField(L10n.T("hint_enter_text"), true, true, "field-input"); input.Changed += _ => RefreshEncryptState();
            encryptIdle.Add(input); encryptIdle.Add(Kit.Spacer(12));
            var row = Kit.Row(); row.AddToClassList("dw-attachrow");
            var attach = new DwButton(L10n.T("btn_attach_image"), IconKind.Image, BtnKind.Tonal, PickImage); attach.style.flexGrow = 1; attach.style.flexShrink = 1; row.Add(attach);
            thumbWrap = new VisualElement(); thumbWrap.AddToClassList("dw-thumb");
            thumb = new VisualElement { pickingMode = PickingMode.Ignore }; thumb.AddToClassList("dw-thumb__img"); thumbWrap.Add(thumb);
            var rm = new VisualElement(); rm.AddToClassList("dw-thumb__remove"); rm.tooltip = L10n.T("btn_remove_image"); var rmIc = new Icon(IconKind.Close, 16); rm.Add(rmIc);
            ThemeBinder.On(rm, (e, p) => { e.style.backgroundColor = p.Error; rmIc.Tint = ColorMath.BestOn(p.Error); });
            Kit.Action(rm, () => { Native.I.Haptic(Haptic.LongPress); ClearImage(); });
            thumbWrap.Add(rm); thumbWrap.style.display = DisplayStyle.None; row.Add(thumbWrap);
            encryptIdle.Add(row); encryptIdle.Add(Kit.Spacer(14));
            encryptBtn = new DwButton(L10n.T("btn_encrypt"), IconKind.Lock, BtnKind.Primary, DoEncrypt); encryptBtn.viewDataKey = "btn-encrypt"; encryptBtn.AddToClassList("dw-btn--lg");
            encryptIdle.Add(encryptBtn); c.Add(encryptIdle);
            encryptDone = Kit.Col("dw-section");
            encryptDone.Add(Kit.Banner(L10n.T("success_encrypted"), true)); encryptDone.Add(Kit.Spacer(12));
            var card = Kit.Card();
            card.Add(Kit.Text(L10n.T("label_encrypted_link"), "dw-overline", p => p.OnSurfaceVariant)); card.Add(Kit.Spacer(8));
            var pill = Kit.Row(); pill.AddToClassList("dw-linkpill");
            pill.Add(Kit.Icon(IconKind.Link, 18, p => p.Primary));
            linkLabel = Kit.Text("", "dw-linkpill__text", p => p.OnSurface); linkLabel.style.flexGrow = 1; linkLabel.style.flexShrink = 1; pill.Add(linkLabel);
            ThemeBinder.On(pill, (e, p) => { e.style.backgroundColor = ThemeBinder.A(p.SurfaceVariant, 0.9f); ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.3f)); });
            card.Add(pill); card.Add(Kit.Spacer(12));
            var btns = Kit.Row(); btns.AddToClassList("dw-gap-row");
            copyBtn = new DwButton(L10n.T("btn_copy"), IconKind.Copy, BtnKind.Primary, CopyLink); copyBtn.style.flexGrow = 1; copyBtn.style.flexBasis = 0;
            shareBtn = new DwButton(L10n.T("btn_share"), IconKind.Share, BtnKind.Primary, ShareLink); shareBtn.style.flexGrow = 1; shareBtn.style.flexBasis = 0;
            btns.Add(copyBtn); btns.Add(shareBtn); card.Add(btns);
            encryptDone.Add(card); encryptDone.Add(Kit.Spacer(12));
            encryptDone.Add(new DwButton(L10n.T("btn_new_message"), IconKind.Plus, BtnKind.Tonal, ResetEncrypt));
            c.Add(encryptDone);
            RefreshEncryptState();
        }
        void BuildDecrypt(VisualElement c)
        {
            decryptIdle = Kit.Card(); decryptIdle.AddToClassList("dw-section");
            decryptIdle.Add(Kit.Text(L10n.T("decrypt_hint_body"), "dw-body", p => p.OnSurfaceVariant)); decryptIdle.Add(Kit.Spacer(12));
            var row = Kit.Row(); row.AddToClassList("dw-gap-row"); row.style.alignItems = Align.FlexStart;
            decInput = new DwField(L10n.T("hint_decrypt_link"), true, true, "field-decrypt-link"); decInput.style.flexGrow = 1; decInput.style.flexShrink = 1;
            decInput.Changed += v => { errorHost.Clear(); };
            row.Add(decInput);
            var paste = new DwButton("", IconKind.Paste, BtnKind.Tonal, () => { var t = Native.I.ReadClipboard(); if (!string.IsNullOrEmpty(t)) { decInput.Value = t.Trim(); errorHost.Clear(); } }); paste.AddToClassList("dw-btn--square");
            row.Add(paste); decryptIdle.Add(row); decryptIdle.Add(Kit.Spacer(14));
            decryptBtn = new DwButton(L10n.T("btn_decrypt"), IconKind.Unlock, BtnKind.Primary, () => DoDecrypt()); decryptBtn.viewDataKey = "btn-decrypt"; decryptBtn.AddToClassList("dw-btn--lg");
            decryptIdle.Add(decryptBtn);
            errorHost = Kit.Col(); decryptIdle.Add(errorHost);
            c.Add(decryptIdle);
            decryptDone = Kit.Col("dw-section");
            decryptDone.Add(Kit.Banner(L10n.T("success_decrypted"), true)); decryptDone.Add(Kit.Spacer(12));
            var card = Kit.Card(p => ThemeBinder.A(p.Secondary, 0.35f));
            card.Add(Kit.Text(L10n.T("label_secret_message"), "dw-overline", p => p.Tertiary)); card.Add(Kit.Spacer(8));
            decText = Kit.Text("", "dw-secret", p => p.OnSurface); card.Add(decText);
            decImage = new VisualElement(); decImage.AddToClassList("dw-decimage"); decImage.tooltip = L10n.T("btn_open_image");
            Kit.Action(decImage, () => { if (decTex != null) { Native.I.Haptic(Haptic.LongPress); Shell.ShowImage(decTex); } });
            decImage.RegisterCallback<GeometryChangedEvent>(_ => SizeDecImage());
            card.Add(decImage);
            var div = new VisualElement(); div.AddToClassList("dw-divider"); ThemeBinder.On(div, (e, p) => e.style.backgroundColor = ThemeBinder.A(p.Outline, 0.4f)); card.Add(div);
            var cd = Kit.Row(); cd.AddToClassList("dw-gap-row");
            ring = new CountdownRing(58); ring.Value.style.display = DisplayStyle.None; cd.Add(ring);
            ThemeBinder.On(ring, (e, p) => { e.Value.style.color = p.AccentText; UpdateRing(); });
            var texts = Kit.Col(); texts.style.flexShrink = 1; texts.style.flexGrow = 1;
            countdownText = Kit.Text("", "dw-small dw-bold", p => p.AccentText); texts.Add(countdownText);
            texts.Add(Kit.Text(L10n.T("note_destroyed"), "dw-small", p => p.OnSurfaceVariant));
            cd.Add(texts); card.Add(cd);
            decryptDone.Add(card); decryptDone.Add(Kit.Spacer(12));
            decryptDone.Add(new DwButton(L10n.T("btn_read_another"), IconKind.Plus, BtnKind.Tonal, () => ClearDecrypted(true)));
            c.Add(decryptDone);
        }
        void RefreshEncryptState()
        {
            input.Hint = L10n.T(imageBytes != null ? "hint_enter_text_optional" : "hint_enter_text");
            encryptBtn.SetEnabled(!isEncrypting && (!string.IsNullOrWhiteSpace(input.Value) || imageBytes != null));
        }
        void PickImage()
        {
            Native.I.PickImage(bytes =>
            {
                if (bytes == null || bytes.Length == 0) return;
                var tex = new Texture2D(2, 2);
                if (!tex.LoadImage(bytes)) { UnityEngine.Object.Destroy(tex); DwLog.W(Module, "picked image not decodable"); return; }
                ClearImage(); imageBytes = bytes; thumbTex = tex;
                thumb.style.backgroundImage = new StyleBackground(tex); thumbWrap.style.display = DisplayStyle.Flex; Kit.Enter(thumbWrap);
                DwLog.Var(Module, "imageBytes", bytes.Length);
                RefreshEncryptState();
            });
        }
        void ClearImage()
        {
            imageBytes = null; thumbWrap.style.display = DisplayStyle.None; thumb.style.backgroundImage = StyleKeyword.None;
            if (thumbTex != null) { UnityEngine.Object.Destroy(thumbTex); thumbTex = null; }
            RefreshEncryptState();
        }
        void DoEncrypt() => _ = DoEncryptAsync();
        async Task DoEncryptAsync()
        {
            var text = input.Value.Trim();
            if (text.Length == 0 && imageBytes == null) return;
            var finalText = text.Length == 0 && imageBytes != null ? " " : text;
            var cancellation = new CancellationTokenSource();
            encryptCancellation = cancellation;
            isEncrypting = true; encryptBtn.Busy = true; encryptBtn.Text.text = L10n.T("label_encrypting"); RefreshEncryptState();
            try
            {
                var result = await notes.EncryptAndStore(finalText, imageBytes, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (result.Ok)
                {
                    Native.I.Haptic(Haptic.TextHandleMove);
                    generatedLink = result.Link; linkLabel.text = result.Link; copyBtn.SetEnabled(true); shareBtn.SetEnabled(true);
                    encryptDone.AddToClassList("dw-on"); ClearImage(); SelectTab(false, true);
                }
                else Shell.Toast(result.Error);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { DwLog.I(Module, "encrypt canceled"); }
            catch (Exception e) { DwLog.E(Module, "encrypt", e); Shell.Toast(L10n.T("error_encrypt_failed")); }
            finally
            {
                if (ReferenceEquals(encryptCancellation, cancellation))
                {
                    encryptCancellation = null; isEncrypting = false; encryptBtn.Busy = false;
                    encryptBtn.Text.text = L10n.T("btn_encrypt"); RefreshEncryptState();
                }
                cancellation.Dispose();
            }
        }
        void CopyLink()
        {
            if (string.IsNullOrEmpty(generatedLink)) return;
            Native.I.CopyText(generatedLink); Shell.Toast(L10n.T("snackbar_link_copied")); ConsumeLink();
        }
        void ShareLink()
        {
            if (string.IsNullOrEmpty(generatedLink)) return;
            var link = generatedLink;
            Native.I.ShareText(L10n.T("share_message", link), L10n.T("share_title"), ok => { if (!ok) { Native.I.CopyText(link); Shell.Toast(L10n.T("snackbar_link_copied")); } });
            ConsumeLink();
        }
        void ConsumeLink() { generatedLink = ""; linkLabel.text = ""; copyBtn.SetEnabled(false); shareBtn.SetEnabled(false); }
        void ResetEncrypt() { input.Value = ""; generatedLink = ""; encryptDone.RemoveFromClassList("dw-on"); ClearImage(); SelectTab(false, true); }
        public void OpenForDecrypt(string value)
        {
            DwLog.Call(Module);
            if (value.Contains("|")) { decInput.Value = LinkParser.Before(value, "|"); remotePass = LinkParser.After(value, "|"); }
            else { decInput.Value = value; remotePass = ""; }
            ClearDecrypted(false); errorHost.Clear(); SelectTab(true, true);
            var v = decInput.Value;
            if (v.Length >= 5 || v.StartsWith("http")) Root.schedule.Execute(() => DoDecrypt()).StartingIn(100);
        }
        void DoDecrypt() => _ = DoDecryptAsync();
        async Task DoDecryptAsync()
        {
            var raw = decInput.Value.Trim(); if (raw.Length == 0 || isDecrypting) return;
            var cancellation = new CancellationTokenSource();
            decryptCancellation = cancellation;
            isDecrypting = true; decryptBtn.Busy = true; decryptBtn.Text.text = L10n.T("label_decrypting"); errorHost.Clear();
            try
            {
                var result = await notes.DecryptLink(raw, remotePass, cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (result.Status != DecryptStatus.Ok)
                {
                    var banner = Kit.Banner(L10n.T(result.Status == DecryptStatus.NotFound ? "error_not_found" : "error_decrypt_failed"), false);
                    banner.style.marginTop = 12; errorHost.Clear(); errorHost.Add(banner); Kit.Enter(banner); return;
                }
                Native.I.Haptic(Haptic.TextHandleMove);
                remotePass = "";
                var text = result.Note.Text == " " ? "" : result.Note.Text;
                decText.text = text; decText.style.display = text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                if (decTex != null) { UnityEngine.Object.Destroy(decTex); decTex = null; }
                if (result.Note.Image != null) { var texture = new Texture2D(2, 2); if (texture.LoadImage(result.Note.Image)) decTex = texture; else UnityEngine.Object.Destroy(texture); }
                decImage.style.display = decTex != null ? DisplayStyle.Flex : DisplayStyle.None;
                decImage.style.backgroundImage = decTex != null ? new StyleBackground(decTex) : new StyleBackground(StyleKeyword.None);
                decryptShown = true; decryptUntil = Time.realtimeSinceStartup + AppInfo.DecryptCountdownSeconds; lastSecond = -1;
                SelectTab(true, true); UpdateCountdown(); scroll.schedule.Execute(() => scroll.ScrollTo(decryptDone)).StartingIn(60);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { DwLog.I(Module, "decrypt canceled"); }
            catch (Exception e) { DwLog.E(Module, "decrypt", e); Shell.Toast(L10n.T("error_decrypt_failed")); }
            finally
            {
                if (ReferenceEquals(decryptCancellation, cancellation))
                {
                    decryptCancellation = null; isDecrypting = false; decryptBtn.Busy = false;
                    decryptBtn.Text.text = L10n.T("btn_decrypt");
                }
                cancellation.Dispose();
            }
        }
        public void CancelPendingRequests()
        {
            if (encryptCancellation != null) { DwLog.I(Module, "cancel pending encrypt"); encryptCancellation.Cancel(); }
            if (decryptCancellation != null) { DwLog.I(Module, "cancel pending decrypt"); decryptCancellation.Cancel(); }
        }
        public override void OnHide() { CancelPendingRequests(); adTimer?.Pause(); }
        void SizeDecImage()
        {
            if (decTex == null) return; float w = decImage.resolvedStyle.width; if (float.IsNaN(w) || w <= 0) return;
            float h = Mathf.Min(400, w * decTex.height / (float)decTex.width);
            if (Mathf.Abs(decImage.resolvedStyle.height - h) > 0.5f) decImage.style.height = h;
        }
        void ClearDecrypted(bool clearInput)
        {
            decryptShown = false; decText.text = "";
            if (decTex != null) { decImage.style.backgroundImage = new StyleBackground(StyleKeyword.None); UnityEngine.Object.Destroy(decTex); decTex = null; }
            if (clearInput) decInput.Value = "";
            SelectTab(decryptTab, true);
        }
        void UpdateCountdown()
        {
            if (!decryptShown) return;
            float left = decryptUntil - Time.realtimeSinceStartup;
            if (left <= 0) { ClearDecrypted(true); return; }
            int sec = Mathf.CeilToInt(left);
            if (sec != lastSecond) { lastSecond = sec; countdownText.text = L10n.Plural("note_auto_delete", sec); ring.Value.text = sec.ToString(); ring.tooltip = countdownText.text; }
            UpdateRing();
        }
        void UpdateRing() { if (ring == null) return; var p = ThemeBinder.Current; ring.Set(decryptShown ? (decryptUntil - Time.realtimeSinceStartup) / AppInfo.DecryptCountdownSeconds : 0, ThemeBinder.A(p.Outline, 0.3f), p.AccentText); }
        public override void OnUpdate() => UpdateCountdown();
        bool rateShown;
        public override void OnShow()
        {
            if (!adHovered && !adFocused) adTimer?.Resume();
            if (rateShown || !Shell.ShowRatePrompt) return; rateShown = true; Shell.ShowRatePrompt = false;
            Root.schedule.Execute(() => Shell.ShowDialog(L10n.T("rate_dialog_title"), L10n.T("rate_dialog_message"), L10n.T("rate_dialog_confirm"), L10n.T("rate_dialog_dismiss"),
                () => { Shell.Preferences.HasSeenRatePrompt = true; Native.I.RequestReview(() => { Shell.Preferences.HasRatedApp = true; Footer.RefreshAll(); }); },
                () => Shell.Preferences.HasSeenRatePrompt = true)).StartingIn(700);
        }
        public void OpenQuickEncrypt(int widgetId) => QuickEncryptSheet.Open(Shell, widgetId);
    }
}
