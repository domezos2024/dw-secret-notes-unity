using UnityEngine;
namespace DwSecretNotes.UI
{
    public static class Textures
    {
        static Texture2D glow;
        public static Texture2D Glow
        {
            get
            {
                if (glow != null) return glow;
                const int n = 128; glow = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - d); a = a * a * (3 - 2 * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
                glow.SetPixels32(px); glow.Apply(); return glow;
            }
        }
        public static Texture2D Gradient(Color a, Color b, Texture2D reuse = null)
        {
            var t = reuse ?? new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int x = 0; x < 64; x++) t.SetPixel(x, 0, Color.Lerp(a, b, x / 63f));
            t.Apply(); return t;
        }
    }
}
