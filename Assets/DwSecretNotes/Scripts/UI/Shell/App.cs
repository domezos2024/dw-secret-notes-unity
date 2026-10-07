using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public sealed class App : MonoBehaviour
    {
        const string Module = "App";
        static App inst;
        AppShell shell; Camera cam;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (inst == null) new GameObject("DwApp").AddComponent<App>(); }
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
            bool firstRun = Prefs.Language == null;
            L10n.Init(Prefs.Language ?? L10n.DeviceLanguageTag());
            int runs = Prefs.IncrementRunCount();
            bool showRate = runs == AppInfo.RatePromptRunCount && !Prefs.HasRatedApp && !Prefs.HasSeenRatePrompt;
            ThemeBinder.Set(ThemeCatalog.Get(Prefs.Theme));
            cam = new GameObject("Camera").AddComponent<Camera>(); cam.transform.SetParent(transform); cam.clearFlags = CameraClearFlags.SolidColor; cam.cullingMask = 0; cam.orthographic = true;
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/DwTheme");
            if (Application.isMobilePlatform) { ps.scaleMode = PanelScaleMode.ConstantPhysicalSize; ps.referenceDpi = 160; ps.fallbackDpi = 160; }
            else { ps.scaleMode = PanelScaleMode.ConstantPixelSize; ps.scale = 1; }
            ps.clearColor = true;
            var go = new GameObject("UI"); go.transform.SetParent(transform); go.SetActive(false);
            var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; go.SetActive(true);
            var root = doc.rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("UI/dw"));
            FontService.Apply(root, ps);
            ThemeBinder.On(root, (e, p) => { ps.colorClearValue = p.Background; cam.backgroundColor = p.Background; });
            shell = new AppShell(root, firstRun) { ShowRatePrompt = showRate };
            Native.I.SetSecure(!Debug.isDebugBuild);
            PushWidgetStyle();
            HandleLaunch();
        }
        void Update()
        {
            if (shell == null) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) shell.Back();
#endif
            shell.ApplySafeArea();
            shell.Update();
            DebugCapture();
        }
        string shotPath; float shotAt = -1; bool shotDone;
        void DebugCapture()
        {
            if (shotDone || Application.isMobilePlatform) return;
            if (shotAt < 0)
            {
                shotAt = float.MaxValue;
                foreach (var a in System.Environment.GetCommandLineArgs())
                {
                    if (a.StartsWith("--shot=")) shotPath = a.Substring(7);
                    else if (a.StartsWith("--at=") && float.TryParse(a.Substring(5), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var t)) shotAt = t;
                    else if (a.StartsWith("--route=")) shell.Navigate(a.Substring(8), false);
                    else if (a == "--menu") shell.ShowMenu();
                    else if (a == "--themes") shell.ShowThemeSheet();
                }
                if (shotPath != null && shotAt == float.MaxValue) shotAt = 3;
            }
            if (shotPath != null && Time.realtimeSinceStartup >= shotAt) { shotDone = true; ScreenCapture.CaptureScreenshot(shotPath); Invoke(nameof(Quit), 1.5f); }
        }
        void Quit() => Application.Quit();
        void OnApplicationFocus(bool focus) { if (focus && shell != null) HandleLaunch(); }
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
        public static void SetTheme(string id) { Prefs.Theme = id; ThemeBinder.Set(ThemeCatalog.Get(id)); PushWidgetStyle(); }
        public static void SetLanguage(string tag) { Prefs.Language = tag; L10n.Init(tag); PushWidgetStyle(); inst.shell.OnLanguageConfirmed(); }
        static void PushWidgetStyle() => Native.I.UpdateWidgets(ThemeBinder.Current, L10n.T("hint_enter_text"), L10n.T("btn_encrypt"), L10n.T("btn_share"), L10n.T("share_message"), L10n.T("share_title"));
    }
}
