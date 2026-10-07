using DwSecretNotes.Core;
using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public sealed class Aurora : VisualElement
    {
        readonly VisualElement[] blobs = new VisualElement[3]; float t;
        public Aurora()
        {
            AddToClassList("dw-aurora"); pickingMode = PickingMode.Ignore;
            for (int i = 0; i < blobs.Length; i++) { var b = new VisualElement { pickingMode = PickingMode.Ignore }; b.AddToClassList("dw-aurora__blob"); b.style.backgroundImage = new StyleBackground(Textures.Glow); blobs[i] = b; Add(b); }
            ThemeBinder.On(this, (e, p) =>
            {
                e.style.backgroundColor = p.Background;
                e.blobs[0].style.unityBackgroundImageTintColor = ThemeBinder.A(p.Primary, p.IsLight ? 0.16f : 0.22f);
                e.blobs[1].style.unityBackgroundImageTintColor = ThemeBinder.A(p.Secondary, p.IsLight ? 0.12f : 0.14f);
                e.blobs[2].style.unityBackgroundImageTintColor = ThemeBinder.A(ColorMath.Lerp(p.Primary, p.Secondary, 0.5f), p.IsLight ? 0.08f : 0.12f);
            });
            schedule.Execute(Tick).Every(33);
        }
        void Tick()
        {
            t += 0.033f; var r = contentRect; if (r.width < 1) return;
            float w = r.width, h = r.height, s = Mathf.Max(w, h);
            Place(blobs[0], s * 1.0f, w * (-0.25f + 0.08f * Mathf.Sin(t * 0.21f)), h * (-0.18f + 0.05f * Mathf.Cos(t * 0.17f)));
            Place(blobs[1], s * 0.9f, w * (0.45f + 0.07f * Mathf.Cos(t * 0.19f)), h * (0.55f + 0.06f * Mathf.Sin(t * 0.13f)));
            Place(blobs[2], s * 0.7f, w * (0.35f + 0.12f * Mathf.Sin(t * 0.11f + 1)), h * (0.18f + 0.08f * Mathf.Cos(t * 0.15f + 2)));
        }
        static void Place(VisualElement b, float size, float x, float y) { b.style.width = size; b.style.height = size; b.style.translate = new Translate(x, y); }
    }
}
