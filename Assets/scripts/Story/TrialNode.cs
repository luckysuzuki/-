using System;
using UnityEngine;

namespace WitchTrial.Story
{
    [Serializable]
    public sealed class TrialTextPart
    {
        [TextArea] public string text;
        [Tooltip("留空为普通文字；填写关键词 ID 时由 UI 渲染为可点击的红字。")]
        public string keywordId;
    }

    [Serializable]
    public sealed class TrialKeyword
    {
        public string id;
        [TextArea] public string prompt;
        public string correctReasonId;
    }

    [Serializable]
    public sealed class TrialReason
    {
        public string id;
        [TextArea] public string text;
    }

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
