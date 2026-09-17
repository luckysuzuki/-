using System;
using UnityEngine;

namespace WitchTrial.Story
{
    /// <summary>
    /// 描述审判证言中的一段文本及其可关联的关键词。
    /// </summary>
    [Serializable]
    public sealed class TrialTextPart
    {
        [TextArea] public string text;
        [Tooltip("留空为普通文字；填写关键词 ID 时由 UI 渲染为可点击的红字。")]
        public string keywordId;
    }

    /// <summary>
    /// 描述证言关键词及能够正确反驳它的理由标识。
    /// </summary>
    [Serializable]
    public sealed class TrialKeyword
    {
        public string id;
        [TextArea] public string prompt;
        public string correctReasonId;
    }

    /// <summary>
    /// 描述玩家在审判中可以提交的一个反驳理由。
    /// </summary>
    [Serializable]
    public sealed class TrialReason
    {
        public string id;
        [TextArea] public string text;
    }

    /// <summary>
    /// 组织证言、关键词和理由，并根据判断结果进入成功或失败节点。
    /// </summary>
    [CreateAssetMenu(fileName = "Trial", menuName = "Witch Trial/Story/Trial")]
    public sealed class TrialNode : StoryNode
    {
        public string speaker;
        public TrialTextPart[] statement = Array.Empty<TrialTextPart>();
        public TrialKeyword[] keywords = Array.Empty<TrialKeyword>();
        public TrialReason[] reasons = Array.Empty<TrialReason>();
        [Tooltip("通常连接成功反馈 DialogueNode，之后再连接结尾。")]
        public StoryNode success;
        [Tooltip("通常连接失败反馈 DialogueNode，再连接回本审判节点重试。")]
        public StoryNode failure;
    }
}
