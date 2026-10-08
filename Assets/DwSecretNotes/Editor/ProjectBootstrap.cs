#pragma warning disable CS0618
using System;
using System.IO;
using System.Linq;
using DwSecretNotes.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace DwSecretNotes.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectBootstrap
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string IconPath = "Assets/DwSecretNotes/Branding/app_icon.png";
        const string ProductName = "dw Secret Notes";
        static ProjectBootstrap() { EditorApplication.delayCall += () => Setup(false); }
        [MenuItem("dw Secret Notes/Projekt einrichten", priority = 1)] static void SetupMenu() => Setup(true, true);
        static void Setup(bool verbose, bool force = false)
        {
            if (!force && (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)) { EditorApplication.delayCall += () => Setup(verbose); return; }
            try { EnsureScene(); EnsurePlayerSettings(); if (verbose) Debug.Log("[DW] Projekt eingerichtet."); }
            catch (Exception e) { Debug.LogException(e); }
        }
        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var active = SceneManager.GetActiveScene();
                bool emptyUntitled = string.IsNullOrEmpty(active.path) && !active.isDirty;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, emptyUntitled ? NewSceneMode.Single : NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, ScenePath);
                if (!emptyUntitled) EditorSceneManager.CloseScene(scene, true);
                Debug.Log("[DW] Startszene angelegt: " + ScenePath);
            }
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) { scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
        }
        static void EnsurePlayerSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = "DoMeZos-Ware";
            PlayerSettings.bundleVersion = AppInfo.Version;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultScreenWidth = 450; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SplashScreen.show = false; PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color32(5, 13, 31, 255);
            var nbt = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(nbt, AppInfo.PackageId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppInfo.PackageId);
            PlayerSettings.SetScriptingBackend(nbt, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(nbt, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(nbt, ManagedStrippingLevel.Low);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = HighestSdk(36);
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.Android.startInFullscreen = false;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.bundleVersionCode = Math.Max(PlayerSettings.Android.bundleVersionCode, AppInfo.VersionCode);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan });
            PlayerSettings.allowedAutorotateToPortrait = true; PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            DisableHardwareStatistics();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
                foreach (var kind in new[] { UnityEditor.Android.AndroidPlatformIconKind.Legacy, UnityEditor.Android.AndroidPlatformIconKind.Round })
                {
                    var icons = PlayerSettings.GetPlatformIcons(nbt, kind);
                    foreach (var ic in icons) ic.SetTexture(icon, 0);
                    PlayerSettings.SetPlatformIcons(nbt, kind, icons);
                }
            }
        }
        static void DisableHardwareStatistics()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]); var prop = so.FindProperty("submitAnalytics");
            if (prop != null && prop.boolValue) { prop.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); Debug.Log("[DW] Hardware-Statistiken an Unity deaktiviert"); }
        }
        static AndroidSdkVersions HighestSdk(int max)
        {
            var best = AndroidSdkVersions.AndroidApiLevel35;
            foreach (AndroidSdkVersions v in Enum.GetValues(typeof(AndroidSdkVersions))) { int n = (int)v; if (n > (int)best && n <= max) best = v; }
            return best;
        }
        [MenuItem("dw Secret Notes/Android-APK erstellen", priority = 20)] static void BuildApkMenu() => BuildAndroid("Builds/Android/dw-secret-notes.apk", false, false);
        [MenuItem("dw Secret Notes/Android-AAB erstellen", priority = 21)] static void BuildAabMenu() => BuildAndroid("Builds/Android/dw-secret-notes.aab", true, false);
        static bool BuildAndroid(string path, bool bundle, bool development)
        {
            Setup(false, true); AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            EditorUserBuildSettings.buildAppBundle = bundle;
            EditorUserBuildSettings.development = development;
            ConfigureSigning();
            try { return Run(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android, options = development ? BuildOptions.Development : BuildOptions.None }); }
            finally { ClearSigning(); }
        }
        static bool BuildWindows(string path, bool development)
        {
            Setup(false, true); AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            return Run(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = BuildTarget.StandaloneWindows64, targetGroup = BuildTargetGroup.Standalone, options = development ? BuildOptions.Development : BuildOptions.None });
        }
        static bool Run(BuildPlayerOptions opts)
        {
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[DW] Build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB -> {opts.locationPathName}");
            return report.summary.result == BuildResult.Succeeded;
        }
        static void ConfigureSigning()
        {
            var ks = Environment.GetEnvironmentVariable("DW_KEYSTORE");
            if (!string.IsNullOrEmpty(ks) && File.Exists(ks))
            {
                PlayerSettings.Android.useCustomKeystore = true; PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("DW_KEYSTORE_PASS");
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("DW_KEY_ALIAS") ?? "domezos";
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("DW_KEY_PASS");
                Debug.Log("[DW] Signierung mit eigenem Keystore");
            }
            else PlayerSettings.Android.useCustomKeystore = false;
        }
        static void ClearSigning()
        {
            PlayerSettings.Android.keystorePass = ""; PlayerSettings.Android.keyaliasPass = "";
            PlayerSettings.Android.keystoreName = ""; PlayerSettings.Android.keyaliasName = ""; PlayerSettings.Android.useCustomKeystore = false;
            AssetDatabase.SaveAssets();
        }
        static string Arg(string name, string def) { var a = Environment.GetCommandLineArgs(); for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1]; return def; }
        static bool Flag(string name) => Environment.GetCommandLineArgs().Contains(name);
        public static void CiBuild()
        {
            bool ok = false;
            try
            {
                var target = Arg("-dwTarget", "android"); bool dev = Flag("-development");
                if (target == "windows") ok = BuildWindows(Arg("-buildPath", "Builds/Windows/dw-secret-notes.exe"), dev);
                else { bool aab = Flag("-aab"); var p = Arg("-buildPath", aab ? "Builds/Android/dw-secret-notes.aab" : "Builds/Android/dw-secret-notes.apk"); ok = BuildAndroid(p, aab, dev); }
            }
            catch (Exception e) { Debug.LogException(e); }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
