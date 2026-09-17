using System;
using System.Linq;
using UnityEngine;
using WitchTrial.Characters;

namespace WitchTrial.Story
{
    /// <summary>
    /// 保存舞台人物的外观资源标识、变换、镜像和排序状态。
    /// </summary>
    [Serializable] public sealed class SavedStageCharacter
    {
        public string instanceId, poseId;
        public Vector3 position, scale;
        public bool mirror;
        public int sortingOrder;
    }
    /// <summary>
    /// 记录可持久化的章节、节点、句子、选择及已解析舞台状态。
    /// </summary>
    [Serializable] public sealed class StorySaveData
    {
        public int version = 1;
        public string graphId, chapterId, nodeId, backgroundId, selectedKeywordId;
        public int lineIndex;
        public SavedStageCharacter[] characters;

        public static StorySaveData Capture(StoryRunner runner, SaveContentCatalog catalog)
        {
            if (runner == null || !runner.CanSave) throw new InvalidOperationException("请在剧情对话、选择或审判节点保存，转场中暂不可保存。");
            if (catalog == null) throw new InvalidOperationException("缺少存档资源目录。");
            return new StorySaveData {
                graphId = catalog.Id(runner.Story), chapterId = catalog.Id(runner.Chapter),
                nodeId = catalog.Id(runner.CurrentNode), lineIndex = runner.LineIndex,
                selectedKeywordId = runner.SelectedKeywordId, backgroundId = catalog.Id(runner.CurrentStage.Background),
                characters = runner.CurrentStage.Characters.Select(c => new SavedStageCharacter {
                    instanceId=c.instanceId, poseId=catalog.Id(c.appearance), position=c.position,
                    scale=c.scale, mirror=c.mirror, sortingOrder=c.sortingOrder }).ToArray()
            };
        }

        // Resolve and validate everything before replacing the running game.
        public ResolvedStorySave Resolve(SaveContentCatalog catalog)
        {
            if (version != 1 || catalog == null) throw new InvalidOperationException("不支持的存档版本或缺少资源目录。");
            var graph = catalog.Resolve<StoryGraph>(graphId);
            var chapter = catalog.Resolve<StoryChapter>(chapterId);
            var node = catalog.Resolve<StoryNode>(nodeId);
            if (graph == null || node == null || (chapter != null && chapter.graph != graph))
                throw new InvalidOperationException("存档的章节或节点不匹配。");
            var errors = graph.Validate();
            if (errors.Count > 0 || !Reachable(graph.entry, node)) throw new InvalidOperationException("存档剧情已变更，无法恢复此节点。");
            if (node is DialogueNode dialogue) {
                if (lineIndex < 0 || lineIndex >= dialogue.lines.Length) throw new InvalidOperationException("存档句子已不存在。");
            } else if (!(node is ChoiceNode) && !(node is TrialNode)) throw new InvalidOperationException("此节点不支持读档。");
            else if (lineIndex != -1) throw new InvalidOperationException("存档进度无效。");
            if (!string.IsNullOrEmpty(selectedKeywordId) && (!(node is TrialNode trial) || !Array.Exists(trial.keywords, k=>k.id==selectedKeywordId)))
                throw new InvalidOperationException("存档关键词已不存在。");
            if (characters == null) throw new InvalidOperationException("存档画面数据不完整。");
            var cast = characters.Select(c => new StageCharacter { instanceId=c.instanceId,
                appearance=catalog.Resolve<CharacterPose>(c.poseId), position=c.position, scale=c.scale,
                mirror=c.mirror, sortingOrder=c.sortingOrder }).ToArray();
            var background = catalog.Resolve<Sprite>(backgroundId);
            var stageErrors = new System.Collections.Generic.List<string>();
            StoryStage.ValidateLine(new DialogueLine { castChange=CastChange.Replace, characters=cast }, "存档", stageErrors);
            if(stageErrors.Count>0) throw new InvalidOperationException(string.Join("\n",stageErrors));
            return new ResolvedStorySave { graph=graph, chapter=chapter, node=node, lineIndex=lineIndex,
                selectedKeywordId=selectedKeywordId, stage=new StoryStage(background,cast) };
        }
        private static bool Reachable(StoryNode entry, StoryNode target)
        {
            var visited=new System.Collections.Generic.HashSet<StoryNode>();
            var queue=new System.Collections.Generic.Stack<StoryNode>(); queue.Push(entry);
            while(queue.Count>0) {
                var n=queue.Pop(); if(n==null || !visited.Add(n))continue; if(n==target)return true;
                if(n is DialogueNode d)queue.Push(d.next);
                else if(n is TransitionNode t)queue.Push(t.next);
                else if(n is ChoiceNode c)foreach(var choice in c.choices)queue.Push(choice.next);
                else if(n is TrialNode r){queue.Push(r.success);queue.Push(r.failure);}
            }
            return false;
        }
    }
    /// <summary>
    /// 保存经过资源解析和完整校验、可直接交给剧情运行器恢复的进度。
    /// </summary>
    public sealed class ResolvedStorySave
    {
        public StoryGraph graph; public StoryChapter chapter; public StoryNode node;
        public int lineIndex; public string selectedKeywordId; public StoryStage stage;
    }
}
