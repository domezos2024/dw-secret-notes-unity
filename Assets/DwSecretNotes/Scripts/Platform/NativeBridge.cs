using System;
using System.IO;
using DwSecretNotes.Core;
using UnityEngine;
namespace DwSecretNotes.Platform
{
    public enum Haptic { LongPress = 0, TextHandleMove = 9 }
    public sealed class LaunchRequest { public string Kind, Value; }
    public interface INativeBridge
    {
        void PickImage(Action<byte[]> onPicked);
        void ShareText(string text, string title, Action<bool> done);
        void CopyText(string text);
        string ReadClipboard();
        void OpenUrl(string url);
        void Haptic(Haptic h);
        void SetSecure(bool secure);
        LaunchRequest ConsumeLaunchRequest();
        void RequestReview(Action done);
        void SetWidgetLink(int widgetId, string link);
        void UpdateWidgets(ThemePalette p, string hint, string encrypt, string share, string shareMessage, string shareTitle);
        void MoveTaskToBack();
    }
    public static class Native
    {
        static INativeBridge instance;
        public static INativeBridge I => instance ??= Create();
        static INativeBridge Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidBridge();
#else
            return new DesktopBridge();
#endif
        }
    }
    public sealed class NativeReceiver : MonoBehaviour
    {
        public const string ObjectName = "DwNative";
        public static event Action<string> ImagePicked, ReviewDone, ShareDone, NewIntent;
        public static NativeReceiver Ensure()
        {
            var go = GameObject.Find(ObjectName);
            if (go == null) { go = new GameObject(ObjectName); DontDestroyOnLoad(go); }
            var c = go.GetComponent<NativeReceiver>(); if (c == null) c = go.AddComponent<NativeReceiver>(); return c;
        }
        public void OnImagePicked(string path) => ImagePicked?.Invoke(path);
        public void OnReviewDone(string s) => ReviewDone?.Invoke(s);
        public void OnShareDone(string s) => ShareDone?.Invoke(s);
        public void OnNewIntent(string s) => NewIntent?.Invoke(s);
        internal static byte[] ReadAndDelete(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try { var b = File.ReadAllBytes(path); File.Delete(path); return b; } catch (Exception e) { DwLog.E("Native", "read picked image failed", e); return null; }
        }
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    sealed class AndroidBridge : INativeBridge
    {
        readonly AndroidJavaClass bridge = new AndroidJavaClass("com.snote.domezos.unity.DwBridge");
        public AndroidBridge() { NativeReceiver.Ensure(); }
        public void PickImage(Action<byte[]> onPicked)
        {
            Action<string> h = null;
            h = p => { NativeReceiver.ImagePicked -= h; onPicked(NativeReceiver.ReadAndDelete(p)); };
            NativeReceiver.ImagePicked += h;
            bridge.CallStatic("pickImage", AppInfo.MaxImageEdge, AppInfo.JpegQuality);
        }
        public void ShareText(string text, string title, Action<bool> done) { bool ok = bridge.CallStatic<bool>("shareText", text, title); done?.Invoke(ok); }
        public void CopyText(string text) => bridge.CallStatic("copyText", text);
        public string ReadClipboard() => bridge.CallStatic<string>("readClipboard") ?? "";
        public void OpenUrl(string url) => Application.OpenURL(url);
        public void Haptic(Haptic h) => bridge.CallStatic("haptic", (int)h);
        public void SetSecure(bool secure) => bridge.CallStatic("setSecure", secure);
        public LaunchRequest ConsumeLaunchRequest()
        {
            var s = bridge.CallStatic<string>("consumeLaunchRequest");
            if (string.IsNullOrEmpty(s)) return null;
            int i = s.IndexOf('\n');
            return i < 0 ? null : new LaunchRequest { Kind = s.Substring(0, i), Value = s.Substring(i + 1) };
        }
        public void RequestReview(Action done)
        {
            Action<string> h = null;
            h = _ => { NativeReceiver.ReviewDone -= h; done?.Invoke(); };
            NativeReceiver.ReviewDone += h;
            bridge.CallStatic("requestReview");
        }
        public void SetWidgetLink(int widgetId, string link) => bridge.CallStatic("setWidgetLink", widgetId, link);
        public void UpdateWidgets(ThemePalette p, string hint, string encrypt, string share, string shareMessage, string shareTitle) =>
            bridge.CallStatic("updateWidgets", ColorMath.ToArgbInt(p.Background), ColorMath.ToArgbInt(p.OnSurface), ColorMath.ToArgbInt(p.Secondary), ColorMath.ToArgbInt(p.OnSecondary), hint, encrypt, share, shareMessage, shareTitle);
        public void MoveTaskToBack() => bridge.CallStatic("moveTaskToBack");
    }
#endif
    sealed class DesktopBridge : INativeBridge
    {
        public void PickImage(Action<byte[]> onPicked)
        {
#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanel("Image", "", "png,jpg,jpeg");
            onPicked(string.IsNullOrEmpty(path) ? null : ImageCodec.PrepareForUpload(File.ReadAllBytes(path)));
#else
            DwLog.W("Native", "image picker not available on this platform"); onPicked(null);
#endif
        }
        public void ShareText(string text, string title, Action<bool> done) { GUIUtility.systemCopyBuffer = text; done?.Invoke(false); }
        public void CopyText(string text) => GUIUtility.systemCopyBuffer = text;
        public string ReadClipboard() => GUIUtility.systemCopyBuffer ?? "";
        public void OpenUrl(string url) => Application.OpenURL(url);
        public void Haptic(Haptic h) { }
        public void SetSecure(bool secure) { }
        public LaunchRequest ConsumeLaunchRequest()
        {
            foreach (var a in Environment.GetCommandLineArgs()) if (a.StartsWith("--open=")) return new LaunchRequest { Kind = "view", Value = a.Substring(7) };
            return null;
        }
        public void RequestReview(Action done) { Application.OpenURL("https://play.google.com/store/apps/details?id=" + AppInfo.PackageId); done?.Invoke(); }
        public void SetWidgetLink(int widgetId, string link) { }
        public void UpdateWidgets(ThemePalette p, string hint, string encrypt, string share, string shareMessage, string shareTitle) { }
        public void MoveTaskToBack() { }
    }
    public static class ImageCodec
    {
        public static byte[] PrepareForUpload(byte[] src)
        {
            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(src)) { UnityEngine.Object.Destroy(tex); return null; }
            int w = tex.width, h = tex.height, max = Mathf.Max(w, h);
            if (max > AppInfo.MaxImageEdge)
            {
                float s = (float)AppInfo.MaxImageEdge / max; int nw = Mathf.Max(1, (int)(w * s)), nh = Mathf.Max(1, (int)(h * s));
                var rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, rt); var prev = RenderTexture.active; RenderTexture.active = rt;
                var scaled = new Texture2D(nw, nh, TextureFormat.RGB24, false); scaled.ReadPixels(new Rect(0, 0, nw, nh), 0, 0); scaled.Apply();
                RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.Destroy(tex); tex = scaled;
            }
            var jpg = tex.EncodeToJPG(AppInfo.JpegQuality); UnityEngine.Object.Destroy(tex); return jpg;
        }
    }
}
