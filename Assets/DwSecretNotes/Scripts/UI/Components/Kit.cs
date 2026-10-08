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
            if (icon.HasValue) { Icon = new Icon(icon.Value, 20); Add(Icon); }
            spinner = new Spinner(18); spinner.style.display = DisplayStyle.None; Add(spinner);
            Text = new Label(text) { pickingMode = PickingMode.Ignore }; Text.AddToClassList("dw-btn__text"); Add(Text);
            if (string.IsNullOrEmpty(text)) Text.style.display = DisplayStyle.None;
            this.AddManipulator(new Clickable(() => { if (!enabledSelf || busy) return; Native.I.Haptic(Haptic.LongPress); click?.Invoke(); }));
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
                default: bg = Color.clear; fg = p.Primary; break;
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
        public readonly TextField Input; readonly Label placeholder; public event Action<string> Changed;
        public DwField(string hint, bool multiline)
        {
            AddToClassList("dw-field");
            Input = new TextField { multiline = multiline }; Input.AddToClassList("dw-field__input");
            if (multiline) { Input.AddToClassList("dw-field__input--multi"); Input.verticalScrollerVisibility = ScrollerVisibility.Auto; }
            placeholder = new Label(hint) { pickingMode = PickingMode.Ignore }; placeholder.AddToClassList("dw-field__placeholder");
            Add(Input); Add(placeholder);
            Input.RegisterValueChangedCallback(e => { placeholder.style.display = string.IsNullOrEmpty(e.newValue) ? DisplayStyle.Flex : DisplayStyle.None; Changed?.Invoke(e.newValue); });
            Input.RegisterCallback<FocusInEvent>(_ => AddToClassList("dw-field--focus"));
            Input.RegisterCallback<FocusOutEvent>(_ => RemoveFromClassList("dw-field--focus"));
            ThemeBinder.On(this, (e, p) =>
            {
                e.style.backgroundColor = ThemeBinder.A(p.SurfaceVariant, 0.9f);
                ThemeBinder.Border(e, ThemeBinder.A(p.Primary, 0.28f));
                e.placeholder.style.color = p.OnSurfaceVariant;
                foreach (var t in e.Input.Query<TextElement>().ToList()) t.style.color = p.OnSurface;
#pragma warning disable CS0618
                e.Input.textSelection.cursorColor = p.Primary; e.Input.textSelection.selectionColor = ThemeBinder.A(p.Primary, 0.35f);
#pragma warning restore CS0618
            });
        }
        public string Value { get => Input.value; set { Input.SetValueWithoutNotify(value ?? ""); placeholder.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.Flex : DisplayStyle.None; } }
        public string Hint { set => placeholder.text = value; }
    }
    public static class Kit
    {
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
            ThemeBinder.On(b, (e, p) => { var fg = success ? p.Primary : p.Error; e.style.backgroundColor = success ? ThemeBinder.A(p.PrimaryContainer, 0.75f) : ThemeBinder.A(p.ErrorContainer, 0.8f); ThemeBinder.Border(e, ThemeBinder.A(fg, 0.35f)); ic.Tint = fg; l.style.color = success ? (p.IsLight ? p.OnPrimaryContainer : fg) : (p.IsLight ? p.Error : ColorMath.Lerp(p.Error, Color.white, 0.3f)); });
            return b;
        }
        public static VisualElement IconButton(IconKind k, Action onClick, float size = 22)
        {
            var b = new VisualElement(); b.AddToClassList("dw-iconbtn");
            var ic = new Icon(k, size); b.Add(ic);
            b.AddManipulator(new Clickable(() => { Native.I.Haptic(Haptic.LongPress); onClick(); }));
            ThemeBinder.On(b, (e, p) => ic.Tint = p.OnSurface);
            return b;
        }
        public static Label Link(string text, Action onClick)
        {
            var l = Text(text, "dw-link", p => p.Primary); l.AddManipulator(new Clickable(onClick)); return l;
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
