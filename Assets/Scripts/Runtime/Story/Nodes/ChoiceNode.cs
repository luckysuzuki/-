using System;
using UnityEngine;

namespace WitchTrial.Story
{
    /// <summary>
    /// 描述一个玩家可选的分支文本及其目标剧情节点。
    /// </summary>
    [Serializable]
    public sealed class StoryChoice
    {
        [TextArea] public string text;
        public StoryNode next;
    }

    /// <summary>
    /// 提供一组玩家选择，并根据所选项进入对应剧情节点。
    /// </summary>
    [CreateAssetMenu(fileName = "Choice", menuName = "Witch Trial/Story/Choice")]
    public sealed class ChoiceNode : StoryNode
    {
        [TextArea] public string prompt;
        public StoryChoice[] choices = Array.Empty<StoryChoice>();
    }
}
