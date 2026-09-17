using UnityEngine;

namespace WitchTrial.Story
{
    /// <summary>
    /// 标记剧情或章节结束，并提供稳定的结局标识。
    /// </summary>
    [CreateAssetMenu(fileName = "End", menuName = "Witch Trial/Story/End")]
    public sealed class EndNode : StoryNode
    {
        public string endingId = "default";
        [TextArea] public string summary;
    }
}
