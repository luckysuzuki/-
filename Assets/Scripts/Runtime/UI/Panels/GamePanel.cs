using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WitchTrial.Story;
using WitchTrial.Presentation;
using WitchTrial.Settings;

namespace WitchTrial.UI.Panels
{
    /// <summary>
    /// 承载视觉小说游戏界面，协调剧情播放、输入、自动播放与菜单。
    /// </summary>
    public sealed class GamePanel : UIPanel
    {
        public StoryRunner runner;
        public StoryDialogueView dialogue;
        public StoryChapter chapter;
        public GameObject characterStage;
        public GameObject backgroundWorld;
        public Button nextButton;
        public Toggle autoToggle;
        public Button optionsButton;
        public GameObject chapterEnd;
        public Button restartButton;
        public Button WitchBookButton;
        public UnityEvent onChapterCompleted = new UnityEvent();
        public bool AutoPlaying { get; private set; }
        private float elapsed;
        private float autoInterval = 5f;
        private Coroutine starting;
        private ResolvedStorySave pendingSave;
        public override UILayer Layer => UILayer.Fullscreen;
        private void OnEnable()
        {
            nextButton?.onClick.AddListener(NextLine);
            optionsButton?.onClick.AddListener(OpenOptions);
            restartButton?.onClick.AddListener(RestartChapter);
            autoToggle?.onValueChanged.AddListener(SetAuto);
            WitchBookButton?.onClick.AddListener(OpenWitchBook);
            if (runner != null) { runner.DialogueLineChanged += OnLine; runner.ChapterCompleted += OnComplete; }
            VisualNovelSettings.Changed += ApplySettings;
            ApplySettings(VisualNovelSettings.Load());
            SetAuto(false);
        }
        private void OnDisable()
        {
            nextButton?.onClick.RemoveListener(NextLine);
            optionsButton?.onClick.RemoveListener(OpenOptions);
            restartButton?.onClick.RemoveListener(RestartChapter);
            autoToggle?.onValueChanged.RemoveListener(SetAuto);
            WitchBookButton?.onClick.RemoveListener(OpenWitchBook);
            if (runner != null) { runner.DialogueLineChanged -= OnLine; runner.ChapterCompleted -= OnComplete; }
            VisualNovelSettings.Changed -= ApplySettings;
            if (starting != null) StopCoroutine(starting);
            starting = null;
            SetAuto(false);
            SetWorld(false);
        }
        protected override void OnOpening(object context)
        {
            pendingSave = context as ResolvedStorySave;
            if (pendingSave != null) chapter = pendingSave.chapter;
            if (context is StoryChapter value) chapter = value;
        }
        protected override void OnRefreshed(object context) { OnOpening(context); RestartChapter(); }
        protected override void OnOpened() { RestartChapter(); }
        protected override void OnClosing() { if (runner != null) runner.Stop(); }
        protected override void OnRevealed() { SetWorld(true); }
        public void RestartChapter()
        {
            if (!isActiveAndEnabled) return;
            if (starting != null) StopCoroutine(starting);
            starting = StartCoroutine(Begin());
        }
        private IEnumerator Begin()
        {
            SetAuto(false);
            if (chapterEnd != null) chapterEnd.SetActive(false);
            SetWorld(true);
            yield return null; // Allow every presenter to subscribe before playback.
            starting = null;
            var save = pendingSave; pendingSave = null;
            if (runner != null && !(save != null ? runner.Restore(save) : runner.PlayChapter(chapter)))
                Debug.LogError(runner.LastError, this);
        }
        private bool Blocked => UIManager.Instance != null &&
            (UIManager.Instance.IsOpen<OptionsPanel>() || UIManager.Instance.IsOpen<ConfirmPopUp>() ||
             UIManager.Instance.IsOpen<MenuPanel>() || UIManager.Instance.IsOpen<SaveAndLoadPanel>());
        public void NextLine()
        {
            if (!IsVisible || Blocked || starting != null || runner == null || !runner.CanAdvance) return;
            elapsed = 0;
            runner.Advance();
        }
        public void SetAuto(bool value)
        {
            AutoPlaying = value;
            elapsed = 0;
            if (autoToggle != null) autoToggle.SetIsOnWithoutNotify(value);
        }
        private void Update()
        {
            if (!AutoPlaying || !IsVisible || Blocked || runner == null || !runner.CanAdvance) return;
            if (dialogue != null && dialogue.voiceSource != null && dialogue.voiceSource.isPlaying) { elapsed = 0; return; }
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= autoInterval) NextLine();
        }
        private void OnLine(DialogueLine line) { elapsed = 0; }
        private void OnComplete(StoryChapter completed)
        {
            SetAuto(false);
            if (chapterEnd != null) chapterEnd.SetActive(true);
            onChapterCompleted.Invoke();
        }
        public void OpenWitchBook()
        {
            UIRouter.Open<WitchBookPanel>();
        }
        public void OpenGame() { UIRouter.Replace<GamePanel>(chapter); }
        public void OpenOptions() { UIRouter.Open<MenuPanel>(); }
        private void ApplySettings(VisualNovelSettingsData value)
        {
            autoInterval = Mathf.Max(1, value.autoPlayInterval);
            if (dialogue != null && dialogue.voiceSource != null)
                dialogue.voiceSource.volume = value.MainVolume * value.characterVolume / 100f;
        }
        private void SetWorld(bool visible)
        {
            if (characterStage != null) characterStage.SetActive(visible);
            if (backgroundWorld != null) backgroundWorld.SetActive(visible);
        }
    }
}
