#pragma warning disable CS0618
using System;
using System.IO;
using System.Linq;
using DwSecretNotes.Core;
using DwSecretNotes.Bootstrap;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace DwSecretNotes.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectBootstrap
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string IconPath = "Assets/DwSecretNotes/Branding/app_icon.png";
        const string FontFolder = "Assets/DwSecretNotes/Fonts";
        const string PrimaryFontPath = FontFolder + "/NotoSans.ttf";
        const string ArabicFontPath = FontFolder + "/NotoSansArabic.ttf";
        const string PrimaryFontAssetPath = FontFolder + "/NotoSans SDF.asset";
        const string ArabicFontAssetPath = FontFolder + "/NotoSansArabic SDF.asset";
        const string ProductName = "dw Secret Notes";
        static ProjectBootstrap() { EditorApplication.delayCall += () => Setup(false); }
        [MenuItem("dw Secret Notes/Projekt einrichten", priority = 1)] static void SetupMenu() => Setup(true, true);
        public static void SetupForBatch() => Setup(false, true);
        static void Setup(bool verbose, bool force = false)
        {
            if (!force && (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)) { EditorApplication.delayCall += () => Setup(verbose); return; }
            try
            {
                var fonts = EnsureFonts();
                EnsureScene(fonts.primary, fonts.arabic);
                EnsurePlayerSettings();
                if (verbose) Debug.Log("[DW] Projekt eingerichtet.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }
        static (FontAsset primary, FontAsset arabic) EnsureFonts()
        {
            var primary = EnsureFontAsset(PrimaryFontPath, PrimaryFontAssetPath);
            var arabic = EnsureFontAsset(ArabicFontPath, ArabicFontAssetPath);
            primary.fallbackFontAssetTable ??= new System.Collections.Generic.List<FontAsset>();
            if (!primary.fallbackFontAssetTable.Contains(arabic)) primary.fallbackFontAssetTable.Add(arabic);
            EditorUtility.SetDirty(primary);
            AssetDatabase.SaveAssets();
            return (primary, arabic);
        }
        static FontAsset EnsureFontAsset(string fontPath, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath);
            if (asset != null) return asset;
            var source = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (source == null) throw new FileNotFoundException("Bundled font is missing: " + fontPath);
            asset = FontAsset.CreateFontAsset(source);
            if (asset == null) throw new InvalidOperationException("Could not generate TextCore font asset from " + fontPath);
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            asset.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(asset, assetPath);
            if (asset.atlasTextures != null)
                foreach (var texture in asset.atlasTextures)
                    if (texture != null && !AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, asset);
            if (asset.material != null && !AssetDatabase.Contains(asset.material)) AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[DW] TextCore font asset generated: " + assetPath);
            return asset;
        }
        static void EnsureScene(FontAsset primaryFont, FontAsset arabicFont)
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
            var loadedScene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !loadedScene.IsValid() || !loadedScene.isLoaded;
            var sceneToEdit = openedHere ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : loadedScene;
            bool sceneChanged = false;
            try
            {
                AppBootstrap bootstrap = null;
                foreach (var root in sceneToEdit.GetRootGameObjects())
                {
                    bootstrap = root.GetComponentInChildren<AppBootstrap>(true);
                    if (bootstrap != null) break;
                }
                if (bootstrap == null)
                {
                    var go = new GameObject("DwApp");
                    SceneManager.MoveGameObjectToScene(go, sceneToEdit);
                    bootstrap = go.AddComponent<AppBootstrap>();
                    sceneChanged = true;
                }
                var serialized = new SerializedObject(bootstrap);
                var primaryProperty = serialized.FindProperty("primaryFont");
                var arabicProperty = serialized.FindProperty("arabicFont");
                if (primaryProperty == null || arabicProperty == null)
                    throw new InvalidOperationException("AppBootstrap font references are not serialized as expected.");
                if (primaryProperty.objectReferenceValue != primaryFont || arabicProperty.objectReferenceValue != arabicFont)
                {
                    primaryProperty.objectReferenceValue = primaryFont;
                    arabicProperty.objectReferenceValue = arabicFont;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    sceneChanged = true;
                }
                if (sceneChanged)
                {
                    EditorSceneManager.MarkSceneDirty(sceneToEdit);
                    EditorSceneManager.SaveScene(sceneToEdit);
                    Debug.Log("[DW] Startszene mit Bootstrap und FontAssets aktualisiert: " + ScenePath);
                }
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(sceneToEdit, true);
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
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
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
            PlayerSettings.allowedAutorotateToPortrait = true; PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true; PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.resizeableActivity = true;
            PlayerSettings.Android.minifyRelease = true; PlayerSettings.Android.minifyDebug = false;
            UnityEditor.Android.UserBuildSettings.DebugSymbols.level = Unity.Android.Types.DebugSymbolLevel.SymbolTable;
            UnityEditor.Android.UserBuildSettings.DebugSymbols.format = Unity.Android.Types.DebugSymbolFormat.Zip | Unity.Android.Types.DebugSymbolFormat.IncludeInBundle;
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
            var previousArchitectures = PlayerSettings.Android.targetArchitectures;
            if (development) PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
            ConfigureSigning();
            try
            {
                bool ok = Run(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = BuildTarget.Android, targetGroup = BuildTargetGroup.Android, options = development ? BuildOptions.Development : BuildOptions.None });
                if (ok) CopyReleaseArtifacts(path);
                return ok;
            }
            finally { ClearSigning(); PlayerSettings.Android.targetArchitectures = previousArchitectures; AssetDatabase.SaveAssets(); }
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
        static void CopyReleaseArtifacts(string path)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path)); var tag = AppInfo.Version + "-" + AppInfo.VersionCode;
            var mapping = Path.GetFullPath("Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/mapping/release/mapping.txt");
            if (File.Exists(mapping)) { File.Copy(mapping, Path.Combine(dir, "mapping-" + tag + ".txt"), true); Debug.Log("[DW] ReTrace-Zuordnungsdatei: mapping-" + tag + ".txt"); }
            else Debug.LogWarning("[DW] Keine mapping.txt gefunden: " + mapping);
            foreach (var z in Directory.GetFiles(dir, "*.symbols.zip")) Debug.Log("[DW] Native Debug-Symbole: " + Path.GetFileName(z));
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
