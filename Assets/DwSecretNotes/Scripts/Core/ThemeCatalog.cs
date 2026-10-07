using System;
using System.Collections.Generic;
using UnityEngine;
namespace DwSecretNotes.Core
{
    public sealed class ThemePalette
    {
        public string Id, NameKey; public bool IsLight;
        public Color Background, Surface, SurfaceVariant, Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer, Secondary, OnSecondary, OnSurface, OnSurfaceVariant, Outline, Tertiary, Error, ErrorContainer, Accent;
    }
    public static class ColorMath
    {
        public static Color Hex(uint argb) => new Color(((argb >> 16) & 0xFF) / 255f, ((argb >> 8) & 0xFF) / 255f, (argb & 0xFF) / 255f, ((argb >> 24) & 0xFF) / 255f);
        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
        static float ToLin(float c) => c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        static float ToSrgb(float c) => c <= 0.0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;
        public static float Luminance(Color c) => 0.2126f * ToLin(c.r) + 0.7152f * ToLin(c.g) + 0.0722f * ToLin(c.b);
        public static double Contrast(Color a, Color b) { double l1 = Luminance(a), l2 = Luminance(b); return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05); }
        static Vector3 ToOklab(Color c)
        {
            float r = ToLin(c.r), g = ToLin(c.g), b = ToLin(c.b);
            float l = Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b), m = Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b), s = Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);
            return new Vector3(0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s, 1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s, 0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s);
        }
        static Color FromOklab(Vector3 o, float a)
        {
            float l = o.x + 0.3963377774f * o.y + 0.2158037573f * o.z, m = o.x - 0.1055613458f * o.y - 0.0638541728f * o.z, s = o.x - 0.0894841775f * o.y - 1.2914855480f * o.z;
            l *= l * l; m *= m * m; s *= s * s;
            float r = 4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s, g = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s, b = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s;
            return new Color(Mathf.Clamp01(ToSrgb(Mathf.Clamp01(r))), Mathf.Clamp01(ToSrgb(Mathf.Clamp01(g))), Mathf.Clamp01(ToSrgb(Mathf.Clamp01(b))), a);
        }
        static float Cbrt(float v) => v < 0 ? -Mathf.Pow(-v, 1f / 3f) : Mathf.Pow(v, 1f / 3f);
        public static Color Lerp(Color a, Color b, float t) => FromOklab(Vector3.LerpUnclamped(ToOklab(a), ToOklab(b), t), Mathf.Lerp(a.a, b.a, t));
        public static Color BestOn(Color c) => Contrast(Color.black, c) >= Contrast(Color.white, c) ? Color.black : Color.white;
        public static Color EnsureReadable(Color baseC, Color bg, double min, Color target)
        {
            if (Contrast(baseC, bg) >= min) return baseC;
            float lo = 0, hi = 1; var res = target;
            for (int i = 0; i < 14; i++) { float mid = (lo + hi) / 2; var cand = Lerp(baseC, target, mid); if (Contrast(cand, bg) >= min) { res = cand; hi = mid; } else lo = mid; }
            return res;
        }
        public static int ToArgbInt(Color c) { var c32 = (Color32)c; return (c32.a << 24) | (c32.r << 16) | (c32.g << 8) | c32.b; }
    }
    public static class ThemeCatalog
    {
        static readonly Color NearWhite = ColorMath.Hex(0xFFF2F2F5), NearBlack = ColorMath.Hex(0xFF1A1A1A);
        public static readonly Color Cyan = ColorMath.Hex(0xFF00D4FF), Gold = ColorMath.Hex(0xFFF0C040);
        public static readonly IReadOnlyList<ThemePalette> All;
        static ThemeCatalog()
        {
            All = new List<ThemePalette>
            {
                Classic(),
                Dark("dark", 0xFF121212, 0xFF1E1E1E, 0xFF00D4FF, 0xFFF0C040, 0xFF00D4FF),
                Light("light", 0xFFF7F7FA, 0xFFFFFFFF, 0xFF0077A6, 0xFFB8860B),
                Dark("midnight", 0xFF0F0817, 0xFF1B0E29, 0xFFBB86FC, 0xFF03DAC6),
                Dark("forest", 0xFF081009, 0xFF0F1F12, 0xFF81C784, 0xFFFFF176),
                Dark("ocean", 0xFF010B13, 0xFF031A29, 0xFF4FC3F7, 0xFFFFB74D),
                Dark("cyberpunk", 0xFF050505, 0xFF1A001A, 0xFFFCEE09, 0xFF00E5FF),
                Dark("dracula", 0xFF282A36, 0xFF343746, 0xFFBD93F9, 0xFFFF79C6),
                Dark("sunset", 0xFF1A0F0E, 0xFF2E1A17, 0xFFFF7043, 0xFFFFCA28),
                Dark("nordic", 0xFF232935, 0xFF3B4252, 0xFF88C0D0, 0xFFA3BE8C),
                Dark("matrix", 0xFF000000, 0xFF001500, 0xFF00FF41, 0xFF00CC33),
                Dark("sakura", 0xFF1D0A14, 0xFF2D111E, 0xFFFFB7C5, 0xFFD4875A),
                Dark("golden", 0xFF140F07, 0xFF251C0D, 0xFFD4AF37, 0xFFC8960C),
                Dark("ruby", 0xFF1A0407, 0xFF2E070D, 0xFFE0115F, 0xFFFF8C42),
                Dark("electric", 0xFF0D001A, 0xFF1A0033, 0xFFB44DFF, 0xFF00FFFF),
                Dark("ghost", 0xFF1C1C1E, 0xFF2C2C2E, 0xFF98989D, 0xFF636366),
                Dark("solarized", 0xFF002B36, 0xFF073642, 0xFF268BD2, 0xFFB58900),
            };
        }
        public static ThemePalette Get(string id) { foreach (var t in All) if (t.Id == id) return t; return All[0]; }
        static ThemePalette Classic() => new ThemePalette
        {
            Id = "classic", NameKey = "theme_classic", Primary = Cyan, OnPrimary = ColorMath.Hex(0xFF001F2B), PrimaryContainer = ColorMath.Hex(0xFF004D66), OnPrimaryContainer = ColorMath.Hex(0xFFB3EFFF),
            Secondary = Gold, OnSecondary = ColorMath.Hex(0xFF3F2E00), Background = ColorMath.Hex(0xFF050D1F), Surface = ColorMath.Hex(0xFF091428), OnSurface = ColorMath.Hex(0xFFF0F4FF),
            SurfaceVariant = ColorMath.Hex(0xFF0D1E3A), OnSurfaceVariant = ColorMath.Hex(0xFF8899BB), Outline = ColorMath.Hex(0xFF4A6080), Tertiary = ColorMath.Hex(0xFF91D8FF),
            Error = ColorMath.Hex(0xFFF2B8B5), ErrorContainer = ColorMath.Hex(0xFF8C1D18), Accent = Gold
        };
        static ThemePalette Dark(string id, uint bg, uint surf, uint pri, uint sec, uint accent = 0)
        {
            Color b = ColorMath.Hex(bg), s = ColorMath.Hex(surf), p = ColorMath.Hex(pri), se = ColorMath.Hex(sec);
            var on = ColorMath.EnsureReadable(NearWhite, s, 7.0, Color.white);
            var pc = ColorMath.Lerp(p, b, 0.55f);
            return new ThemePalette
            {
                Id = id, NameKey = "theme_" + id, Background = b, Surface = s, Primary = p, OnPrimary = ColorMath.BestOn(p), PrimaryContainer = pc, OnPrimaryContainer = ColorMath.BestOn(pc),
                Secondary = se, OnSecondary = ColorMath.BestOn(se), OnSurface = on, SurfaceVariant = ColorMath.Lerp(s, on, 0.12f), OnSurfaceVariant = ColorMath.Lerp(on, b, 0.3f),
                Outline = ColorMath.Lerp(on, b, 0.45f), Tertiary = ColorMath.EnsureReadable(se, s, 4.5, Color.white), Error = ColorMath.Hex(0xFFF2B8B5), ErrorContainer = ColorMath.Hex(0xFF8C1D18),
                Accent = accent == 0 ? p : ColorMath.Hex(accent)
            };
        }
        static ThemePalette Light(string id, uint bg, uint surf, uint pri, uint sec)
        {
            Color b = ColorMath.Hex(bg), s = ColorMath.Hex(surf), p = ColorMath.Hex(pri), se = ColorMath.Hex(sec);
            var on = ColorMath.EnsureReadable(NearBlack, s, 7.0, Color.black);
            var pc = ColorMath.Lerp(p, b, 0.75f);
            return new ThemePalette
            {
                Id = id, NameKey = "theme_" + id, IsLight = true, Background = b, Surface = s, Primary = p, OnPrimary = ColorMath.BestOn(p), PrimaryContainer = pc, OnPrimaryContainer = ColorMath.BestOn(pc),
                Secondary = se, OnSecondary = ColorMath.BestOn(se), OnSurface = on, SurfaceVariant = ColorMath.Lerp(s, on, 0.08f), OnSurfaceVariant = ColorMath.Lerp(on, b, 0.35f),
                Outline = ColorMath.Lerp(on, b, 0.5f), Tertiary = ColorMath.EnsureReadable(se, s, 4.5, Color.black), Error = ColorMath.Hex(0xFFB3261E), ErrorContainer = ColorMath.Hex(0xFFF9DEDC), Accent = p
            };
        }
    }
}
