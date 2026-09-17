using TMPro;
using UnityEngine;
using WitchTrial.Story;

namespace WitchTrial.Presentation
{
    /// <summary>Bind an authored Game Panel. Displays complete lines, without generating UI or a typewriter.</summary>
    public sealed class StoryDialogueView : MonoBehaviour
    {
        public StoryRunner runner;
        public TMP_Text bodyText;
        public TMP_Text speakerText;
        [Tooltip("独立的名字装饰子对象；不能是此组件自身或其父对象。")]
        public GameObject speakerRoot;
        public GameObject continueIndicator;
        public CanvasGroup dialogueGroup;
        [Tooltip("可选的专用语音 AudioSource，不要与 BGM 共用。")]
        public AudioSource voiceSource;
        private StoryRunner subscribedRunner;
        private void OnEnable()
        {
            subscribedRunner = runner;
            if (subscribedRunner == null) return;
            subscribedRunner.DialogueLineChanged += Show;
            subscribedRunner.NodeEntered += Enter;
            subscribedRunner.Stopped += Clear;
            subscribedRunner.PlaybackStarted += Clear;
            if (subscribedRunner.CanAdvance) Show(subscribedRunner.CurrentLine); else Clear();
        }
        private void OnDisable()
        {
            if (subscribedRunner != null)
            {
                subscribedRunner.DialogueLineChanged -= Show;
                subscribedRunner.NodeEntered -= Enter;
                subscribedRunner.Stopped -= Clear;
                subscribedRunner.PlaybackStarted -= Clear;
            }
            subscribedRunner = null;
            Clear();
        }
        public void NextLine() { if (runner != null && runner.CanAdvance) runner.Advance(); }
        private void Enter(StoryNode node) { if (!(node is DialogueNode)) Clear(); }
        private void Show(DialogueLine line)
        {
            if (line == null) { Clear(); return; }
            if (bodyText != null) bodyText.text = line.text;
            if (speakerText != null) speakerText.text = line.HasSpeaker ? line.speaker : "";
            SetChild(speakerRoot, line.HasSpeaker);
            SetChild(continueIndicator, true);
            SetVisibility(true);
            StopVoice();
            if (voiceSource != null && line.voice != null)
            { voiceSource.loop = false; voiceSource.clip = line.voice; voiceSource.Play(); }
        }
        private void StopVoice() { if (voiceSource != null) { voiceSource.Stop(); voiceSource.clip = null; } }
        private void SetVisibility(bool visible)
        {
            if (dialogueGroup == null) return;
            dialogueGroup.alpha = visible ? 1 : 0;
            dialogueGroup.interactable = visible;
            dialogueGroup.blocksRaycasts = visible;
        }
        private void SetChild(GameObject target, bool active)
        { if (target != null && !transform.IsChildOf(target.transform)) target.SetActive(active); }
        private void Clear()
        {
            if (bodyText != null) bodyText.text = "";
            if (speakerText != null) speakerText.text = "";
            SetChild(speakerRoot, false);
            SetChild(continueIndicator, false);
            SetVisibility(false);
            StopVoice();
        }
    }
}
