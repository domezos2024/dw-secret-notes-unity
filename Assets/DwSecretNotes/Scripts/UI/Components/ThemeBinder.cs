using System;
using System.Collections.Generic;
using DwSecretNotes.Core;
using UnityEngine;
using UnityEngine.UIElements;
namespace DwSecretNotes.UI
{
    public static class ThemeBinder
    {
        sealed class Binding { public VisualElement El; public Action<ThemePalette> Apply; }
        static readonly List<Binding> bindings = new List<Binding>();
        public static ThemePalette Current { get; private set; } = ThemeCatalog.Get("classic");
        public static event Action<ThemePalette> Changed;
        public static T On<T>(T el, Action<T, ThemePalette> apply) where T : VisualElement
        {
            bindings.Add(new Binding { El = el, Apply = p => apply(el, p) });
            apply(el, Current);
            return el;
        }
        public static void Set(ThemePalette p)
        {
            Current = p; DwLog.Var("Theme", "theme", p.Id);
            bindings.RemoveAll(b => b.El.panel == null);
            foreach (var b in bindings.ToArray()) b.Apply(Current);
            Changed?.Invoke(p);
        }
        public static void Prune() => bindings.RemoveAll(b => b.El.panel == null);
        public static void Border(VisualElement e, Color c) { e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c; }
        public static Color A(Color c, float a) => ColorMath.WithAlpha(c, a);
    }
}
