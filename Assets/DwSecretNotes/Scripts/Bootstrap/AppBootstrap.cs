using DwSecretNotes.Core;
using DwSecretNotes.Business;
using DwSecretNotes.Data;
using DwSecretNotes.Platform;
using DwSecretNotes.UI;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
namespace DwSecretNotes.Bootstrap
{
    public sealed class AppBootstrap : MonoBehaviour
    {
        const string Module = "App";
        static AppBootstrap inst;
        [SerializeField] FontAsset primaryFont;
        [SerializeField] FontAsset arabicFont;
        AppShell shell; Camera cam;
        System.Threading.Tasks.Task e2eTask;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (inst == null && FindAnyObjectByType<AppBootstrap>() == null) new GameObject("DwApp").AddComponent<AppBootstrap>(); }
        void Awake()
        {
            if (inst != null) { Destroy(gameObject); return; }
            inst = this; DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            NativeReceiver.Ensure();
            NativeReceiver.NewIntent += _ => HandleLaunch();
        }
        void Start()
        {
            DwLog.I(Module, $"start v{AppInfo.Version} platform={Application.platform}");
            var preferences = new PlayerPrefsRepository();
            bool firstRun = preferences.Language == null;
            L10n.Init(preferences.Language ?? L10n.DeviceLanguageTag());
            int runs = preferences.IncrementRunCount();
            bool showRate = runs == AppInfo.RatePromptRunCount && !preferences.HasRatedApp && !preferences.HasSeenRatePrompt;
            ThemeBinder.Set(ThemeCatalog.Get(preferences.Theme));
            ThemeBinder.SetReducedMotion(SystemAnimationPreference.ReducedMotion());
            cam = new GameObject("Camera").AddComponent<Camera>(); cam.transform.SetParent(transform); cam.clearFlags = CameraClearFlags.SolidColor; cam.cullingMask = 0; cam.orthographic = true;
            var theme = Resources.Load<ThemeStyleSheet>("UI/DwTheme");
            var styles = Resources.Load<StyleSheet>("UI/dw");
            var staticPanel = CreatePanelSettings(theme, 0, true);
            var staticGo = new GameObject("Static UI"); staticGo.transform.SetParent(transform); staticGo.SetActive(false);
            var staticDoc = staticGo.AddComponent<UIDocument>(); staticDoc.panelSettings = staticPanel; staticGo.SetActive(true);
            var staticRoot = staticDoc.rootVisualElement; staticRoot.styleSheets.Add(styles);
            staticRoot.EnableInClassList("dw-reduced-motion", ThemeBinder.ReducedMotion);
            ThemeBinder.On(staticRoot, (e, p) => { staticPanel.colorClearValue = p.Background; cam.backgroundColor = p.Background; });
            var dynamicPanel = CreatePanelSettings(theme, 1, false);
            var go = new GameObject("Dynamic UI"); go.transform.SetParent(transform); go.SetActive(false);
            var doc = go.AddComponent<UIDocument>(); doc.panelSettings = dynamicPanel; go.SetActive(true);
            var root = doc.rootVisualElement;
            root.styleSheets.Add(styles);
            root.EnableInClassList("dw-reduced-motion", ThemeBinder.ReducedMotion);
            FontService.Apply(root, primaryFont, arabicFont, L10n.CurrentTag);
            root.style.unityTextGenerator = new StyleEnum<TextGeneratorType>(TextGeneratorType.Advanced);
            DwLog.I(Module, "textGenerator=" + root.resolvedStyle.unityTextGenerator);
            shell = new AppShell(root, firstRun, new SecretNotesService(new HttpNoteRepository()), preferences, ApplyTheme, ApplyLanguage) { ShowRatePrompt = showRate };
            Native.I.SetSecure(!Debug.isDebugBuild);
            PushWidgetStyle();
            HandleLaunch();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--e2e") >= 0) e2eTask = E2E();
        }
        static PanelSettings CreatePanelSettings(ThemeStyleSheet theme, int sortingOrder, bool clearColor)
        {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            bool mobile = Application.isMobilePlatform;
            float scale = mobile ? (Screen.dpi > 0 ? Screen.dpi / 160f : Screen.width / 411f) : 1f;
            panel.themeStyleSheet = theme; panel.scaleMode = mobile ? PanelScaleMode.ConstantPixelSize : PanelScaleMode.ScaleWithScreenSize;
            panel.scale = scale;
            panel.referenceResolution = new Vector2Int(1080, 1920); panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f; panel.sortingOrder = sortingOrder; panel.clearColor = clearColor;
            return panel;
        }
        async System.Threading.Tasks.Task E2E()
        {
            var sb = new System.Text.StringBuilder(); bool all = true;
            void R(string n, bool ok, string d = "") { all &= ok; var l = $"{(ok ? "PASS" : "FAIL")} {n} {d}"; sb.AppendLine(l); DwLog.I(Module, "e2e " + l); }
            try
            {
                var text = "e2e äöü 秘密 ✓ " + System.DateTime.Now.Ticks;
                var tex = new Texture2D(96, 64, TextureFormat.RGB24, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 96; x++) tex.SetPixel(x, y, new Color(x / 96f, y / 64f, 0.5f));
                tex.Apply(); var img = tex.EncodeToJPG(80); Destroy(tex);
                var enc = await shell.Notes.EncryptAndStore(text, img); R("encrypt", enc.Ok, enc.Error);
                if (enc.Ok)
                {
                    Native.I.CopyText(enc.Link); await System.Threading.Tasks.Task.Delay(700);
                    var clip = Native.I.ReadClipboard(); R("copy-clipboard", clip == enc.Link, clip);
                    bool shareCalled = false; bool? shareOk = null;
                    Native.I.ShareText(L10n.T("share_message", enc.Link), L10n.T("share_title"), ok => { shareCalled = true; shareOk = ok; });
                    R("share-callback", shareCalled || Application.isMobilePlatform, shareOk.ToString());
                    var res = await shell.Notes.ResolveLink(clip); R("resolve", res.HasValue);
                    if (res.HasValue)
                    {
                        var dec = await shell.Notes.FetchAndDecrypt(res.Value.Alias, res.Value.Pass);
                        R("decrypt-text", dec.Status == DecryptStatus.Ok && dec.Note.Text == text, dec.Status.ToString());
                        R("decrypt-image", dec.Note?.Image != null && System.Linq.Enumerable.SequenceEqual(dec.Note.Image, img), dec.Note?.Image?.Length.ToString());
                        await System.Threading.Tasks.Task.Delay(1500);
                        var again = await shell.Notes.FetchAndDecrypt(res.Value.Alias, res.Value.Pass); R("one-time-read", again.Status == DecryptStatus.NotFound, again.Status.ToString());
                    }
                }
            }
            catch (System.Exception e) { R("exception", false, e.ToString()); }
            try
            {
                DonationOffer[] offers = null;
                Native.I.QueryDonations(o => offers = o);
                for (int i = 0; i < 100 && offers == null; i++) await System.Threading.Tasks.Task.Delay(100);
                R("donate-query", offers != null, offers == null ? "timeout" : string.Join(",", System.Array.ConvertAll(offers, x => x.Id + "=" + x.Price)));
                if (Native.I.UsesPlayBilling)
                {
                    string bad = null; Native.I.Donate("donate_unknown_product", r => bad = r);
                    for (int i = 0; i < 50 && bad == null; i++) await System.Threading.Tasks.Task.Delay(100);
                    R("donate-unknown-product", bad == "error", bad);
                }
                else R("donate-paypal-url", AppInfo.PayPalUrl.StartsWith("https://www.paypal.com/"), AppInfo.PayPalUrl);
                foreach (var l in L10n.Languages) R("donate-i18n-" + l.Tag, L10n.TableFor(l.Tag) != null);
            }
            catch (System.Exception e) { R("donate-exception", false, e.ToString()); }
            sb.AppendLine("RESULT " + (all ? "PASS" : "FAIL"));
            System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "e2e.txt"), sb.ToString());
            if (!Application.isMobilePlatform) Invoke(nameof(Quit), 0.5f);
        }
        void Update()
        {
            if (e2eTask != null && e2eTask.IsCompleted)
            {
                var completed = e2eTask;
                e2eTask = null;
                if (completed.IsFaulted) DwLog.E(Module, "e2e task failed", completed.Exception);
            }
            if (shell == null) return;
            shell.ApplySafeArea();
            shell.Update();
            DebugCapture();
        }
        string shotPath; float shotAt = -1; bool shotDone;
        void DebugCapture()
        {
            if (shotDone || (Application.isMobilePlatform && !Debug.isDebugBuild)) return;
            if (shotAt < 0)
            {
                shotAt = float.MaxValue;
                foreach (var a in System.Environment.GetCommandLineArgs())
                {
                    if (a.StartsWith("--shot=")) shotPath = a.Substring(7);
                    else if (a.StartsWith("--at=") && float.TryParse(a.Substring(5), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t)) shotAt = t;
                    else if (a.StartsWith("--route=")) shell.Navigate(a.Substring(8), false);
                    else if (a.StartsWith("--lang=")) { L10n.Init(a.Substring(7)); shell.Rebuild(); }
                    else if (a.StartsWith("--theme=")) ThemeBinder.Set(ThemeCatalog.Get(a.Substring(8)));
                    else if (a == "--menu") shell.ShowMenu();
                    else if (a == "--themes") shell.ShowThemeSheet();
                }
                if (shotPath != null && shotAt == float.MaxValue) shotAt = 3;
            }
            if (shotPath != null && !Application.isMobilePlatform && Time.realtimeSinceStartup >= shotAt) { shotDone = true; ScreenCapture.CaptureScreenshot(shotPath); Invoke(nameof(Quit), 1.5f); }
        }
        void Quit() => Application.Quit();
        void OnApplicationFocus(bool focus)
        {
            if (focus && shell != null) { RefreshMotionPreference(); HandleLaunch(); }
            else if (!focus) shell?.OnAppBackground();
        }
        void OnApplicationPause(bool paused) { if (paused) shell?.OnAppBackground(); }
        void RefreshMotionPreference()
        {
            var reduced = SystemAnimationPreference.ReducedMotion();
            if (reduced == ThemeBinder.ReducedMotion) return;
            ThemeBinder.SetReducedMotion(reduced);
            shell.Root.EnableInClassList("dw-reduced-motion", reduced);
            var staticRoot = transform.Find("Static UI")?.GetComponent<UIDocument>()?.rootVisualElement;
            staticRoot?.EnableInClassList("dw-reduced-motion", reduced);
        }
        void HandleLaunch()
        {
            var r = Native.I.ConsumeLaunchRequest(); if (r == null) return;
            DwLog.Var(Module, "launchKind", r.Kind);
            switch (r.Kind)
            {
                case "view": OpenDecrypt(LinkParser.AliasFromDeepLink(r.Value) ?? r.Value); break;
                case "send": OpenDecrypt(LinkParser.AliasFromShareText(r.Value)); break;
                case "widget": if (int.TryParse(r.Value, out var id)) { shell.Navigate(Routes.Main); shell.Main.OpenQuickEncrypt(id); } break;
            }
        }
        void OpenDecrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            shell.Navigate(Routes.Main);
            shell.Main.OpenForDecrypt(input.Trim());
        }
        void ApplyTheme(string id) { shell.Preferences.Theme = id; ThemeBinder.Set(ThemeCatalog.Get(id)); PushWidgetStyle(); }
        void ApplyLanguage(string tag) { shell.Preferences.Language = tag; L10n.Init(tag); FontService.ApplyLanguage(shell.Root, L10n.CurrentTag); PushWidgetStyle(); shell.OnLanguageConfirmed(); }
        static void PushWidgetStyle() => Native.I.UpdateWidgets(ThemeBinder.Current, L10n.T("hint_enter_text"), L10n.T("btn_encrypt"), L10n.T("btn_share"), L10n.T("share_message"), L10n.T("share_title"));
    }
}
