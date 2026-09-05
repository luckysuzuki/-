using System;
using UnityEngine;

namespace WitchTrial.Settings
{
    public enum VisualNovelLanguage
    {
        SimplifiedChinese = 0,
        TraditionalChinese = 1,
        Japanese = 2,
        English = 3
    }

    public enum Resolutions
    {
        _2560x1440 = 0,
        _1920x1080 = 1,
        _1600x900=2,
        _1280x720 = 3
    }

    public enum MaxFps
    {
        _60fps = 0,
        _30fps=1
    }
    [Serializable]
    public sealed class VisualNovelSettingsData
    {
        [Range(1, 10)] public int textDisplaySpeed = 8;
        [Range(1, 10)] public int autoPlayInterval = 5;
        [Range(1, 10)] public int MainVolume = 10;
        [Range(1, 10)] public int backGroundVolume = 6;
        [Range(1,10)] public int effectVolume = 4;
        [Range(1, 10)] public int  characterVolume=8;
        public bool skipOnlyRead = true;
        public bool showImportantChoiceHints = true;
        public bool displayMode = true;//ture means fullscreen ,false means window
        public MaxFps maxFps = MaxFps._60fps;
        public Resolutions resolution = Resolutions._2560x1440;
        public VisualNovelLanguage language = VisualNovelLanguage.SimplifiedChinese;

        public float SecondsPerCharacter => Mathf.Lerp(0.08f, 0.01f, (textDisplaySpeed - 1f) / 9f);

        public void Normalize()
        {
            textDisplaySpeed = Mathf.Clamp(textDisplaySpeed, 1, 10);
            autoPlayInterval = Mathf.Clamp(autoPlayInterval, 1, 10);
            MainVolume = Mathf.Clamp(MainVolume, 1, 10);
            backGroundVolume = Mathf.Clamp(backGroundVolume, 1, 10);
            effectVolume = Mathf.Clamp(effectVolume, 1, 10);
            characterVolume = Mathf.Clamp(characterVolume, 1, 10);
            resolution = (Resolutions)Mathf.Clamp((int)resolution, 0, 3);
            maxFps = (MaxFps)Mathf.Clamp((int)maxFps, 0, 1);
            language = (VisualNovelLanguage)Mathf.Clamp((int)language, 0, 3);
        }

        public VisualNovelSettingsData Clone()
        {
            return new VisualNovelSettingsData
            {
                textDisplaySpeed = textDisplaySpeed,
                autoPlayInterval = autoPlayInterval,
                skipOnlyRead = skipOnlyRead,
                showImportantChoiceHints = showImportantChoiceHints,
                MainVolume = MainVolume,
                backGroundVolume = backGroundVolume,
                effectVolume = effectVolume,
                characterVolume = characterVolume,
                displayMode = displayMode,
                resolution = resolution,
                maxFps = maxFps,
                language = language
            };
        }
    }

    /// <summary>
    /// Persistence boundary for the Options popup. Dialogue/audio systems can
    /// subscribe to Changed without referencing OptionsPanel.
    /// </summary>
    public static class VisualNovelSettings
    {
        private const string Prefix = "vn.settings.";
        private const string TextSpeedKey = Prefix + "text-speed";
        private const string AutoIntervalKey = Prefix + "auto-interval";
        private const string SkipOnlyReadKey = Prefix + "skip-only-read";
        private const string ImportantHintKey = Prefix + "important-hint";
        private const string LanguageKey = Prefix + "language";
        private const string MainVolumeKey = Prefix + "main-volume";
        private const string backGroundVolumeKey = Prefix + "background-volume";
        private const string effectVolumeKey = Prefix + "effect-volume";
        private const string characterVolumeKey = Prefix + "character-volume";
        private const string displayModeKey = Prefix + "fullscreen";
        private const string resolutionKey = Prefix + "resolution";
        private const string maxFpsKey = Prefix + "max-fps";

        public static event Action<VisualNovelSettingsData> Changed;

