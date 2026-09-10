#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WitchTrial.Story
{
    /// <summary>Opt-in Play-mode preview. Never changes or saves the authored scene.</summary>
    public sealed class StoryRuntimePreview : MonoBehaviour
    {
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private StoryRunner runner;
        private Text speaker, dialogue, autoLabel;
        private Font font;
        private bool automatic;
        private float elapsed;

        [MenuItem("Witch Trial/Story/Open Runtime Preview (Play Mode)")]
        public static void Open()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode before opening the story preview."); return; }
            if (FindAnyObjectByType<StoryRuntimePreview>() != null) return;
            new GameObject("Story Runtime Preview (temporary)").AddComponent<StoryRuntimePreview>().Build();
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimSun" }, 36);
            var background = Rect("Existing Emma CG", 0,0,1,1).gameObject.AddComponent<RawImage>();
            background.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/resource/Still_001_001.png");
            background.color = new Color(.72f,.72f,.78f,1);
            var nextArea = background.gameObject.AddComponent<Button>();
            nextArea.onClick.AddListener(Next);
            var shade = Rect("Dialogue shade",0,0,1,.31f).gameObject.AddComponent<Image>();
            shade.color = new Color(.025f,.018f,.045f,.88f);
            shade.raycastTarget = false;
            speaker = Label("Speaker",.18f,.225f,.6f,.31f,56,new Color(1,.53f,.68f));
            dialogue = Label("Dialogue",.22f,.08f,.83f,.22f,34,Color.white);
            Button("Auto",.025f,.025f,.105f,.075f,ToggleAuto,out autoLabel);
            Text unused;
            Button("继续  ›",.83f,.04f,.94f,.095f,Next,out unused);
            Button("返回",.9f,.91f,.98f,.97f,()=>Destroy(gameObject),out unused);
            runner = gameObject.AddComponent<StoryRunner>();
            runner.DialogueLineChanged += ShowLine;
            runner.Completed += ShowEnd;
            var graph = Make<StoryGraph>();
            var intro = Make<DialogueNode>();
            var end = Make<EndNode>();
            graph.entry = intro;
            intro.lines = new[] {
                new DialogueLine { speaker="樱羽艾玛", text="为什么会变成这样……\n完全搞不懂啊……" },
                new DialogueLine { speaker="樱羽艾玛", text="先冷静下来，整理一下眼前的情况。" }
            };
            intro.next = end;
            end.endingId = "runtime-preview";
            if (!runner.Play(graph)) throw new InvalidOperationException(runner.LastError);
        }

        private T Make<T>() where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            value.hideFlags = HideFlags.DontSave;
            assets.Add(value);
            return value;
        }

        private RectTransform Rect(string label,float x0,float y0,float x1,float y1)
        {
            var rect = new GameObject(label,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform,false);
            rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            return rect;
        }

        private Text Label(string label,float x0,float y0,float x1,float y1,int size,Color color)
        {
            var text=Rect(label,x0,y0,x1,y1).gameObject.AddComponent<Text>();
            text.font=font; text.fontSize=size; text.color=color;
            text.alignment=TextAnchor.MiddleLeft; text.raycastTarget=false;
            return text;
        }

        private void Button(string label,float x0,float y0,float x1,float y1,UnityEngine.Events.UnityAction action,out Text text)
        {
            var rect=Rect(label,x0,y0,x1,y1);
            var image=rect.gameObject.AddComponent<Image>(); image.color=new Color(.16f,.12f,.19f,.9f);
            rect.gameObject.AddComponent<Button>().onClick.AddListener(action);
            text=Label(label+" label",x0,y0,x1,y1,26,new Color(.95f,.88f,.82f));
            text.alignment=TextAnchor.MiddleCenter; text.text=label;
        }

        private void ShowLine(DialogueLine line) { speaker.text=line.speaker; dialogue.text=line.text; elapsed=0; }
        private void ShowEnd(EndNode node) { automatic=false; autoLabel.text="Auto"; dialogue.text="预览结束"; }
        private void ToggleAuto() { automatic=!automatic; autoLabel.text=automatic?"Auto ●":"Auto"; elapsed=0; }
        private void Next() { if(runner!=null && runner.State==StoryRunnerState.Dialogue) runner.Advance(); }
        private void Update() { if(automatic) { elapsed+=Time.unscaledDeltaTime; if(elapsed>=3f) { elapsed=0; Next(); } } }

        [MenuItem("Witch Trial/Story/Check Runtime Preview")]
        public static void Check()
        {
            var view=FindAnyObjectByType<StoryRuntimePreview>();
            if(view==null) throw new InvalidOperationException("Open runtime preview first.");
            view.runner.Play();
            if(view.dialogue.text!=view.runner.CurrentLine.text) throw new InvalidOperationException("First line binding failed");
            view.Next();
            if(view.runner.LineIndex!=1 || view.dialogue.text!=view.runner.CurrentLine.text) throw new InvalidOperationException("Advance binding failed");
            view.Next();
            if(view.runner.State!=StoryRunnerState.Completed || view.dialogue.text!="预览结束") throw new InvalidOperationException("Completion failed");
            view.runner.Play();
            view.ToggleAuto();
            view.elapsed=3f; view.Update();
            if(view.runner.LineIndex!=1) throw new InvalidOperationException("Auto advance failed");
            view.ToggleAuto(); view.runner.Play();
            Debug.Log("Story runtime preview: PASS (line binding, advance, completion, restart, auto)");
        }

        private void OnDestroy()
        {
            if(runner!=null) { runner.DialogueLineChanged-=ShowLine; runner.Completed-=ShowEnd; }
            foreach(var asset in assets) if(asset!=null) Destroy(asset);
            if(font!=null) Destroy(font);
        }
    }
}
#endif
