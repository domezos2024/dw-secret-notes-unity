using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace DwSecretNotes.EditorTools
{
    public static class AtgEnable
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        public static void Enable()
        {
            var t = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } }).First(x => x.FullName == "UnityEditor.UIElements.UIToolkitProjectSettings");
            var inst = t.BaseType.GetProperty("instance", All).GetValue(null);
            t.GetProperty("enableAdvancedText", All).SetValue(inst, true);
            t.GetMethod("Save", All).Invoke(inst, null);
            Debug.Log("[DW-ATG] enableAdvancedText=" + t.GetProperty("enableAdvancedText", All).GetValue(inst));
            EditorApplication.Exit(0);
        }
    }
}
