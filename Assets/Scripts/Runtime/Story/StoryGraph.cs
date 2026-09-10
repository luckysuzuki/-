using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitchTrial.Story
{
    [CreateAssetMenu(fileName = "Story", menuName = "Witch Trial/Story/Graph")]
    public sealed class StoryGraph : ScriptableObject
    {
        public StoryNode entry;

        /// <summary>校验从入口可达的节点。允许有回路；每个节点都等待玩家输入。</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var visited = new HashSet<StoryNode>();
            var pending = new Stack<StoryNode>();
            var hasEnding = false;
            if (entry == null) { errors.Add("Graph: 未配置入口节点。"); return errors; }
            pending.Push(entry);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!visited.Add(node)) continue;
                var label = string.IsNullOrEmpty(node.name) ? node.GetType().Name : node.name;
                var dialogue = node as DialogueNode;
                var choice = node as ChoiceNode;
                var trial = node as TrialNode;
                var end = node as EndNode;
                if (dialogue != null)
                {
                    if (dialogue.lines == null || dialogue.lines.Length == 0)
                        errors.Add(label + ": 对话至少需要一句。");
                    else for (var i = 0; i < dialogue.lines.Length; i++)
                        if (dialogue.lines[i] == null || string.IsNullOrWhiteSpace(dialogue.lines[i].text))
                            errors.Add(label + ": 第 " + i + " 句为空。");
                    AddTarget(dialogue.next, label + ".next", pending, errors);
                }
                else if (choice != null)
                {
                    if (choice.choices == null || choice.choices.Length == 0)
                        errors.Add(label + ": 分支至少需要一个选项。");
                    else for (var i = 0; i < choice.choices.Length; i++)
                    {
                        var item = choice.choices[i];
                        if (item == null) { errors.Add(label + ": 存在空选项。"); continue; }
                        if (string.IsNullOrWhiteSpace(item.text)) errors.Add(label + ": 选项文字为空。");
                        AddTarget(item.next, label + ".choices[" + i + "]", pending, errors);
                    }
                }
                else if (trial != null)
                {
                    ValidateTrial(trial, label, errors);
                    AddTarget(trial.success, label + ".success", pending, errors);
                    AddTarget(trial.failure, label + ".failure", pending, errors);
                }
                else if (end != null)
                {
                    hasEnding = true;
                    if (string.IsNullOrWhiteSpace(end.endingId)) errors.Add(label + ": endingId 为空。");
                }
                else errors.Add(label + ": 不支持的节点类型 " + node.GetType().Name);
            }
            if (!hasEnding) errors.Add("Graph: 入口无法到达任何 End 节点。");
            return errors;
        }

        private static void AddTarget(StoryNode target, string label,
            Stack<StoryNode> pending, List<string> errors)
        {
            if (target == null) errors.Add(label + ": 未连接目标节点；请使用 End 显式结束剧情。");
            else pending.Push(target);
        }

        private static void ValidateTrial(TrialNode trial, string label, List<string> errors)
        {
            var reasons = new HashSet<string>(StringComparer.Ordinal);
            if (trial.reasons == null || trial.reasons.Length == 0) errors.Add(label + ": 缺少反驳理由。");
            else foreach (var reason in trial.reasons)
            {
                if (reason == null || string.IsNullOrWhiteSpace(reason.id) || !reasons.Add(reason.id))
                    errors.Add(label + ": 理由 ID 为空或重复。");
                if (reason != null && string.IsNullOrWhiteSpace(reason.text)) errors.Add(label + ": 理由文字为空。");
            }
            var keywords = new HashSet<string>(StringComparer.Ordinal);
            if (trial.keywords == null || trial.keywords.Length == 0) errors.Add(label + ": 缺少关键词。");
            else foreach (var keyword in trial.keywords)
            {
                if (keyword == null) { errors.Add(label + ": 存在空关键词。"); continue; }
                if (string.IsNullOrWhiteSpace(keyword.id) || !keywords.Add(keyword.id))
                    errors.Add(label + ": 关键词 ID 为空或重复。");
                if (string.IsNullOrWhiteSpace(keyword.correctReasonId) || !reasons.Contains(keyword.correctReasonId))
                    errors.Add(label + ": 关键词的正确理由不存在。");
            }
            var linked = new HashSet<string>(StringComparer.Ordinal);
            if (trial.statement == null || trial.statement.Length == 0) errors.Add(label + ": 缺少证言。");
            else foreach (var part in trial.statement)
            {
                if (part == null || string.IsNullOrWhiteSpace(part.text))
                { errors.Add(label + ": 证言片段为空。"); continue; }
                if (string.IsNullOrEmpty(part.keywordId)) continue;
                if (!keywords.Contains(part.keywordId)) errors.Add(label + ": 证言引用了不存在的关键词。");
                linked.Add(part.keywordId);
            }
            foreach (var id in keywords)
                if (!linked.Contains(id)) errors.Add(label + ": 关键词 " + id + " 未出现在证言中。");
        }
    }
}
