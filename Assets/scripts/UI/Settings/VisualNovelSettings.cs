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

    [Serializable]
    public sealed class VisualNovelSettingsData
    {
        [Range(1, 10)] public int textDisplaySpeed = 8;
        [Range(1, 10)] public int autoPlayInterval = 5;
        public bool skipOnlyRead = true;
        public bool showImportantChoiceHints = true;
        public VisualNovelLanguage language = VisualNovelLanguage.SimplifiedChinese;

        public float SecondsPerCharacter => Mathf.Lerp(0.08f, 0.01f, (textDisplaySpeed - 1f) / 9f);

        public void Normalize()
        {
            textDisplaySpeed = Mathf.Clamp(textDisplaySpeed, 1, 10);
            autoPlayInterval = Mathf.Clamp(autoPlayInterval, 1, 10);
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

        public static event Action<VisualNovelSettingsData> Changed;

        public static VisualNovelSettingsData Load()
        {
            var defaults = new VisualNovelSettingsData();
            var data = new VisualNovelSettingsData
            {
                textDisplaySpeed = PlayerPrefs.GetInt(TextSpeedKey, defaults.textDisplaySpeed),
                autoPlayInterval = PlayerPrefs.GetInt(AutoIntervalKey, defaults.autoPlayInterval),
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
