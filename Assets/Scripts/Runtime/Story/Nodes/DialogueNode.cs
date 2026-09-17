using System;
using UnityEngine;
using WitchTrial.Characters;

namespace WitchTrial.Story
{
    /// <summary>
    /// 描述一句对话的文本、说话人、语音、人物外观和画面变化。
    /// </summary>
    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker;
        [TextArea(2, 8)] public string text;
        public Sprite portrait;
        [Tooltip("完整人物外观预设；由 DialogueCharacterPresenter 显示。空值隐藏分层人物，保留原 portrait 用法。")]
        public CharacterPose appearance;
        public AudioClip voice;
        [Tooltip("可选：身份标识；与画面里显示谁独立。名字仍由 speaker 决定，空名字为旁白。")]
        public string speakerId;
        public BackgroundChange backgroundChange = BackgroundChange.Keep;
        public Sprite background;
        [Tooltip("旧资产保留 LegacyAppearance；新段落首句建议 Replace/Clear，后续不改画面选 Keep。")]
        public CastChange castChange = CastChange.LegacyAppearance;
        public StageCharacter[] characters = Array.Empty<StageCharacter>();
        public bool HasSpeaker => !string.IsNullOrWhiteSpace(speaker);
    }

    /// <summary>
    /// 包含连续对话行，并在播放完毕后衔接下一个剧情节点。
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Witch Trial/Story/Dialogue")]
    public sealed class DialogueNode : StoryNode
    {
        public string segmentId;
        public string title;
        public DialogueLine[] lines = Array.Empty<DialogueLine>();
        public StoryNode next;
    }
}
