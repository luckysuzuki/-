using UnityEditor;
using UnityEngine;
using WitchTrial.Characters;

namespace WitchTrial.Story.Editor
{
    /// <summary>
    /// 在编辑器中创建一套可运行的章节、剧情图与节点示例资源。
    /// </summary>
    public static class StoryChapterExample
    {
        [MenuItem("Witch Trial/Story/Create Chapter Example")]
        public static void Create()
        {
            const string root="Assets/StoryExamples";
            if(!AssetDatabase.IsValidFolder(root)) AssetDatabase.CreateFolder("Assets","StoryExamples");
            var folder=AssetDatabase.GenerateUniqueAssetPath(root+"/ChapterExample");
            AssetDatabase.CreateFolder(root, System.IO.Path.GetFileName(folder));
            var chapter=Save<StoryChapter>(folder,"Chapter01");
            var graph=Save<StoryGraph>(folder,"Graph");
            var first=Save<DialogueNode>(folder,"Segment01");
            var second=Save<DialogueNode>(folder,"Segment02");
            var transition=Save<TransitionNode>(folder,"Transition");
            var end=Save<EndNode>(folder,"ChapterEnd");
            chapter.chapterId="chapter-01"; chapter.title="对话示例"; chapter.graph=graph;
            graph.entry=first; first.segmentId="segment-01"; second.segmentId="segment-02";
            first.next=transition; transition.next=second; second.next=end; end.endingId="chapter-01-complete";
            var ema=AssetDatabase.LoadAssetAtPath<CharacterPose>("Assets/Characters/Imported/ema/Poses/ema_Default.asset");
            var nanoka=AssetDatabase.LoadAssetAtPath<CharacterPose>("Assets/Characters/Imported/nanoka/Poses/nanoka_Default.asset");
            var background=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/resource/Stills/Still_001_001.png");
            var cast=ema!=null && nanoka!=null ? new[] {
                new StageCharacter {instanceId="ema",appearance=ema,position=new Vector3(-3,0,0),sortingOrder=1},
                new StageCharacter {instanceId="nanoka",appearance=nanoka,position=new Vector3(3,0,0),sortingOrder=2} } : new StageCharacter[0];
            first.lines=new[] {
                new DialogueLine {text="房间里安静下来。",castChange=CastChange.Replace,characters=cast,
                    backgroundChange=background==null ? BackgroundChange.Clear : BackgroundChange.Replace,background=background},
                new DialogueLine {speaker="艾玛",speakerId="ema",text="我们开始吧。",castChange=CastChange.Keep},
                new DialogueLine {text="她点了点头。",castChange=CastChange.Keep} };
            second.lines=new[] {
                new DialogueLine {text="转场结束，进入第二段。",castChange=CastChange.Clear,backgroundChange=BackgroundChange.Keep},
                new DialogueLine {speaker="？？？",text="下一段故事即将开始。",castChange=CastChange.Keep} };
            foreach(var asset in new ScriptableObject[]{chapter,graph,first,second,transition,end}) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets(); Selection.activeObject=chapter;
            Debug.Log("Chapter example created: "+folder+". Assign this Chapter to StoryRunner; no scene or UI was created.");
        }
        private static T Save<T>(string folder,string name) where T:ScriptableObject
        { var asset=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,folder+"/"+name+".asset"); return asset; }
    }
}
