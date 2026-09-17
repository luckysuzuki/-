using System;
using UnityEngine;

namespace WitchTrial.Story
{
    /// <summary>
    /// 表示剧情运行器当前等待的交互类型或播放阶段。
    /// </summary>
    public enum StoryRunnerState { Idle, Dialogue, Choice, Trial, Completed, Transition }

    /// <summary>唯一剧情推进入口。UI 负责展示，Runner 负责进度与跳转。</summary>
    public sealed class StoryRunner : MonoBehaviour
    {
        [SerializeField] private StoryGraph story;
        [SerializeField] private bool playOnStart;
        [SerializeField] private StoryChapter chapter;

        private bool _executing;
        public StoryGraph Story => story;
        public StoryNode CurrentNode { get; private set; }
        public StoryRunnerState State { get; private set; }
        public int LineIndex { get; private set; } = -1;
        public string SelectedKeywordId { get; private set; }
        public string LastError { get; private set; }
        public StoryChapter Chapter => chapter;
        public StoryStage CurrentStage { get; private set; } = new StoryStage();
        public StoryTransition CurrentTransition { get; private set; }
        public bool CanAdvance => State == StoryRunnerState.Dialogue;
        public bool CanSave => !_executing && (State == StoryRunnerState.Dialogue || State == StoryRunnerState.Choice || State == StoryRunnerState.Trial);

        public bool Restore(ResolvedStorySave save)
        {
            return Execute(() => {
                if (save == null || save.graph == null || save.node == null || save.stage == null)
                    return Reject("存档数据不完整。");
                story = save.graph; chapter = save.chapter;
                CurrentTransition = null; CurrentNode = save.node;
                LineIndex = save.lineIndex; SelectedKeywordId = save.selectedKeywordId;
                CurrentStage = save.stage;
                State = CurrentNode is DialogueNode ? StoryRunnerState.Dialogue :
                    CurrentNode is ChoiceNode ? StoryRunnerState.Choice : StoryRunnerState.Trial;
                Publish(PlaybackStarted);
                Publish(NodeEntered, CurrentNode);
                Publish(StageChanged, CurrentStage);
                if (State == StoryRunnerState.Dialogue) Publish(DialogueLineChanged, CurrentLine);
                else if (State == StoryRunnerState.Trial && !string.IsNullOrEmpty(SelectedKeywordId))
                    Publish(KeywordSelected, Array.Find(((TrialNode)CurrentNode).keywords, k => k.id == SelectedKeywordId));
                return true;
            });
        }
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
        public event Action PlaybackStarted;
        public event Action<StoryStage> StageChanged;
        public event Action<StoryTransition> TransitionRequested;
        public event Action<StoryChapter> ChapterCompleted;

        private void Start()
        {
            if (playOnStart) Play();
        }

        public bool Play() => chapter != null ? PlayChapter(chapter) : Play(story);

        public bool PlayChapter(StoryChapter value)
        {
            return Execute(() =>
            {
                if (value == null) return Reject("未配置章节。");
                var errors = value.Validate();
                if (errors.Count > 0) return Reject(string.Join("\n", errors));
                Begin(value.graph, value);
                return true;
            });
        }

        private void Begin(StoryGraph graph, StoryChapter value)
        {
            story = graph;
            chapter = value;
            CurrentTransition = null;
            CurrentNode = null;
            State = StoryRunnerState.Idle;
            LineIndex = -1;
            CurrentStage = new StoryStage();
            Publish(PlaybackStarted);
            Publish(StageChanged, CurrentStage);
            Enter(graph.entry);
        }

        /// <summary>重新开始。校验失败时保留当前运行进度。</summary>
        public bool Play(StoryGraph graph)
        {
            return Execute(() =>
            {
                if (graph == null) return Reject("未配置 StoryGraph。");
                var errors = graph.Validate();
                if (errors.Count > 0) return Reject(string.Join("\n", errors));
                Begin(graph, null);
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
                    ApplyLineStage();
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
                CurrentTransition = null;
                CurrentStage = new StoryStage();
                Publish(StageChanged, CurrentStage);
                Publish(Stopped);
                return true;
            });
        }

        public bool PrepareTransition(StoryTransition request)
        {
            return Execute(() =>
            {
                if (State != StoryRunnerState.Transition || request == null || request != CurrentTransition)
                    return Reject("转场请求已失效。");
                if (request.IsPrepared) return true;
                CurrentStage = StoryStage.Resolve(CurrentStage, request.Node.next.lines[0]);
                request.IsPrepared = true;
                Publish(StageChanged, CurrentStage);
                return true;
            });
        }

        public bool CompleteTransition(StoryTransition request)
        {
            return Execute(() =>
            {
                if (State != StoryRunnerState.Transition || request == null || request != CurrentTransition || !request.IsPrepared)
                    return Reject("转场已失效或尚未准备完成。");
                CurrentTransition = null;
                Enter(request.Node.next, true);
                return true;
            });
        }

        private void ApplyLineStage()
        {
            CurrentStage = StoryStage.Resolve(CurrentStage, CurrentLine);
            Publish(StageChanged, CurrentStage);
        }

        private void Enter(StoryNode node, bool stagePrepared = false)
        {
            CurrentNode = node;
            LineIndex = -1;
            SelectedKeywordId = null;
            if (node is DialogueNode) { State = StoryRunnerState.Dialogue; LineIndex = 0; }
            else if (node is ChoiceNode) State = StoryRunnerState.Choice;
            else if (node is TrialNode) State = StoryRunnerState.Trial;
            else if (node is TransitionNode transition)
            {
                State = StoryRunnerState.Transition;
                CurrentTransition = new StoryTransition(transition);
            }
            else State = StoryRunnerState.Completed;
            Publish(NodeEntered, node);
            if (State == StoryRunnerState.Dialogue)
            {
                if (!stagePrepared) ApplyLineStage();
                Publish(DialogueLineChanged, CurrentLine);
            }
            else if (State == StoryRunnerState.Transition) Publish(TransitionRequested, CurrentTransition);
            else if (State == StoryRunnerState.Completed)
            {
                Publish(Completed, (EndNode)node);
                if (chapter != null) Publish(ChapterCompleted, chapter);
            }
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
