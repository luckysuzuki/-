using System;
using UnityEngine;

namespace WitchTrial.Story
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker;
        [TextArea(2, 8)] public string text;
        public Sprite portrait;
        public AudioClip voice;
    }

    [CreateAssetMenu(fileName = "Dialogue", menuName = "Witch Trial/Story/Dialogue")]
    public sealed class DialogueNode : StoryNode
    {
        public DialogueLine[] lines = Array.Empty<DialogueLine>();
        public StoryNode next;
    }
}
