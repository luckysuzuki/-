using System;
using UnityEngine;

namespace WitchTrial.Story
{
    public enum StoryRunnerState { Idle, Dialogue, Choice, Trial, Completed }

    /// <summary>唯一剧情推进入口。UI 负责展示，Runner 负责进度与跳转。</summary>
    public sealed class StoryRunner : MonoBehaviour
    {
        [SerializeField] private StoryGraph story;
        [SerializeField] private bool playOnStart;

        private bool _executing;
        public StoryGraph Story => story;
        public StoryNode CurrentNode { get; private set; }
        public StoryRunnerState State { get; private set; }
        public int LineIndex { get; private set; } = -1;
        public string SelectedKeywordId { get; private set; }
        public string LastError { get; private set; }
        public DialogueLine CurrentLine
        {
            get
            {
                var dialogue = CurrentNode as DialogueNode;
                return dialogue != null && dialogue.lines != null &&
                    LineIndex >= 0 && LineIndex < dialogue.lines.Length ? dialogue.lines[LineIndex] : null;
            }
        }

        public event Action<StoryNode> NodeEntered;
        public event Action<DialogueLine> DialogueLineChanged;
        public event Action<TrialKeyword> KeywordSelected;
        // 先发布结果，再进入 success / failure 指向的反馈节点。
        public event Action<bool> TrialResolved;
        public event Action<EndNode> Completed;
        public event Action Stopped;

        private void Start()
        {
            if (playOnStart) Play();
        }

        public bool Play() => Play(story);

        /// <summary>重新开始。校验失败时保留当前运行进度。</summary>
        public bool Play(StoryGraph graph)
        {
            return Execute(() =>
            {
                if (graph == null) return Reject("未配置 StoryGraph。");
                var errors = graph.Validate();
                if (errors.Count > 0) return Reject(string.Join("\n", errors));
                story = graph;
                Enter(graph.entry);
                return true;
            });
        }

        /// <summary>下一句；最后一句之后进入 next。打字机尚未结束时应由 UI 先补全文字。</summary>
        public bool Advance()
        {
            return Execute(() =>
            {
                var node = CurrentNode as DialogueNode;
                if (State != StoryRunnerState.Dialogue || node == null)
                    return Reject("当前不是对话节点。");
                if (LineIndex + 1 < node.lines.Length)
                {
                    LineIndex++;
                    Publish(DialogueLineChanged, CurrentLine);
                }
                else Enter(node.next);
                return true;
            });
        }

        public bool Choose(int index)
        {
            return Execute(() =>
            {
                var node = CurrentNode as ChoiceNode;
                if (State != StoryRunnerState.Choice || node == null)
                    return Reject("当前不是分支节点。");
                if (index < 0 || index >= node.choices.Length) return Reject("分支索引越界。");
                Enter(node.choices[index].next);
                return true;
            });
        }

        public bool SelectKeyword(string keywordId)
        {
            return Execute(() =>
            {
                var node = CurrentNode as TrialNode;
                if (State != StoryRunnerState.Trial || node == null)
                    return Reject("当前不是审判节点。");
                var keyword = Array.Find(node.keywords, item => item.id == keywordId);
                if (keyword == null) return Reject("关键词不存在。");
                SelectedKeywordId = keyword.id;
                Publish(KeywordSelected, keyword);
                return true;
            });
        }

        /// <summary>返回值表示提交是否被受理；对错通过 TrialResolved 发布。</summary>
        public bool SubmitReason(string reasonId)
        {
            return Execute(() =>
            {
                var node = CurrentNode as TrialNode;
                if (State != StoryRunnerState.Trial || node == null)
                    return Reject("当前不是审判节点。");
                if (SelectedKeywordId == null) return Reject("请先选择证言关键词。");
                if (!Array.Exists(node.reasons, item => item.id == reasonId)) return Reject("反驳理由不存在。");
                var keyword = Array.Find(node.keywords, item => item.id == SelectedKeywordId);
                var correct = keyword.correctReasonId == reasonId;
                Publish(TrialResolved, correct);
                Enter(correct ? node.success : node.failure);
                return true;
            });
        }

        public bool Stop()
        {
            return Execute(() =>
            {
                CurrentNode = null;
                State = StoryRunnerState.Idle;
                LineIndex = -1;
                SelectedKeywordId = null;
                Publish(Stopped);
                return true;
            });
        }

        private void Enter(StoryNode node)
        {
            CurrentNode = node;
            LineIndex = -1;
            SelectedKeywordId = null;
            if (node is DialogueNode) { State = StoryRunnerState.Dialogue; LineIndex = 0; }
            else if (node is ChoiceNode) State = StoryRunnerState.Choice;
            else if (node is TrialNode) State = StoryRunnerState.Trial;
            else State = StoryRunnerState.Completed;
            Publish(NodeEntered, node);
            if (State == StoryRunnerState.Dialogue) Publish(DialogueLineChanged, CurrentLine);
            else if (State == StoryRunnerState.Completed) Publish(Completed, (EndNode)node);
        }

        private bool Execute(Func<bool> operation)
        {
            // 防止事件回调同步推进，导致同一次操作跳过多个节点。
            if (_executing) return Reject("剧情事件派发中，请在回调结束后操作。");
            _executing = true;
            LastError = null;
            try { return operation(); }
            finally { _executing = false; }
        }

        private bool Reject(string message)
        {
            LastError = message;
            return false;
        }

        private void Publish<T>(Action<T> handlers, T value)
        {
            if (handlers == null) return;
            foreach (Action<T> handler in handlers.GetInvocationList())
            {
                try { handler(value); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }

        private void Publish(Action handlers)
        {
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList())
            {
                try { handler(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }
    }
}
