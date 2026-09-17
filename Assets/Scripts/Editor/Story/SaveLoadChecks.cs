using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WitchTrial.Story.Editor
{
    /// <summary>
    /// 对存档持久化、精确恢复、跨节点推进、边界与删除行为执行回归检查。
    /// </summary>
    public static class SaveLoadChecks
    {
        [MenuItem("Witch Trial/Story/Run Save Load Checks")]
        public static void Run() { Debug.Log(Check()); }
        public static string Check()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("请先退出运行模式。");
            SaveContentCatalogBuilder.Rebuild();
            var catalog=SaveContentCatalog.Load();
            var chapter=AssetDatabase.LoadAssetAtPath<StoryChapter>("Assets/StoryExamples/GamePanelDemo/Chapter.asset");
            var root=Path.Combine(Application.temporaryCachePath,"SaveLoadChecks-"+Guid.NewGuid().ToString("N"));
            var first=new GameObject("SaveLoadCheck-A");var second=new GameObject("SaveLoadCheck-B");
            var texture=new Texture2D(4,4);
            var store=new StorySaveStore(root);
            try {
                var a=first.AddComponent<StoryRunner>();var b=second.AddComponent<StoryRunner>();
                Require(a.PlayChapter(chapter),a.LastError); Require(a.Advance(),a.LastError);
                var data=StorySaveData.Capture(a,catalog);var png=texture.EncodeToPNG();
                store.Write(0,data,png,"First");
                var record=new StorySaveStore(root).Read(0);
                Require(record.progress.lineIndex==1 && record.screenshotBase64==Convert.ToBase64String(png),"Disk roundtrip");
                Require(b.Restore(record.progress.Resolve(catalog)),b.LastError);
                Require(b.CurrentNode==a.CurrentNode && b.LineIndex==a.LineIndex,"Restore exact line");
                Require(b.CurrentStage.Background==a.CurrentStage.Background && b.CurrentStage.Characters.Count==2,"Inherited stage");
                Require(b.CurrentStage.Characters[0].appearance==a.CurrentStage.Characters[0].appearance &&
                    b.CurrentStage.Characters[0].position==a.CurrentStage.Characters[0].position,"Character pose and position");
                Require(b.Advance() && b.LineIndex==2,"Continue next line");
                Require(b.Advance() && b.State==StoryRunnerState.Transition,"Reach original transition");
                var transition=b.CurrentTransition;
                Require(b.PrepareTransition(transition) && b.CompleteTransition(transition),"Complete original transition");
                Require(b.CurrentNode.name=="Segment02","Reach next node");
                for(int i=0;i<4;i++)Require(b.Advance(),b.LastError);
                Require(b.CurrentNode.name=="SaveTestSegment03" && b.LineIndex==1,"New test node");
                var later=StorySaveData.Capture(b,catalog);
                store.Write(119,later,png,"Page10");
                Require(a.Restore(new StorySaveStore(root).Read(119).progress.Resolve(catalog)),a.LastError);
                Require(a.CurrentStage.Background==b.CurrentStage.Background && a.LineIndex==1,"Page10 restoration");
                Require(a.Advance() && a.Advance() && a.CurrentNode.name=="SaveTestSegment04","Cross node after load");
                int limit=30;while(a.State==StoryRunnerState.Dialogue && limit-->0)Require(a.Advance(),a.LastError);
                Require(a.State==StoryRunnerState.Completed,"All four new nodes reach ending");
                store.Write(0,later,png,"Overwrite");Require(store.Read(0).title=="Overwrite","Atomic overwrite");
                bool rejected=false;try{store.Read(120);}catch(ArgumentOutOfRangeException){rejected=true;}
                Require(rejected,"Page capacity bound");
                later.version=999;rejected=false;try{later.Resolve(catalog);}catch(InvalidOperationException){rejected=true;}
                Require(rejected,"Reject incompatible progress");
                store.Delete(0);Require(new StorySaveStore(root).Read(0)==null,"Deletion persists");
                return "SaveLoadChecks PASS: disk roundtrip, exact line, inherited stage/pose, transition, page10, four-node continuation/ending, overwrite, bounds, incompatible data, deletion.";
            } finally {
                UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(second);UnityEngine.Object.DestroyImmediate(texture);
                // Only this check's unique temporary directory is touched, never the player's slots.
                store.Delete(0);store.Delete(119);
                if(Directory.Exists(root))Directory.Delete(root);
            }
        }
        private static void Require(bool condition,string message) {if(!condition)throw new Exception("SaveLoadChecks: "+message);}
    }
}
