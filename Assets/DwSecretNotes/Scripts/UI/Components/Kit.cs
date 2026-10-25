using System;
using DwSecretNotes.Core;
using DwSecretNotes.Platform;
using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public enum BtnKind { Primary, Tonal, Ghost, Accent }
    public sealed class DwButton : VisualElement
    {
        public readonly Label Text; public readonly Icon Icon; readonly Spinner spinner; readonly BtnKind kind; Action click; bool busy; Texture2D grad;
        public DwButton(string text, IconKind? icon, BtnKind kind, Action onClick)
        {
            this.kind = kind; click = onClick; AddToClassList("dw-btn"); AddToClassList("dw-btn--" + kind.ToString().ToLowerInvariant()); AddToClassList("dw-row");
            tooltip = !string.IsNullOrWhiteSpace(text) ? text : icon == IconKind.Paste ? L10n.T("btn_paste") : "";
            if (icon.HasValue) { Icon = new Icon(icon.Value, 20); Add(Icon); }
            spinner = new Spinner(18); spinner.style.display = DisplayStyle.None; Add(spinner);
            Text = new Label(text) { pickingMode = PickingMode.Ignore }; Text.AddToClassList("dw-btn__text"); Add(Text);
            if (string.IsNullOrEmpty(text)) Text.style.display = DisplayStyle.None;
            Kit.Action(this, () => { if (!enabledSelf || busy) return; Native.I.Haptic(Haptic.LongPress); click?.Invoke(); });
            ThemeBinder.On(this, (e, p) => e.Paint(p));
        }
        public void SetAction(Action a) => click = a;
        public bool Busy { get => busy; set { busy = value; spinner.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; if (Icon != null) Icon.style.display = value ? DisplayStyle.None : DisplayStyle.Flex; EnableInClassList("dw-btn--busy", value); } }
        void Paint(ThemePalette p)
        {
            Color bg, fg, border = Color.clear;
            switch (kind)
            {
                case BtnKind.Primary: bg = Color.white; fg = p.OnSecondary; break;
                case BtnKind.Accent: bg = p.Primary; fg = p.OnPrimary; break;
                case BtnKind.Tonal: bg = p.SurfaceVariant; fg = p.OnSurface; border = ThemeBinder.A(p.Primary, 0.22f); break;
                default: bg = Color.clear; fg = p.AccentText; break;
            }
            if (kind == BtnKind.Primary)
            {
                grad = Textures.Gradient(p.Secondary, ColorMath.Lerp(p.Secondary, p.Primary, 0.38f), grad);
                style.backgroundImage = new StyleBackground(grad); style.backgroundColor = p.Secondary;
            }
            else style.backgroundColor = bg;
            ThemeBinder.Border(this, border);
            Text.style.color = fg; if (Icon != null) Icon.Tint = fg; spinner.Tint = fg;
        }
    }
    public sealed class DwField : VisualElement
    {
        public readonly TextField Input; readonly Label placeholder; readonly Button clearButton, revealButton; readonly Icon revealIcon; readonly bool sensitive; bool masked;
        public event Action<string> Changed;
        public DwField(string hint, bool multiline, bool sensitive = false, string key = null)
        {
            this.sensitive = sensitive; masked = sensitive;
            AddToClassList("dw-field");
            Input = new TextField { multiline = multiline, isPasswordField = sensitive, tooltip = hint }; Input.AddToClassList("dw-field__input");
            Input.focusable = true;
            Input.viewDataKey = key ?? "field-" + Kit.NextActionId();
            Input.name = "dw-field-input";
            if (multiline) { Input.AddToClassList("dw-field__input--multi"); Input.verticalScrollerVisibility = ScrollerVisibility.Auto; }
            placeholder = new Label(hint) { pickingMode = PickingMode.Ignore }; placeholder.AddToClassList("dw-field__placeholder");
            Add(Input); Add(placeholder);
            clearButton = new Button(() => { Native.I.Haptic(Haptic.LongPress); Input.value = ""; Input.Focus(); }) { tooltip = L10n.T("btn_clear") };
            clearButton.name = "clear-input"; clearButton.AddToClassList("dw-field__action");
            clearButton.Add(new Icon(IconKind.Close, 18));
            clearButton.viewDataKey = Input.viewDataKey + "-clear";
            var actions = new VisualElement(); actions.AddToClassList("dw-field__actions");
            actions.Add(clearButton);
            if (sensitive)
            {
                revealButton = new Button(() => { Native.I.Haptic(Haptic.LongPress); ToggleMask(); }) { tooltip = L10n.T("btn_show") };
                revealIcon = new Icon(IconKind.Unlock, 18); revealButton.Add(revealIcon);
                revealButton.name = "toggle-input-visibility"; revealButton.AddToClassList("dw-field__action"); actions.Add(revealButton);
                revealButton.viewDataKey = Input.viewDataKey + "-reveal";
            }
            Add(actions);
            Kit.FocusRing(this);
            Kit.FocusRing(clearButton);
            if (revealButton != null) Kit.FocusRing(revealButton);
            Input.RegisterValueChangedCallback(e => { placeholder.style.display = string.IsNullOrEmpty(e.newValue) ? DisplayStyle.Flex : DisplayStyle.None; UpdateActions(e.newValue); Changed?.Invoke(e.newValue); });
            Input.RegisterCallback<FocusInEvent>(_ => AddToClassList("dw-field--focus"));
            Input.RegisterCallback<FocusOutEvent>(_ => RemoveFromClassList("dw-field--focus"));
            ThemeBinder.On(this, (e, p) =>
            {
                e.style.backgroundColor = ThemeBinder.A(p.SurfaceVariant, 0.9f);
                ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.28f));
                e.placeholder.style.color = p.OnSurfaceVariant;
                foreach (var t in e.Input.Query<TextElement>().ToList()) t.style.color = p.OnSurface;
                var actionBackground = ColorMath.Lerp(p.SurfaceVariant, p.Primary, 0.1f);
                var actionText = ColorMath.EnsureReadable(p.AccentText, actionBackground, 4.5, p.OnSurface);
                foreach (var action in e.Query<Button>().ToList())
                {
                    action.style.backgroundColor = actionBackground;
                    action.style.color = actionText;
                    foreach (var icon in action.Query<Icon>().ToList()) icon.Tint = actionText;
                    ThemeBinder.Border(action, ThemeBinder.A(p.Outline, 0.65f));
                }
#pragma warning disable CS0618
                e.Input.textSelection.cursorColor = p.Primary; e.Input.textSelection.selectionColor = ThemeBinder.A(p.Primary, 0.35f);
#pragma warning restore CS0618
            });
            UpdateActions(Input.value);
        }
        void ToggleMask()
        {
            masked = !masked;
            Input.isPasswordField = masked;
            var key = masked ? "btn_show" : "btn_hide";
            revealIcon.Kind = masked ? IconKind.Unlock : IconKind.Lock;
            revealButton.tooltip = L10n.T(key);
            Input.Focus();
        }
        void UpdateActions(string value) { var hasValue = !string.IsNullOrEmpty(value); clearButton.SetEnabled(hasValue); clearButton.style.display = hasValue ? DisplayStyle.Flex : DisplayStyle.None; }
        public string Value { get => Input.value; set { Input.SetValueWithoutNotify(value ?? ""); placeholder.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.Flex : DisplayStyle.None; UpdateActions(value); } }
        public string Hint { set { placeholder.text = value; Input.tooltip = value; } }
    }
    public static class Kit
    {
        static int nextActionId;
        public static int NextActionId() => ++nextActionId;
        public static void FocusRing(VisualElement target)
        {
            if (target is TextElement)
            {
                target.style.borderTopWidth = 2; target.style.borderBottomWidth = 2;
                target.style.borderLeftWidth = 2; target.style.borderRightWidth = 2;
                target.RegisterCallback<FocusInEvent>(_ => { target.AddToClassList("dw-focus-text"); ThemeBinder.Border(target, ThemeBinder.A(ThemeBinder.Current.Primary, 0.8f)); });
                target.RegisterCallback<FocusOutEvent>(_ => { target.RemoveFromClassList("dw-focus-text"); ThemeBinder.Border(target, Color.clear); });
                ThemeBinder.On(target, (e, p) => ThemeBinder.Border(e, e.ClassListContains("dw-focus-text") ? ThemeBinder.A(p.Primary, 0.8f) : Color.clear));
                return;
            }
            var ring = new VisualElement { pickingMode = PickingMode.Ignore };
            ring.AddToClassList("dw-focus-ring");
            target.Add(ring);
            target.RegisterCallback<FocusInEvent>(_ => ring.AddToClassList("dw-focus-ring--visible"));
            target.RegisterCallback<FocusOutEvent>(_ => ring.RemoveFromClassList("dw-focus-ring--visible"));
            ThemeBinder.On(ring, (e, p) => ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.8f)));
        }
        public static void Action(VisualElement target, Action action)
        {
            target.focusable = true;
            target.AddToClassList("dw-action");
            if (string.IsNullOrEmpty(target.viewDataKey)) target.viewDataKey = "action-" + NextActionId();
            target.AddManipulator(new Clickable(action));
            target.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                if (!target.enabledInHierarchy) return;
                action();
                evt.StopPropagation();
            });
            FocusRing(target);
        }
        public static Label Text(string t, string cls, Func<ThemePalette, Color> color = null)
        {
            var l = new Label(t); foreach (var c in cls.Split(' ')) if (c.Length > 0) l.AddToClassList(c);
            if (color != null) ThemeBinder.On(l, (e, p) => e.style.color = color(p));
            return l;
        }
        public static VisualElement Row(params VisualElement[] kids) { var r = new VisualElement(); r.AddToClassList("dw-row"); foreach (var k in kids) r.Add(k); return r; }
        public static VisualElement Col(string cls = null) { var c = new VisualElement(); c.AddToClassList("dw-col"); if (cls != null) foreach (var x in cls.Split(' ')) c.AddToClassList(x); return c; }
        public static VisualElement Spacer(float h) { var s = new VisualElement(); s.style.height = h; s.style.flexShrink = 0; return s; }
        public static VisualElement Flex() { var s = new VisualElement(); s.style.flexGrow = 1; return s; }
        public static VisualElement Card(Func<ThemePalette, Color> border = null)
        {
            var c = Col("dw-card");
            ThemeBinder.On(c, (e, p) => { e.style.backgroundColor = ThemeBinder.A(p.Surface, p.IsLight ? 0.94f : 0.82f); ThemeBinder.Border(e, border != null ? border(p) : ThemeBinder.A(p.Primary, 0.18f)); });
            return c;
        }
        public static Icon Icon(IconKind k, float size, Func<ThemePalette, Color> color) { var i = new Icon(k, size); ThemeBinder.On(i, (e, p) => e.Tint = color(p)); return i; }
        public static VisualElement Banner(string text, bool success)
        {
            var b = Row(); b.AddToClassList("dw-banner");
            var ic = new Icon(success ? IconKind.Check : IconKind.Info, 18); var l = new Label(text); l.AddToClassList("dw-banner__text");
            b.Add(ic); b.Add(l);
            ThemeBinder.On(b, (e, p) => { var fg = success ? p.Primary : p.Error; e.style.backgroundColor = success ? ThemeBinder.A(p.PrimaryContainer, 0.75f) : ThemeBinder.A(p.ErrorContainer, 0.8f); ThemeBinder.Border(e, ThemeBinder.A(fg, 0.35f)); ic.Tint = fg; l.style.color = success ? p.OnPrimaryContainer : (p.IsLight ? p.Error : ColorMath.Lerp(p.Error, Color.white, 0.3f)); });
            return b;
        }
        public static VisualElement IconButton(IconKind k, Action onClick, float size = 22, string label = null)
        {
            var b = new VisualElement(); b.AddToClassList("dw-iconbtn");
            b.tooltip = label ?? k.ToString();
            var ic = new Icon(k, size); b.Add(ic);
            Action(b, () => { Native.I.Haptic(Haptic.LongPress); onClick(); });
            ThemeBinder.On(b, (e, p) => ic.Tint = p.OnSurface);
            return b;
        }
        public static Label Link(string text, Action onClick)
        {
            var l = Text(text, "dw-link", p => p.AccentText); l.tooltip = text; Action(l, () => { Native.I.Haptic(Haptic.LongPress); onClick(); }); return l;
        }
        public const float MaxContentWidth = 720;
        public static T CenterColumn<T>(T e, float minPad = 16) where T : VisualElement
        {
            e.RegisterCallback<GeometryChangedEvent>(ev =>
            {
                float pad = Mathf.Max(minPad, (ev.newRect.width - MaxContentWidth) / 2f);
                if (Mathf.Abs(e.resolvedStyle.paddingLeft - pad) > 0.5f) { e.style.paddingLeft = pad; e.style.paddingRight = pad; }
            });
            return e;
        }
        public static void Enter(VisualElement e, int delayMs = 0)
        {
            e.AddToClassList("dw-enter");
            e.schedule.Execute(() => e.RemoveFromClassList("dw-enter")).StartingIn(Mathf.Max(30, delayMs));
        }
    }
}
