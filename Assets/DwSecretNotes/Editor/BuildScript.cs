#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildAndroid()
    {
        var scenes = new[] { "Assets/Scenes/Main.unity" };
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/Android/dw-secret-notes.aab",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        // Release-Signierung: Passwoerter NUR aus Umgebungsvariablen (nie im Repo).
        var storePass = System.Environment.GetEnvironmentVariable("DW_KEYSTORE_PASS");
        if (string.IsNullOrEmpty(storePass))
        {
            Debug.LogError("Build abgebrochen: DW_KEYSTORE_PASS nicht gesetzt (Release muss signiert werden).");
            EditorApplication.Exit(1);
            return;
        }
        var keyPass = System.Environment.GetEnvironmentVariable("DW_KEY_PASS");
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = System.Environment.GetEnvironmentVariable("DW_KEYSTORE_PATH")
            ?? @"C:\Users\micha\OneDrive\Dokumente\AndroidSignKey";
        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasName = System.Environment.GetEnvironmentVariable("DW_KEY_ALIAS") ?? "domezos";
        PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(keyPass) ? storePass : keyPass;
        EditorUserBuildSettings.buildAppBundle = true;
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"Build succeeded: {report.summary.outputPath}");
        }
    }
}
#endif
