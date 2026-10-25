using System;
using System.Collections.Generic;
using DwSecretNotes.Business;
using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public static class Routes { public const string Language = "language", Main = "main", Help = "help", Info = "info", TinyUrl = "tinyUrl"; }
    public sealed class AppShell
    {
        const string Module = "Shell";
        public readonly VisualElement Root;
        public readonly SecretNotesService Notes;
        public readonly IAppPreferences Preferences;
        readonly VisualElement safe, pageHost, overlay, toastHost;
        readonly List<VisualElement> toastQueue = new List<VisualElement>();
        const int MaxToasts = 3;
        TopBar topBar;
        readonly Dictionary<string, Page> alive = new Dictionary<string, Page>();
        Page current; MainPage main;
        readonly Stack<Action> overlayClosers = new Stack<Action>();
        readonly Action<string> applyTheme, applyLanguage;
        readonly ScreenReaderBridge screenReader;
        public bool StartWithLanguage { get; private set; }
        public bool ShowRatePrompt;
        public AppShell(VisualElement root, bool firstRun, SecretNotesService notes, IAppPreferences preferences, Action<string> applyTheme, Action<string> applyLanguage)
        {
            Root = root; Notes = notes; Preferences = preferences; this.applyTheme = applyTheme; this.applyLanguage = applyLanguage; StartWithLanguage = firstRun;
            screenReader = new ScreenReaderBridge(root);
            root.AddToClassList("dw-root");
            safe = new VisualElement(); safe.AddToClassList("dw-safe"); root.Add(safe);
            pageHost = new VisualElement(); pageHost.AddToClassList("dw-pagehost");
            overlay = new VisualElement { pickingMode = PickingMode.Ignore }; overlay.AddToClassList("dw-overlay"); root.Add(overlay);
            toastHost = new VisualElement { pickingMode = PickingMode.Ignore }; toastHost.AddToClassList("dw-toasthost"); root.Add(toastHost);
            root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());
            root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            root.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            BuildChrome();
            Navigate(firstRun ? Routes.Language : Routes.Main, false);
        }
        void BuildChrome()
        {
            safe.Clear(); topBar = new TopBar(this); safe.Add(topBar); safe.Add(pageHost);
            Root.EnableInClassList("dw-rtl", L10n.IsRtl);
        }
        public void Rebuild()
        {
            DwLog.Call(Module);
            CloseAllOverlays();
            foreach (var p in alive.Values) p.OnHide();
            alive.Clear(); main = null; pageHost.Clear(); var route = current?.Route ?? Routes.Main; current = null;
            BuildChrome(); ThemeBinder.Prune(); Navigate(route == Routes.Language ? Routes.Main : route, false);
        }
        Rect lastSafe; float lastWidth;
        public void ApplySafeArea()
        {
            var sa = Screen.safeArea; float w = Root.resolvedStyle.width; if (float.IsNaN(w) || w <= 0 || Screen.width <= 0) return;
            if (sa == lastSafe && Mathf.Approximately(w, lastWidth)) return; lastSafe = sa; lastWidth = w;
            float k = w / Screen.width;
            safe.style.paddingTop = (Screen.height - sa.yMax) * k; safe.style.paddingBottom = sa.y * k; safe.style.paddingLeft = sa.x * k; safe.style.paddingRight = (Screen.width - sa.xMax) * k;
            overlay.style.paddingBottom = sa.y * k; toastHost.style.bottom = sa.y * k + 24;
        }
        Page Create(string route)
        {
            Page p = route switch
            {
                Routes.Language => new LanguagePage(StartWithLanguage && current == null, Preferences, applyLanguage),
                Routes.Help => new HelpPage(), Routes.Info => new InfoPage(), Routes.TinyUrl => new TinyUrlPage(),
                _ => main ??= new MainPage(Notes)
            };
            if (p.Root == null) p.Init(this);
            return p;
        }
        public void Navigate(string route, bool animate = true)
        {
            DwLog.Var(Module, "route", route);
            if (current != null && current.Route == route) return;
            var focusedPage = current?.Root != null && Root.panel?.focusController.focusedElement is VisualElement focused && current.Root.Contains(focused);
            var prev = current;
            var next = Create(route);
            if (prev != null)
            {
                prev.OnHide();
                if (prev.KeepAlive) prev.Root.style.display = DisplayStyle.None;
            }
            current = next;
            if (next.Root.parent != pageHost) pageHost.Add(next.Root);
            if (prev != null && !prev.KeepAlive) { prev.Root.RemoveFromHierarchy(); ThemeBinder.Prune(); }
            next.Root.style.display = DisplayStyle.Flex;
            topBar.Configure(next);
            if (animate) Kit.Enter(next.Root);
            next.OnShow();
            if (focusedPage) next.Root.schedule.Execute(() => FocusFirst(next.Root)).StartingIn(20);
            RefreshAccessibility();
        }
        internal static bool CanFocus(VisualElement element)
        {
            if (!element.focusable || !element.enabledInHierarchy || element.panel == null) return false;
            for (var node = element; node != null; node = node.parent)
                if (node.resolvedStyle.display == DisplayStyle.None || node.resolvedStyle.visibility == Visibility.Hidden) return false;
            return element.ClassListContains("dw-action") || element is TextField || element is Button;
        }
        void FocusFirst(VisualElement scope)
        {
            foreach (var element in scope.Query<VisualElement>().ToList())
                if (CanFocus(element)) { element.Focus(); return; }
        }
        void OnNavigationMove(NavigationMoveEvent evt)
        {
            var focused = Root.panel?.focusController.focusedElement as VisualElement;
            if (overlayClosers.Count == 0 && focused != null) return;
            var scope = overlayClosers.Count > 0 ? overlay : current?.Root;
            if (scope == null) return;
            var candidates = scope.Query<VisualElement>().ToList().FindAll(CanFocus);
            if (candidates.Count == 0) return;
            var index = candidates.IndexOf(focused);
            var backwards = evt.direction == NavigationMoveEvent.Direction.Up || evt.direction == NavigationMoveEvent.Direction.Left;
            var next = index < 0 ? 0 : (index + (backwards ? -1 : 1) + candidates.Count) % candidates.Count;
            Root.panel.focusController.IgnoreEvent(evt);
            candidates[next].Focus(); evt.StopPropagation();
        }
        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (Root.panel?.focusController.focusedElement != null) return;
            FocusFirst(overlayClosers.Count > 0 ? overlay : current?.Root);
            Root.panel?.focusController.IgnoreEvent(evt);
        }
        void OnNavigationCancel(NavigationCancelEvent evt) { Back(); evt.StopPropagation(); }
        public void RefreshAccessibility() => Root.schedule.Execute(() => screenReader.Refresh(overlayClosers.Count > 0 ? overlay : safe)).StartingIn(80);
        public void OnLanguageConfirmed() { StartWithLanguage = false; Rebuild(); Navigate(Routes.Main); }
        public MainPage Main => main ??= (MainPage)Create(Routes.Main);
        public void Back()
        {
            if (overlayClosers.Count > 0) { overlayClosers.Pop()?.Invoke(); return; }
            if (current != null && current.HandleBack()) return;
            if (current != null && current.Route != Routes.Main && !(current.Route == Routes.Language && StartWithLanguage)) { Navigate(Routes.Main); return; }
            Native.I.MoveTaskToBack();
        }
        public void Update() { current?.OnUpdate(); }
        public void OnAppBackground() => main?.CancelPendingRequests();
        public void OpenOverlay(VisualElement panel, Action onClosed, bool sheet)
        {
            var previousFocus = Root.panel?.focusController.focusedElement as VisualElement;
            var scrim = new VisualElement(); scrim.AddToClassList("dw-scrim"); scrim.AddToClassList("dw-scrim--hidden");
            ThemeBinder.On(scrim, (e, p) => e.style.backgroundColor = new Color(0, 0, 0, p.IsLight ? 0.35f : 0.55f));
            var host = new VisualElement(); host.AddToClassList(sheet ? "dw-sheethost" : "dw-dialoghost"); host.pickingMode = PickingMode.Ignore;
            panel.AddToClassList(sheet ? "dw-sheet" : "dw-dialog"); panel.AddToClassList("dw-panel--hidden");
            host.Add(panel); overlay.Add(scrim); overlay.Add(host);
            bool closed = false;
            Action close = null;
            close = () =>
            {
                if (closed) return; closed = true;
                scrim.AddToClassList("dw-scrim--hidden"); panel.AddToClassList("dw-panel--hidden");
                overlay.schedule.Execute(() => { scrim.RemoveFromHierarchy(); host.RemoveFromHierarchy(); ThemeBinder.Prune(); if (previousFocus?.panel != null) previousFocus.Focus(); RefreshAccessibility(); }).StartingIn(260);
                onClosed?.Invoke();
            };
            panel.userData = close;
            scrim.AddManipulator(new Clickable(() => { Native.I.Haptic(Haptic.LongPress); CloseTop(); }));
            overlayClosers.Push(close);
            overlay.schedule.Execute(() => { scrim.RemoveFromClassList("dw-scrim--hidden"); panel.RemoveFromClassList("dw-panel--hidden"); FocusFirst(panel); RefreshAccessibility(); }).StartingIn(20);
        }
        public void CloseTop() { if (overlayClosers.Count > 0) overlayClosers.Pop()?.Invoke(); }
        public void Close(VisualElement panel)
        {
            if (!(panel.userData is Action a)) return;
            var tmp = new List<Action>(overlayClosers); tmp.Remove(a); tmp.Reverse(); overlayClosers.Clear(); foreach (var x in tmp) overlayClosers.Push(x);
            a();
        }
        void CloseAllOverlays() { while (overlayClosers.Count > 0) overlayClosers.Pop()?.Invoke(); overlay.Clear(); }
        public void Toast(string msg)
        {
            DwLog.Var(Module, "toast", msg);
            if (toastQueue.Count == MaxToasts) RemoveToast(toastQueue[0]);
            var t = Kit.Row(); t.AddToClassList("dw-toast"); t.AddToClassList("dw-toast--hidden");
            var l = new Label(msg); l.AddToClassList("dw-toast__text"); t.Add(l);
            ThemeBinder.On(t, (e, p) => { e.style.backgroundColor = p.IsLight ? ColorMath.Hex(0xFF1F2430) : ColorMath.Lerp(p.SurfaceVariant, Color.white, 0.08f); l.style.color = p.IsLight ? Color.white : p.OnSurface; ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.3f)); });
            toastQueue.Add(t); toastHost.Add(t);
            DwLog.Var(Module, "toastQueueCount", toastQueue.Count);
            t.schedule.Execute(() => t.RemoveFromClassList("dw-toast--hidden")).StartingIn(20);
            t.schedule.Execute(() => t.AddToClassList("dw-toast--hidden")).StartingIn(2600);
            t.schedule.Execute(() => RemoveToast(t)).StartingIn(3000);
        }
        void RemoveToast(VisualElement toast)
        {
            if (!toastQueue.Remove(toast)) return;
            toast.RemoveFromHierarchy();
            DwLog.Var(Module, "toastQueueCount", toastQueue.Count);
            foreach (var remaining in toastQueue)
            {
                remaining.AddToClassList("dw-toast--shift");
                remaining.schedule.Execute(() => remaining.RemoveFromClassList("dw-toast--shift")).StartingIn(20);
            }
        }
        public void ShowDialog(string title, string message, string confirm, string dismiss, Action onConfirm, Action onDismiss)
        {
            var d = Kit.Col();
            d.Add(Kit.Text(title, "dw-h2", p => p.OnSurface)); d.Add(Kit.Spacer(8)); d.Add(Kit.Text(message, "dw-body", p => p.OnSurfaceVariant)); d.Add(Kit.Spacer(20));
            bool handled = false;
            var row = Kit.Row(); row.AddToClassList("dw-dialog__actions");
            row.Add(new DwButton(dismiss, null, BtnKind.Ghost, () => { handled = true; Close(d); onDismiss?.Invoke(); }));
            row.Add(new DwButton(confirm, IconKind.Star, BtnKind.Primary, () => { handled = true; Close(d); onConfirm?.Invoke(); }));
            d.Add(row);
            ThemeBinder.On(d, (e, p) => { e.style.backgroundColor = p.Surface; ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.25f)); });
            OpenOverlay(d, () => { if (!handled) onDismiss?.Invoke(); }, false);
        }
        public void ShowImage(Texture2D tex)
        {
            var v = new VisualElement(); v.AddToClassList("dw-viewer");
            var img = new VisualElement(); img.AddToClassList("dw-viewer__img"); img.style.backgroundImage = new StyleBackground(tex); v.Add(img);
            float zoom = 1f; Vector2 offset = Vector2.zero; Vector2 dragStart = Vector2.zero; Vector2 offsetStart = Vector2.zero; int pointerId = -1;
            void ApplyTransform() { img.style.scale = new Scale(Vector3.one * zoom); img.style.translate = new Translate(offset.x, offset.y); }
            void ResetTransform() { zoom = 1f; offset = Vector2.zero; ApplyTransform(); }
            img.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (evt.clickCount >= 2) { ResetTransform(); evt.StopPropagation(); return; }
                pointerId = evt.pointerId; dragStart = evt.position; offsetStart = offset; img.CapturePointer(pointerId); evt.StopPropagation();
            });
            img.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (pointerId != evt.pointerId || zoom <= 1f) return;
                offset = offsetStart + (Vector2)evt.position - dragStart; img.style.translate = new Translate(offset.x, offset.y); evt.StopPropagation();
            });
            img.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (pointerId != evt.pointerId) return;
                if (img.HasPointerCapture(pointerId)) img.ReleasePointer(pointerId); pointerId = -1; evt.StopPropagation();
            });
            img.RegisterCallback<WheelEvent>(evt =>
            {
                zoom = Mathf.Clamp(zoom + (evt.delta.y < 0 ? 0.25f : -0.25f), 1f, 5f);
                if (zoom <= 1f) offset = Vector2.zero; ApplyTransform(); evt.StopPropagation();
            });
            v.RegisterCallback<PointerDownEvent>(evt => { if (evt.target == v) Close(v); });
            var close = Kit.IconButton(IconKind.Close, () => Close(v), 26, L10n.T("btn_close")); close.AddToClassList("dw-viewer__close"); v.Add(close);
            Kit.Action(v, () => { Native.I.Haptic(Haptic.LongPress); Close(v); });
            ThemeBinder.On(v, (e, p) => e.style.backgroundColor = ThemeBinder.A(p.Background, 0.97f));
            OpenOverlay(v, null, false);
        }
        public void ShowThemeSheet()
        {
            var s = Kit.Col(); s.Add(SheetHandle());
            s.Add(Kit.Text(L10n.T("theme_selection_title"), "dw-h2", p => p.OnSurface)); s.Add(Kit.Spacer(12));
            var sv = new ScrollView(ScrollViewMode.Vertical) { touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic, verticalScrollerVisibility = ScrollerVisibility.Hidden }; sv.AddToClassList("dw-sheet__scroll");
            var grid = new VisualElement(); grid.AddToClassList("dw-grid"); sv.Add(grid); s.Add(sv);
            foreach (var th in ThemeCatalog.All)
            {
                var t = th; var card = Kit.Col("dw-swatch");
                var bar = Kit.Row(); bar.AddToClassList("dw-swatch__bar");
                var b1 = new VisualElement(); b1.style.flexGrow = 2; b1.style.backgroundColor = t.Background;
                var b2 = new VisualElement(); b2.style.flexGrow = 1; b2.style.backgroundColor = t.Primary;
                var b3 = new VisualElement(); b3.style.flexGrow = 1; b3.style.backgroundColor = t.Secondary;
                bar.Add(b1); bar.Add(b2); bar.Add(b3); card.Add(bar);
                var lbl = Kit.Row(); lbl.AddToClassList("dw-swatch__label");
                var name = Kit.Text(L10n.T(t.NameKey), "dw-label", p => p.OnSurface); name.style.flexGrow = 1; name.style.flexShrink = 1; lbl.Add(name);
                var chk = new Icon(IconKind.Check, 16); lbl.Add(chk); card.Add(lbl);
                ThemeBinder.On(card, (e, p) => { bool sel = p.Id == t.Id; e.style.backgroundColor = p.SurfaceVariant; ThemeBinder.Border(e, sel ? p.Primary : ThemeBinder.A(p.Outline, 0.3f)); e.EnableInClassList("dw-swatch--selected", sel); chk.Tint = p.Primary; chk.style.display = sel ? DisplayStyle.Flex : DisplayStyle.None; });
                card.tooltip = L10n.T(t.NameKey);
                Kit.Action(card, () => { Native.I.Haptic(Haptic.LongPress); Close(s); applyTheme(t.Id); });
                grid.Add(card);
            }
            ThemeBinder.On(s, (e, p) => e.style.backgroundColor = p.Surface);
            OpenOverlay(s, null, true);
        }
        public void ShowMenu()
        {
            var s = Kit.Col(); s.Add(SheetHandle());
            void Item(IconKind ic, string label, Action a, bool accent = false)
            {
                var r = Kit.Row(); r.AddToClassList("dw-menuitem"); r.tooltip = label;
                r.Add(Kit.Icon(ic, 22, p => accent ? p.Secondary : p.Primary));
                var l = Kit.Text(label, "dw-menuitem__text", p => p.OnSurface); l.style.flexGrow = 1; r.Add(l);
                Kit.Action(r, () => { Native.I.Haptic(Haptic.LongPress); Close(s); a(); });
                s.Add(r);
            }
            Item(IconKind.Palette, L10n.T("theme_selection_title"), () => s.schedule.Execute(ShowThemeSheet).StartingIn(180), true);
            var div = new VisualElement(); div.AddToClassList("dw-divider"); ThemeBinder.On(div, (e, p) => e.style.backgroundColor = ThemeBinder.A(p.Outline, 0.25f)); s.Add(div);
            Item(IconKind.Help, L10n.T("nav_help"), () => Navigate(Routes.Help));
            Item(IconKind.Globe, L10n.T("nav_language"), () => Navigate(Routes.Language));
            Item(IconKind.Info, L10n.T("nav_info"), () => Navigate(Routes.Info));
            Item(IconKind.Link, L10n.T("nav_tinyurl"), () => Navigate(Routes.TinyUrl));
            ThemeBinder.On(s, (e, p) => e.style.backgroundColor = p.Surface);
            OpenOverlay(s, null, true);
        }
        public static VisualElement SheetHandle()
        {
            var h = new VisualElement(); h.AddToClassList("dw-sheet__handle");
            ThemeBinder.On(h, (e, p) => e.style.backgroundColor = ThemeBinder.A(p.OnSurfaceVariant, 0.5f));
            return h;
        }
    }
    public sealed class TopBar : VisualElement
    {
        readonly VisualElement back; readonly AppShell shell;
        public TopBar(AppShell shell)
        {
            this.shell = shell; AddToClassList("dw-topbar"); AddToClassList("dw-row");
            back = Kit.IconButton(IconKind.Back, shell.Back, 22, L10n.T("cd_back")); Add(back);
            var logo = new VisualElement(); logo.AddToClassList("dw-topbar__logo"); logo.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("UI/app_icon")); Add(logo);
            var title = Kit.Text(L10n.T("app_name"), "dw-topbar__title", p => p.OnSurface); title.style.flexGrow = 1; title.style.flexShrink = 1; Add(title);
            var menu = Kit.IconButton(IconKind.Menu, shell.ShowMenu, 22, L10n.T("menu_more")); Add(menu);
            ThemeBinder.On(this, (e, p) => { e.style.backgroundColor = ThemeBinder.A(p.Surface, p.IsLight ? 0.85f : 0.55f); e.style.borderBottomColor = ThemeBinder.A(p.Primary, 0.12f); });
        }
        public void Configure(Page p)
        {
            style.display = p.ShowTopBar ? DisplayStyle.Flex : DisplayStyle.None;
            back.style.display = p.ShowBack ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
