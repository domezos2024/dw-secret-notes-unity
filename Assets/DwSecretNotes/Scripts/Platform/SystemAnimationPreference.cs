using DwSecretNotes.Core;
using UnityEngine;

namespace DwSecretNotes.Platform
{
    public static class SystemAnimationPreference
    {
        public static bool ReducedMotion()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");
                using var settings = new AndroidJavaClass("android.provider.Settings$Global");
                var scale = settings.CallStatic<float>("getFloat", resolver, "animator_duration_scale", 1f);
                DwLog.Var("Motion", "androidAnimatorScale", scale);
                return scale <= 0f;
            }
            catch (System.Exception e) { DwLog.W("Motion", "Cannot read Android animator scale: " + e.GetType().Name); }
#endif
            return false;
        }
    }
}
