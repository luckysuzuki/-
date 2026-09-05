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
        [Header("Graphic tab")]
        [SerializeField]private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown maxFpsDropdown;
        [Header("Audio tab")]
        [SerializeField]private Slider mainVolumeSlider;
        [SerializeField] private TMP_Text mainVolumeValue;
        [SerializeField]private Slider backgroundVolumeSlider;
        [SerializeField] private TMP_Text backgroundVolumeValue;
        [SerializeField]private Slider effectVolumeSlider;
        [SerializeField] private TMP_Text effectVolumeValue;
        [SerializeField]private Slider characterVolumeSlider;
        [SerializeField] private TMP_Text characterVolumeValue;
        [Header("Options")]
        [SerializeField] private Toggle textPanel;
        [SerializeField] private Toggle graphicPanel;
        [SerializeField] private Toggle audioPanel;
        [Header("Panel contents")]
        [SerializeField] private GameObject textPanelContent;
        [SerializeField] private GameObject graphicPanelContent;
        [SerializeField] private GameObject audioPanelContent;

        [Header("Actions")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;

        private VisualNovelSettingsData _settings;
        private int _selectedTab;

        public override UILayer Layer => UILayer.Popup;
        public override bool CloseOnMaskClick => true;

        private void Awake()
        {
            ConfigureVolumeSliders();
            SelectTab(0);
            ConfigureSlider(textSpeedSlider);
            ConfigureSlider(autoIntervalSlider);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ConfigureVolumeSliders();
            ConfigureSlider(textSpeedSlider);
            ConfigureSlider(autoIntervalSlider);
        }
#endif

        private void OnEnable()
        {
            ApplySettingsToView();
            fullscreenToggle?.onValueChanged.AddListener(HandleFullscreenChanged);
            resolutionDropdown?.onValueChanged.AddListener(HandleResolutionChanged);
            maxFpsDropdown?.onValueChanged.AddListener(HandleMaxFpsChanged);
            mainVolumeSlider?.onValueChanged.AddListener(HandleMainVolumeChanged);
            backgroundVolumeSlider?.onValueChanged.AddListener(HandleBackgroundVolumeChanged);
            effectVolumeSlider?.onValueChanged.AddListener(HandleEffectVolumeChanged);
            characterVolumeSlider?.onValueChanged.AddListener(HandleCharacterVolumeChanged);
            textPanel?.onValueChanged.AddListener(HandletextTab);
            graphicPanel?.onValueChanged.AddListener(HandlegraphicTab);
            audioPanel?.onValueChanged.AddListener(HandleaudioTab);
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
            VisualNovelSettings.Flush();
            fullscreenToggle?.onValueChanged.RemoveListener(HandleFullscreenChanged);
            resolutionDropdown?.onValueChanged.RemoveListener(HandleResolutionChanged);
            maxFpsDropdown?.onValueChanged.RemoveListener(HandleMaxFpsChanged);
            mainVolumeSlider?.onValueChanged.RemoveListener(HandleMainVolumeChanged);
            backgroundVolumeSlider?.onValueChanged.RemoveListener(HandleBackgroundVolumeChanged);
            effectVolumeSlider?.onValueChanged.RemoveListener(HandleEffectVolumeChanged);
            characterVolumeSlider?.onValueChanged.RemoveListener(HandleCharacterVolumeChanged);
            textPanel?.onValueChanged.RemoveListener(HandletextTab);
            graphicPanel?.onValueChanged.RemoveListener(HandlegraphicTab);
            audioPanel?.onValueChanged.RemoveListener(HandleaudioTab);
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
            SelectTab(0);
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
            ConfigureDropdowns();
            SetBooleanToggle(fullscreenToggle, _settings.displayMode);
            resolutionDropdown?.SetValueWithoutNotify((int)_settings.resolution);
            maxFpsDropdown?.SetValueWithoutNotify((int)_settings.maxFps);
            mainVolumeSlider?.SetValueWithoutNotify(_settings.MainVolume);
            backgroundVolumeSlider?.SetValueWithoutNotify(_settings.backGroundVolume);
            effectVolumeSlider?.SetValueWithoutNotify(_settings.effectVolume);
            characterVolumeSlider?.SetValueWithoutNotify(_settings.characterVolume);
            textSpeedSlider?.SetValueWithoutNotify(_settings.textDisplaySpeed);
            autoIntervalSlider?.SetValueWithoutNotify(_settings.autoPlayInterval);
            SetBooleanToggle(skipOnlyReadToggle, _settings.skipOnlyRead);
            SetBooleanToggle(importantChoiceHintsToggle, _settings.showImportantChoiceHints);
            languageDropdown?.SetValueWithoutNotify((int)_settings.language);
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            if (mainVolumeValue != null) mainVolumeValue.text = _settings.MainVolume.ToString();
            if (backgroundVolumeValue != null) backgroundVolumeValue.text = _settings.backGroundVolume.ToString();
            if (effectVolumeValue != null) effectVolumeValue.text = _settings.effectVolume.ToString();
            if (characterVolumeValue != null) characterVolumeValue.text = _settings.characterVolume.ToString();
            if (textSpeedValue != null)
            {
                textSpeedValue.text = _settings.textDisplaySpeed.ToString();
            }

            if (autoIntervalValue != null)
            {
                autoIntervalValue.text = _settings.autoPlayInterval.ToString();
            }
        }

        private void HandleFullscreenChanged(bool value)
        {
            EnsureSettings();
            _settings.displayMode = value;
            VisualNovelSettings.Save(_settings);
            UpdateValueLabels();
        }

        private void HandleResolutionChanged(int value)
        {
            EnsureSettings();
            _settings.resolution = (Resolutions)value;
            VisualNovelSettings.Save(_settings);
            UpdateValueLabels();
        }

        private void HandleMaxFpsChanged(int value)
        {
            EnsureSettings();
            _settings.maxFps = (MaxFps)value;
            VisualNovelSettings.Save(_settings);
            UpdateValueLabels();
        }

        private void HandleMainVolumeChanged(float value)
        {
            EnsureSettings();
            _settings.MainVolume = Mathf.RoundToInt(value);
            VisualNovelSettings.Save(_settings);
            UpdateValueLabels();
        }

        private void HandleBackgroundVolumeChanged(float value)
        {
            EnsureSettings();
            _settings.backGroundVolume = Mathf.RoundToInt(value);
            VisualNovelSettings.Save(_settings);
            UpdateValueLabels();
        }

        private void HandleEffectVolumeChanged(float value)
        {
            EnsureSettings();
            _settings.effectVolume = Mathf.RoundToInt(value);
            VisualNovelSettings.Save(_settings);
            UpdateValueLabels();
        }

        private void HandleCharacterVolumeChanged(float value)
        {
            EnsureSettings();
            _settings.characterVolume = Mathf.RoundToInt(value);
            VisualNovelSettings.Save(_settings);
            UpdateValueLabels();
        }

        private void HandletextTab(bool value) => HandleTabChanged(0, value);
        private void HandlegraphicTab(bool value) => HandleTabChanged(1, value);
        private void HandleaudioTab(bool value) => HandleTabChanged(2, value);

        private void HandleTabChanged(int tab, bool value)
        {
            if (value) SelectTab(tab);
            else if (_selectedTab == tab)
            {
                // A group turns the old toggle off before notifying the new one.
                if (textPanel != null && textPanel.isOn) SelectTab(0);
                else if (graphicPanel != null && graphicPanel.isOn) SelectTab(1);
                else if (audioPanel != null && audioPanel.isOn) SelectTab(2);
                else SelectTab(tab);
            }
        }

        private void SelectTab(int tab)
        {
            _selectedTab = tab;
            textPanel?.SetIsOnWithoutNotify(tab == 0);
            graphicPanel?.SetIsOnWithoutNotify(tab == 1);
            audioPanel?.SetIsOnWithoutNotify(tab == 2);
            if (textPanelContent != null) textPanelContent.SetActive(tab == 0);
            if (graphicPanelContent != null) graphicPanelContent.SetActive(tab == 1);
            if (audioPanelContent != null) audioPanelContent.SetActive(tab == 2);
        }

        private void ConfigureVolumeSliders()
        {
            ConfigureSlider(mainVolumeSlider);
            ConfigureSlider(backgroundVolumeSlider);
            ConfigureSlider(effectVolumeSlider);
            ConfigureSlider(characterVolumeSlider);
        }

        private void ConfigureDropdowns()
        {
            SetOptions(languageDropdown, "简体中文", "繁體中文", "日本語", "English");
            SetOptions(resolutionDropdown, "2560 x 1440", "1920 x 1080", "1600 x 900", "1280 x 720");
            SetOptions(maxFpsDropdown, "60 FPS", "30 FPS");
        }

        private static void SetOptions(TMP_Dropdown dropdown, params string[] labels)
        {
            if (dropdown == null) return;
            dropdown.options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (var label in labels) dropdown.options.Add(new TMP_Dropdown.OptionData(label));
            dropdown.RefreshShownValue();
        }

        private static void SetBooleanToggle(Toggle toggle, bool value)
        {
            if (toggle == null) return;
            if (!value && toggle.group != null)
            {
                foreach (var other in toggle.group.GetComponentsInChildren<Toggle>(true))
                {
                    if (other != toggle && other.group == toggle.group)
                    {
                        other.SetIsOnWithoutNotify(true);
                        break;
                    }
                }
            }
            toggle.SetIsOnWithoutNotify(value);
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
