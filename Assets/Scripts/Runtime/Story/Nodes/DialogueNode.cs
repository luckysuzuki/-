using System;
using UnityEngine;
using WitchTrial.Characters;

namespace WitchTrial.Story
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker;
        [TextArea(2, 8)] public string text;
        public Sprite portrait;
        [Tooltip("完整人物外观预设；由 DialogueCharacterPresenter 显示。空值隐藏分层人物，保留原 portrait 用法。")]
        public CharacterPose appearance;
        public AudioClip voice;
    }

    [CreateAssetMenu(fileName = "Dialogue", menuName = "Witch Trial/Story/Dialogue")]
    public sealed class DialogueNode : StoryNode
    {
        public DialogueLine[] lines = Array.Empty<DialogueLine>();
        public StoryNode next;
    }
}
