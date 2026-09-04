using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchTrial.Settings;

namespace WitchTrial.UI.Panels
{
    public sealed class OptionsPanel : UIPanel
    {
        [Header("Text tab")]
        [SerializeField] private Slider textSpeedSlider;
        [SerializeField] private TMP_Text textSpeedValue;
        [SerializeField] private Slider autoIntervalSlider;
        [SerializeField] private TMP_Text autoIntervalValue;
        [SerializeField] private Toggle skipOnlyReadToggle;
        [SerializeField] private Toggle importantChoiceHintsToggle;
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Actions")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;

        private VisualNovelSettingsData _settings;

        public override UILayer Layer => UILayer.Popup;
        public override bool CloseOnMaskClick => true;

        private void Awake()
        {
            ConfigureSlider(textSpeedSlider);
            ConfigureSlider(autoIntervalSlider);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ConfigureSlider(textSpeedSlider);
            ConfigureSlider(autoIntervalSlider);
        }
#endif

        private void OnEnable()
        {
            textSpeedSlider?.onValueChanged.AddListener(HandleTextSpeedChanged);
            autoIntervalSlider?.onValueChanged.AddListener(HandleAutoIntervalChanged);
            skipOnlyReadToggle?.onValueChanged.AddListener(HandleSkipModeChanged);
            importantChoiceHintsToggle?.onValueChanged.AddListener(HandleImportantHintsChanged);
            languageDropdown?.onValueChanged.AddListener(HandleLanguageChanged);
            closeButton?.onClick.AddListener(RequestClose);
            resetButton?.onClick.AddListener(ResetToDefaults);
        }

        private void OnDisable()
        {
            textSpeedSlider?.onValueChanged.RemoveListener(HandleTextSpeedChanged);
            autoIntervalSlider?.onValueChanged.RemoveListener(HandleAutoIntervalChanged);
            skipOnlyReadToggle?.onValueChanged.RemoveListener(HandleSkipModeChanged);
            importantChoiceHintsToggle?.onValueChanged.RemoveListener(HandleImportantHintsChanged);
            languageDropdown?.onValueChanged.RemoveListener(HandleLanguageChanged);
            closeButton?.onClick.RemoveListener(RequestClose);
            resetButton?.onClick.RemoveListener(ResetToDefaults);
        }

        protected override void OnOpening(object context)
        {
            _settings = VisualNovelSettings.Load();
            ApplySettingsToView();
        }

        protected override void OnRefreshed(object context)
        {
            _settings = VisualNovelSettings.Load();
            ApplySettingsToView();
        }

        protected override void OnClosing()
        {
            VisualNovelSettings.Flush();
        }

        private void HandleTextSpeedChanged(float value)
        {
            EnsureSettings();
            _settings.textDisplaySpeed = Mathf.RoundToInt(value);
            UpdateValueLabels();
            VisualNovelSettings.Save(_settings);
        }

        private void HandleAutoIntervalChanged(float value)
        {
            EnsureSettings();
            _settings.autoPlayInterval = Mathf.RoundToInt(value);
            UpdateValueLabels();
            VisualNovelSettings.Save(_settings);
        }

        private void HandleSkipModeChanged(bool skipOnlyRead)
        {
            EnsureSettings();
            _settings.skipOnlyRead = skipOnlyRead;
            VisualNovelSettings.Save(_settings);
        }

        private void HandleImportantHintsChanged(bool enabled)
        {
            EnsureSettings();
            _settings.showImportantChoiceHints = enabled;
            VisualNovelSettings.Save(_settings);
        }

        private void HandleLanguageChanged(int index)
        {
            EnsureSettings();
            _settings.language = (VisualNovelLanguage)index;
            VisualNovelSettings.Save(_settings);
        }

        private void ResetToDefaults()
        {
            _settings = VisualNovelSettings.ResetToDefaults();
            ApplySettingsToView();
        }

        private void ApplySettingsToView()
        {
            EnsureSettings();
            textSpeedSlider?.SetValueWithoutNotify(_settings.textDisplaySpeed);
            autoIntervalSlider?.SetValueWithoutNotify(_settings.autoPlayInterval);
            skipOnlyReadToggle?.SetIsOnWithoutNotify(_settings.skipOnlyRead);
            importantChoiceHintsToggle?.SetIsOnWithoutNotify(_settings.showImportantChoiceHints);
            languageDropdown?.SetValueWithoutNotify((int)_settings.language);
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            if (textSpeedValue != null)
            {
                textSpeedValue.text = _settings.textDisplaySpeed.ToString();
            }

            if (autoIntervalValue != null)
            {
                autoIntervalValue.text = _settings.autoPlayInterval.ToString();
            }
        }

        private void EnsureSettings()
        {
            if (_settings == null)
            {
                _settings = VisualNovelSettings.Load();
            }
        }

        private static void ConfigureSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 1f;
            slider.maxValue = 10f;
            slider.wholeNumbers = true;
        }
    }
}
