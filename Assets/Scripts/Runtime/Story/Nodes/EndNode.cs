using UnityEngine;

namespace WitchTrial.Story
{
    [CreateAssetMenu(fileName = "End", menuName = "Witch Trial/Story/End")]
    public sealed class EndNode : StoryNode
    {
        public string endingId = "default";
        [TextArea] public string summary;
    }
}