        public static VisualNovelSettingsData Load()
        {
            var defaults = new VisualNovelSettingsData();
            var data = new VisualNovelSettingsData
            {
                textDisplaySpeed = PlayerPrefs.GetInt(TextSpeedKey, defaults.textDisplaySpeed),
                autoPlayInterval = PlayerPrefs.GetInt(AutoIntervalKey, defaults.autoPlayInterval),
                MainVolume = PlayerPrefs.GetInt(MainVolumeKey, defaults.MainVolume),
                backGroundVolume = PlayerPrefs.GetInt(backGroundVolumeKey, defaults.backGroundVolume),
                effectVolume = PlayerPrefs.GetInt(effectVolumeKey, defaults.effectVolume),
                characterVolume = PlayerPrefs.GetInt(characterVolumeKey, defaults.characterVolume),
                displayMode = PlayerPrefs.GetInt(displayModeKey, defaults.displayMode ? 1 : 0) != 0,
                resolution = (Resolutions)PlayerPrefs.GetInt(resolutionKey, (int)defaults.resolution),
                maxFps = (MaxFps)PlayerPrefs.GetInt(maxFpsKey, (int)defaults.maxFps),
                skipOnlyRead = PlayerPrefs.GetInt(SkipOnlyReadKey, defaults.skipOnlyRead ? 1 : 0) != 0,
                showImportantChoiceHints = PlayerPrefs.GetInt(
                    ImportantHintKey, defaults.showImportantChoiceHints ? 1 : 0) != 0,
                language = (VisualNovelLanguage)PlayerPrefs.GetInt(LanguageKey, (int)defaults.language)
            };

            data.Normalize();
            return data;
        }

        public static void Save(VisualNovelSettingsData data, bool flushToDisk = false)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.Normalize();
            PlayerPrefs.SetInt(TextSpeedKey, data.textDisplaySpeed);
            PlayerPrefs.SetInt(AutoIntervalKey, data.autoPlayInterval);
            PlayerPrefs.SetInt(SkipOnlyReadKey, data.skipOnlyRead ? 1 : 0);
            PlayerPrefs.SetInt(ImportantHintKey, data.showImportantChoiceHints ? 1 : 0);
            PlayerPrefs.SetInt(LanguageKey, (int)data.language);
            PlayerPrefs.SetInt(MainVolumeKey, data.MainVolume);
            PlayerPrefs.SetInt(backGroundVolumeKey, data.backGroundVolume);
            PlayerPrefs.SetInt(effectVolumeKey, data.effectVolume);
            PlayerPrefs.SetInt(characterVolumeKey, data.characterVolume);
            PlayerPrefs.SetInt(displayModeKey, data.displayMode ? 1 : 0);
            PlayerPrefs.SetInt(resolutionKey, (int)data.resolution);
            PlayerPrefs.SetInt(maxFpsKey, (int)data.maxFps);

            if (flushToDisk)
            {
                PlayerPrefs.Save();
            }

            Changed?.Invoke(data.Clone());
        }

        public static VisualNovelSettingsData ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(TextSpeedKey);
            PlayerPrefs.DeleteKey(AutoIntervalKey);
            PlayerPrefs.DeleteKey(SkipOnlyReadKey);
            PlayerPrefs.DeleteKey(ImportantHintKey);
            PlayerPrefs.DeleteKey(LanguageKey);
            PlayerPrefs.DeleteKey(MainVolumeKey);
            PlayerPrefs.DeleteKey(backGroundVolumeKey);
            PlayerPrefs.DeleteKey(effectVolumeKey);
            PlayerPrefs.DeleteKey(characterVolumeKey);
            PlayerPrefs.DeleteKey(displayModeKey);
            PlayerPrefs.DeleteKey(resolutionKey);
            PlayerPrefs.DeleteKey(maxFpsKey);

            var defaults = new VisualNovelSettingsData();
            Save(defaults, true);
            return defaults;
        }

        public static void Flush()
        {
            PlayerPrefs.Save();
        }
    }
}
