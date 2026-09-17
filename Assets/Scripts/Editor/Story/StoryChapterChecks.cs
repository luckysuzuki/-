using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WitchTrial.Characters;
using Object = UnityEngine.Object;

namespace WitchTrial.Story.Editor
{
    /// <summary>
    /// 验证章节、画面状态、转场、取消播放与旧数据兼容行为。
    /// </summary>
    public static class StoryChapterChecks
    {
        [MenuItem("Witch Trial/Story/Run Chapter Checks")]
        public static void Run()
        {
            StoryRunnerChecks.Run();
            var objects = new List<Object>();
            var host = new GameObject("Chapter checks") { hideFlags = HideFlags.HideAndDontSave };
            host.SetActive(false);
            objects.Add(host);
            try
            {
                var runner = host.AddComponent<StoryRunner>();
                var actorObject = new GameObject("test character") { hideFlags = HideFlags.HideAndDontSave };
                objects.Add(actorObject);
                var actor = actorObject.AddComponent<CharacterActor>();
                actor.characterId = "test";
                var pose = Make<CharacterPose>(objects); pose.characterPrefab = actor;
                var texture = new Texture2D(2, 2); objects.Add(texture);
                var bg = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * .5f); objects.Add(bg);
                var graph = Make<StoryGraph>(objects);
                var chapter = Make<StoryChapter>(objects); chapter.chapterId = "test-chapter"; chapter.graph = graph;
                var first = Make<DialogueNode>(objects);
                var next = Make<DialogueNode>(objects);
                var transition = Make<TransitionNode>(objects);
                var end = Make<EndNode>(objects);
                first.lines = new[] {
                    new DialogueLine { text="双人", speaker="画外音", backgroundChange=BackgroundChange.Replace, background=bg,
                        castChange=CastChange.Replace, characters=new[] {
                            new StageCharacter {instanceId="left",appearance=pose,position=Vector3.left,sortingOrder=1},
                            new StageCharacter {instanceId="right",appearance=pose,position=Vector3.right,sortingOrder=2} } },
                    new DialogueLine {text="无姓名旁白，保留画面",castChange=CastChange.Keep} };
                next.lines = new[] { new DialogueLine {text="新段落",castChange=CastChange.Clear,backgroundChange=BackgroundChange.Clear} };
                first.next=transition; transition.next=next; next.next=end; graph.entry=first;
                Check(chapter.Validate().Count==0,"合法章节");
                int lines=0, finished=0;
                runner.DialogueLineChanged += line => lines++;
                runner.ChapterCompleted += value => finished++;
                Check(runner.PlayChapter(chapter),"播放章节");
                Check(runner.CurrentStage.Characters.Count==2 && runner.CurrentStage.Background==bg,"双人背景");
                var presenterHost = new GameObject("stage checks") {hideFlags=HideFlags.HideAndDontSave};
                objects.Add(presenterHost);
                var presenter = presenterHost.AddComponent<DialogueCharacterPresenter>();
                presenter.ShowStage(runner.CurrentStage);
                Check(presenter.Actors.Count==2,"双人实例化");
                var left = presenter.Actors.First(a=>a.name=="left");
                Check(left.transform.localPosition==Vector3.left,"站位");
                Check(left.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder==1,"角色整体排序");
                runner.Advance();
                presenter.ShowStage(runner.CurrentStage);
                Check(!runner.CurrentLine.HasSpeaker && presenter.Actors.Contains(left),"旁白保持并复用人物");
                Check(runner.Advance() && runner.State==StoryRunnerState.Transition,"进入转场");
                var old=runner.CurrentTransition;
                Check(lines==2 && !runner.Advance(),"加载时无新句且禁止推进");
                Check(!runner.CompleteTransition(old),"拒绝未准备转场");
                Check(runner.PrepareTransition(old) && lines==2 && runner.CurrentLine==null,"黑屏准备不发布台词");
                Check(runner.CurrentStage.Background==null && runner.CurrentStage.Characters.Count==0,"准备新画面");
                Check(runner.CompleteTransition(old) && lines==3 && runner.CurrentNode==next,"淡入后进入下一段");
                Check(!runner.CompleteTransition(old),"重复完成无效");
                Check(runner.Advance() && finished==1,"章节完成一次");
                Check(!runner.Advance() && finished==1,"结束后不能重复推进");
                runner.PlayChapter(chapter); runner.Advance(); runner.Advance();
                old=runner.CurrentTransition; runner.Stop();
                Check(!runner.PrepareTransition(old) && !runner.CompleteTransition(old),"停止使旧请求失效");
                runner.PlayChapter(chapter); runner.Advance(); runner.Advance(); old=runner.CurrentTransition;
                runner.PlayChapter(chapter);
                Check(!runner.CompleteTransition(old) && runner.LineIndex==0,"重开使旧请求失效");
                first.lines[0].characters[1].instanceId="left";
                Check(graph.Validate().Count>0,"重复角色 ID 校验");
                first.lines[0].characters[1].instanceId="right";
                first.lines[0].background=null;
                Check(graph.Validate().Count>0,"缺少背景校验");
                first.lines[0].background=bg;
                transition.fadeInSeconds=float.NaN;
                Check(graph.Validate().Count>0,"非法转场时间校验");
                transition.fadeInSeconds=.2f;
                var legacy=StoryStage.Resolve(new StoryStage(),new DialogueLine {appearance=pose});
                Check(legacy.Characters.Count==1,"旧外观字段兼容");
                Check(StoryStage.Resolve(legacy,new DialogueLine()).Characters.Count==0,"旧空外观清场兼容");
                presenter.Clear();
                Debug.Log("Story chapter checks: PASS (chapter, cast, background, narration, transition, cancellation, compatibility)");
            }
            finally { for (int i=objects.Count-1;i>=0;i--) if(objects[i]!=null) Object.DestroyImmediate(objects[i]); }
        }
        private static T Make<T>(List<Object> objects) where T:ScriptableObject
        { var value=ScriptableObject.CreateInstance<T>(); value.hideFlags=HideFlags.HideAndDontSave; objects.Add(value); return value; }
        private static void Check(bool condition,string label)
        { if(!condition) throw new InvalidOperationException("Chapter check failed: "+label); }
    }
}
