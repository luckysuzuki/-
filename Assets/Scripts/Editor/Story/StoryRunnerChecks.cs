#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WitchTrial.Story.Editor
{
    /// <summary>仅创建临时内存对象，不生成剧情资产，也不保存或修改现有场景。</summary>
    public static class StoryRunnerChecks
    {
        [MenuItem("Witch Trial/Story/Run Core Checks")]
        public static void Run()
        {
            var assets = new List<ScriptableObject>();
            var host = new GameObject("StoryRunner checks") { hideFlags = HideFlags.HideAndDontSave };
            host.SetActive(false);
            try
            {
                var runner = host.AddComponent<StoryRunner>();
                var graph = Make<StoryGraph>(assets);
                var intro = Make<DialogueNode>(assets);
                var choice = Make<ChoiceNode>(assets);
                var trial = Make<TrialNode>(assets);
                var success = Make<DialogueNode>(assets);
                var failure = Make<DialogueNode>(assets);
                var end = Make<EndNode>(assets);
                graph.entry = intro;
                intro.lines = new[] { new DialogueLine { text = "第一句" }, new DialogueLine { text = "第二句" } };
                intro.next = choice;
                choice.choices = new[]
                {
                    new StoryChoice { text = "开始审判", next = trial },
                    new StoryChoice { text = "离开", next = end }
                };
                trial.statement = new[] { new TrialTextPart { text = "从未离开", keywordId = "alibi" } };
                trial.keywords = new[] { new TrialKeyword { id = "alibi", correctReasonId = "record" } };
                trial.reasons = new[]
                {
                    new TrialReason { id = "record", text = "门禁记录" },
                    new TrialReason { id = "guess", text = "猜测" }
                };
                trial.success = success;
                trial.failure = failure;
                success.lines = new[] { new DialogueLine { text = "反驳成功" } };
                success.next = end;
                failure.lines = new[] { new DialogueLine { text = "证据不足，请重试" } };
                failure.next = trial;
                Check(graph.Validate().Count == 0, "合法回路应通过校验");
                Check(!runner.Advance() && !runner.Choose(0), "未开始时拒绝推进");
                var eventOrder = new List<string>();
                runner.NodeEntered += node => eventOrder.Add("node");
                runner.DialogueLineChanged += line => eventOrder.Add("line");
                Check(runner.Play(graph) && runner.LineIndex == 0, "从第一句开始");
                Check(eventOrder.Count == 2 && eventOrder[0] == "node" && eventOrder[1] == "line", "事件顺序");
                Check(runner.Advance() && runner.LineIndex == 1, "下一句");
                Check(runner.Advance() && runner.CurrentNode == choice, "对话完成进入分支");
                Check(!runner.Advance() && !runner.Choose(-1) && !runner.Choose(2), "拒绝错误状态和索引");
                Check(runner.Choose(0) && runner.CurrentNode == trial, "选择进入审判");
                Check(!runner.SubmitReason("record") && !runner.SelectKeyword("missing"), "必须先选有效关键词");
                bool? result = null;
                runner.TrialResolved += correct => result = correct;
                Check(runner.SelectKeyword("alibi") && !runner.SubmitReason("missing"), "拒绝不存在的理由");
                Check(runner.SubmitReason("guess") && result == false && runner.CurrentNode == failure, "错误反馈");
                Check(runner.Advance() && runner.CurrentNode == trial && runner.SelectedKeywordId == null, "失败后重试清空关键词");
                Check(runner.SelectKeyword("alibi") && runner.SubmitReason("record"), "正确提交");
                Check(result == true && runner.CurrentNode == success, "正确反馈");
                var endings = 0;
                runner.Completed += node => endings++;
                Check(runner.Advance() && runner.State == StoryRunnerState.Completed && endings == 1, "进入结尾");
                Check(!runner.Advance() && endings == 1, "结束后不重复完成");
                Check(runner.Play() && runner.CurrentNode == intro && runner.LineIndex == 0, "重新开始");
                runner.Advance(); runner.Advance();
                Check(runner.Choose(1) && runner.CurrentNode == end, "另一条分支");
                Check(runner.Stop() && runner.CurrentNode == null && runner.State == StoryRunnerState.Idle, "停止清空进度");
                bool? reentrant = null;
                Action<StoryNode> callback = node => reentrant = runner.Advance();
                runner.NodeEntered += callback;
                Check(runner.Play(graph) && reentrant == false && runner.LineIndex == 0, "拒绝同步重入");
                runner.NodeEntered -= callback;
                var invalid = Make<StoryGraph>(assets);
                Check(!runner.Play(invalid) && runner.CurrentNode == intro, "非法 Graph 不破坏当前进度");
                trial.keywords[0].correctReasonId = "missing";
                Check(graph.Validate().Count > 0, "发现错误理由引用");
                trial.keywords[0].correctReasonId = "record";
                intro.next = null;
                Check(graph.Validate().Count > 0, "发现断链");
                Debug.Log("StoryRunner core checks: PASS（对话、分支、审判重试、结尾、重入、校验）");
            }
            finally
            {
                Object.DestroyImmediate(host);
                foreach (var asset in assets) Object.DestroyImmediate(asset);
            }
        }

        private static T Make<T>(List<ScriptableObject> assets) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.hideFlags = HideFlags.HideAndDontSave;
            assets.Add(asset);
            return asset;
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("StoryRunner check failed: " + label);
        }
    }
}
#endif
