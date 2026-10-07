using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public enum IconKind { Lock, Unlock, Copy, Share, Image, Plus, Close, Check, Back, Menu, Star, Palette, Info, Globe, Help, Link, Paste, Shield, External, Sparkle }
    public sealed class Icon : VisualElement
    {
        IconKind kind; Color tint = Color.white;
        public Icon(IconKind kind, float size = 22)
        {
            this.kind = kind; pickingMode = PickingMode.Ignore; AddToClassList("dw-icon");
            style.width = size; style.height = size; style.flexShrink = 0;
            generateVisualContent += Draw;
        }
        public IconKind Kind { get => kind; set { kind = value; MarkDirtyRepaint(); } }
        public Color Tint { get => tint; set { tint = value; MarkDirtyRepaint(); } }
        void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect; if (r.width < 1) return;
            float s = Mathf.Min(r.width, r.height) / 24f; var o = new Vector2(r.x + (r.width - 24 * s) / 2, r.y + (r.height - 24 * s) / 2);
            var p = mgc.painter2D; p.strokeColor = tint; p.fillColor = tint; p.lineWidth = 2f * s; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            Vector2 P(float x, float y) => o + new Vector2(x, y) * s;
            void Line(float x1, float y1, float x2, float y2) { p.BeginPath(); p.MoveTo(P(x1, y1)); p.LineTo(P(x2, y2)); p.Stroke(); }
            void Circle(float cx, float cy, float rad, bool fill = false) { p.BeginPath(); p.Arc(P(cx, cy), rad * s, Angle.Degrees(0), Angle.Degrees(360)); p.ClosePath(); if (fill) p.Fill(); else p.Stroke(); }
            void RRect(float x, float y, float w, float h, float rad, bool fill = false)
            {
                p.BeginPath(); p.MoveTo(P(x + rad, y)); p.LineTo(P(x + w - rad, y)); p.ArcTo(P(x + w, y), P(x + w, y + rad), rad * s);
                p.LineTo(P(x + w, y + h - rad)); p.ArcTo(P(x + w, y + h), P(x + w - rad, y + h), rad * s); p.LineTo(P(x + rad, y + h));
                p.ArcTo(P(x, y + h), P(x, y + h - rad), rad * s); p.LineTo(P(x, y + rad)); p.ArcTo(P(x, y), P(x + rad, y), rad * s); p.ClosePath();
                if (fill) p.Fill(); else p.Stroke();
            }
            switch (kind)
            {
                case IconKind.Lock: case IconKind.Unlock:
                    RRect(5, 11, 14, 10, 2.5f);
                    p.BeginPath(); p.MoveTo(P(8, 11)); p.LineTo(P(8, 7.5f)); p.Arc(P(12, 7.5f), 4 * s, Angle.Degrees(180), Angle.Degrees(360));
                    p.LineTo(P(16, kind == IconKind.Lock ? 11 : 8.5f)); p.Stroke();
                    if (kind == IconKind.Unlock) { p.BeginPath(); p.MoveTo(P(8, 11)); p.LineTo(P(8, 7.5f)); p.Stroke(); }
                    Circle(12, 16, 1.2f, true); break;
                case IconKind.Copy: RRect(8, 8, 12, 13, 2); p.BeginPath(); p.MoveTo(P(16, 5)); p.LineTo(P(16, 4.5f)); p.ArcTo(P(16, 3), P(14.5f, 3), 1.5f * s); p.LineTo(P(5.5f, 3)); p.ArcTo(P(4, 3), P(4, 4.5f), 1.5f * s); p.LineTo(P(4, 15)); p.ArcTo(P(4, 16.5f), P(5.5f, 16.5f), 1.5f * s); p.Stroke(); break;
                case IconKind.Share: Circle(18, 5, 2.5f); Circle(6, 12, 2.5f); Circle(18, 19, 2.5f); Line(8.2f, 10.8f, 15.8f, 6.2f); Line(8.2f, 13.2f, 15.8f, 17.8f); break;
                case IconKind.Image: RRect(3, 4, 18, 16, 2.5f); Circle(9, 9.5f, 1.8f); p.BeginPath(); p.MoveTo(P(3.5f, 18)); p.LineTo(P(9, 13)); p.LineTo(P(13, 16.5f)); p.LineTo(P(16, 13.5f)); p.LineTo(P(20.5f, 18)); p.Stroke(); break;
                case IconKind.Plus: Line(12, 5, 12, 19); Line(5, 12, 19, 12); break;
                case IconKind.Close: Line(6, 6, 18, 18); Line(18, 6, 6, 18); break;
                case IconKind.Check: p.BeginPath(); p.MoveTo(P(5, 12.5f)); p.LineTo(P(10, 17.5f)); p.LineTo(P(19, 7)); p.Stroke(); break;
                case IconKind.Back: Line(5, 12, 19, 12); p.BeginPath(); p.MoveTo(P(11, 6)); p.LineTo(P(5, 12)); p.LineTo(P(11, 18)); p.Stroke(); break;
                case IconKind.Menu: Circle(12, 5.5f, 1.8f, true); Circle(12, 12, 1.8f, true); Circle(12, 18.5f, 1.8f, true); break;
                case IconKind.Star:
                    p.BeginPath();
                    for (int i = 0; i < 10; i++) { float a = Mathf.Deg2Rad * (-90 + i * 36), rad = i % 2 == 0 ? 10 : 4.2f; var pt = P(12 + Mathf.Cos(a) * rad, 12.8f + Mathf.Sin(a) * rad); if (i == 0) p.MoveTo(pt); else p.LineTo(pt); }
                    p.ClosePath(); p.Fill(); break;
                case IconKind.Palette:
                    p.BeginPath(); p.Arc(P(12, 12), 9 * s, Angle.Degrees(60), Angle.Degrees(400)); p.Stroke();
                    Circle(7.5f, 11, 1.4f, true); Circle(10.5f, 7, 1.4f, true); Circle(15, 7.5f, 1.4f, true); Circle(17, 11.5f, 1.4f, true); Circle(14, 17, 2f); break;
                case IconKind.Info: Circle(12, 12, 9); Line(12, 11, 12, 16.5f); Circle(12, 7.8f, 1.2f, true); break;
                case IconKind.Help:
                    Circle(12, 12, 9); p.BeginPath(); p.Arc(P(12, 9.5f), 2.8f * s, Angle.Degrees(190), Angle.Degrees(400)); p.LineTo(P(12, 14)); p.Stroke(); Circle(12, 17, 1.2f, true); break;
                case IconKind.Globe:
                    Circle(12, 12, 9); Line(3, 12, 21, 12);
                    p.BeginPath(); p.MoveTo(P(12, 3)); p.BezierCurveTo(P(16.5f, 7), P(16.5f, 17), P(12, 21)); p.BezierCurveTo(P(7.5f, 17), P(7.5f, 7), P(12, 3)); p.Stroke(); break;
                case IconKind.Link:
                    RRect(2.5f, 8.5f, 11, 7, 3.5f); RRect(10.5f, 8.5f, 11, 7, 3.5f); break;
                case IconKind.Paste: RRect(5, 5, 14, 16, 2.5f); RRect(9, 3, 6, 4, 1.2f, true); Line(9, 12, 15, 12); Line(9, 16, 13, 16); break;
                case IconKind.Shield:
                    p.BeginPath(); p.MoveTo(P(12, 2.5f)); p.LineTo(P(20, 5.5f)); p.LineTo(P(20, 11)); p.BezierCurveTo(P(20, 16), P(16.5f, 19.5f), P(12, 21.5f)); p.BezierCurveTo(P(7.5f, 19.5f), P(4, 16), P(4, 11)); p.LineTo(P(4, 5.5f)); p.ClosePath(); p.Stroke();
                    p.BeginPath(); p.MoveTo(P(8.5f, 12)); p.LineTo(P(11, 14.5f)); p.LineTo(P(15.5f, 9.5f)); p.Stroke(); break;
                case IconKind.External:
                    p.BeginPath(); p.MoveTo(P(11, 4)); p.LineTo(P(6, 4)); p.ArcTo(P(4, 4), P(4, 6), 2 * s); p.LineTo(P(4, 18)); p.ArcTo(P(4, 20), P(6, 20), 2 * s); p.LineTo(P(18, 20)); p.ArcTo(P(20, 20), P(20, 18), 2 * s); p.LineTo(P(20, 13)); p.Stroke();
                    Line(13, 11, 20, 4); p.BeginPath(); p.MoveTo(P(14.5f, 4)); p.LineTo(P(20, 4)); p.LineTo(P(20, 9.5f)); p.Stroke(); break;
                case IconKind.Sparkle:
                    p.BeginPath(); p.MoveTo(P(12, 2)); p.BezierCurveTo(P(13, 9), P(15, 11), P(22, 12)); p.BezierCurveTo(P(15, 13), P(13, 15), P(12, 22)); p.BezierCurveTo(P(11, 15), P(9, 13), P(2, 12)); p.BezierCurveTo(P(9, 11), P(11, 9), P(12, 2)); p.ClosePath(); p.Fill(); break;
            }
        }
    }
    public sealed class CountdownRing : VisualElement
    {
        float progress = 1; Color track = Color.gray, bar = Color.white; public readonly Label Value;
        public CountdownRing(float size)
        {
            style.width = size; style.height = size; style.alignItems = Align.Center; style.justifyContent = Justify.Center; style.flexShrink = 0;
            Value = new Label { pickingMode = PickingMode.Ignore }; Value.AddToClassList("dw-ring__value"); Add(Value);
            generateVisualContent += Draw;
        }
        public void Set(float p, Color trackColor, Color barColor) { progress = Mathf.Clamp01(p); track = trackColor; bar = barColor; MarkDirtyRepaint(); }
        void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect; float w = Mathf.Max(3, r.width * 0.08f), rad = Mathf.Min(r.width, r.height) / 2 - w; var c = r.center; var p = mgc.painter2D;
            p.lineWidth = w; p.lineCap = LineCap.Round;
            p.strokeColor = track; p.BeginPath(); p.Arc(c, rad, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
            if (progress <= 0.001f) return;
            p.strokeColor = bar; p.BeginPath(); p.Arc(c, rad, Angle.Degrees(-90), Angle.Degrees(-90 + 360 * progress)); p.Stroke();
        }
    }
    public sealed class Spinner : VisualElement
    {
        Color tint = Color.white; float angle; readonly IVisualElementScheduledItem tick;
        public Spinner(float size)
        {
            style.width = size; style.height = size; style.flexShrink = 0; pickingMode = PickingMode.Ignore;
            generateVisualContent += mgc =>
            {
                var r = contentRect; var p = mgc.painter2D; p.lineWidth = Mathf.Max(2, r.width * 0.12f); p.lineCap = LineCap.Round; p.strokeColor = tint;
                p.BeginPath(); p.Arc(r.center, r.width / 2 - p.lineWidth, Angle.Degrees(angle), Angle.Degrees(angle + 270)); p.Stroke();
            };
            tick = schedule.Execute(() => { angle = (angle + 12) % 360; MarkDirtyRepaint(); }).Every(16);
        }
        public Color Tint { get => tint; set { tint = value; MarkDirtyRepaint(); } }
    }
}
