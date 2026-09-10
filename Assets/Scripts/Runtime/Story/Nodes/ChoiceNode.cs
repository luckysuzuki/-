using System;
using UnityEngine;

namespace WitchTrial.Story
{
    [Serializable]
    public sealed class StoryChoice
    {
        [TextArea] public string text;
        public StoryNode next;
    }

    [CreateAssetMenu(fileName = "Choice", menuName = "Witch Trial/Story/Choice")]
    public sealed class ChoiceNode : StoryNode
    {
        [TextArea] public string prompt;
        public StoryChoice[] choices = Array.Empty<StoryChoice>();
    }
}
