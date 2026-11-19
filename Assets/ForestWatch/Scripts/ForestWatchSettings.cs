using UnityEngine;

namespace ForestWatch
{
    public static class ForestWatchSettings
    {
        const string VolumeKey = "ForestWatch.MasterVolume";
        const string FullscreenKey = "ForestWatch.Fullscreen";
        const string VSyncKey = "ForestWatch.VSync";

        public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .7f));
        public static bool Fullscreen => PlayerPrefs.GetInt(FullscreenKey, 0) != 0;
        public static bool VSync => PlayerPrefs.GetInt(VSyncKey, 1) != 0;

        public static void Initialize() => Apply();

        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            if (!Application.isEditor)
            {
                var desired = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                if (Screen.fullScreenMode != desired) Screen.fullScreenMode = desired;
            }
        }

        public static void SetMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
            AudioListener.volume = MasterVolume;
            PlayerPrefs.Save();
        }

        public static void SetFullscreen(bool value)
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            Apply();
            PlayerPrefs.Save();
        }

        public static void SetVSync(bool value)
        {
            PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
            Apply();
            PlayerPrefs.Save();
        }

        public static void ResetDefaults()
        {
            PlayerPrefs.SetFloat(VolumeKey, .7f);
            PlayerPrefs.SetInt(FullscreenKey, 0);
            PlayerPrefs.SetInt(VSyncKey, 1);
            Apply();
            PlayerPrefs.Save();
        }
    }
}
