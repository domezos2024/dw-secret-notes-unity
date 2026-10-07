using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
namespace DwSecretNotes.Core
{
    public static class DwLog
    {
        public static bool Verbose = Debug.isDebugBuild;
        static string Fmt(string lvl, string module, string fn, string msg) => $"[DW][{lvl}][{module}.{fn}][T{Thread.CurrentThread.ManagedThreadId}] {msg}";
        public static void I(string module, string msg, [CallerMemberName] string fn = "") { if (Verbose) Debug.Log(Fmt("I", module, fn, msg)); }
        public static void W(string module, string msg, [CallerMemberName] string fn = "") => Debug.LogWarning(Fmt("W", module, fn, msg));
        public static void E(string module, string msg, Exception ex = null, [CallerMemberName] string fn = "") => Debug.LogError(Fmt("E", module, fn, ex == null ? msg : msg + " | " + ex));
        public static void Call(string module, [CallerMemberName] string fn = "") { if (Verbose) Debug.Log(Fmt("C", module, fn, "call")); }
        public static void Var(string module, string name, object value, [CallerMemberName] string fn = "") { if (Verbose) Debug.Log(Fmt("V", module, fn, name + "=" + (value ?? "null"))); }
    }
}
