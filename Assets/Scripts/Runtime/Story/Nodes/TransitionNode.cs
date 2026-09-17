using UnityEngine;

namespace WitchTrial.Story
{
    /// <summary>
    /// 配置对话段落之间的淡出、加载提示、黑屏停留与淡入时长。
    /// </summary>
    [CreateAssetMenu(fileName = "Transition", menuName = "Witch Trial/Story/Transition")]
    public sealed class TransitionNode : StoryNode
    {
        [Min(0)] public float fadeOutSeconds = 0.35f;
        [Min(0)] public float minimumBlackSeconds = 0.15f;
        [Min(0)] public float fadeInSeconds = 0.35f;
        public string loadingText = "Loading...";
        [Tooltip("下一段对话。会在黑屏期间准备其第一句的背景与人物。")]
        public DialogueNode next;
    }

    /// <summary>One transition attempt. Old requests cannot complete a restarted/stopped story.</summary>
    public sealed class StoryTransition
    {
        public TransitionNode Node { get; }
        public bool IsPrepared { get; internal set; }
        internal StoryTransition(TransitionNode node) { Node = node; }
    }
}
