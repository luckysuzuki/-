using System;
using System.Linq;
using UnityEngine;
using WitchTrial.Story;

namespace WitchTrial.Characters
{
    /// <summary>Controls for the dedicated sample scene; not required by the character prefab.</summary>
    public sealed class CharacterPreviewControls : MonoBehaviour
    {
        public CharacterActor actor;
        public CharacterPose[] presets;
        public StoryGraph sampleStory;
        private StoryRunner runner;
        private string dialogue = "Choose parts or play the dialogue sample.";
        private Vector2 scroll;
        private GUIStyle title, label;
        private void Start()
        {
            runner = gameObject.AddComponent<StoryRunner>();
            runner.DialogueLineChanged += ShowLine;
        }
        private void ShowLine(DialogueLine line)
        {
            dialogue = line.text;
            if (line.appearance != null) actor.Apply(line.appearance);
        }
        private void OnDestroy() { if (runner != null) runner.DialogueLineChanged -= ShowLine; }
        private void OnGUI()
        {
            if (actor == null) return;
            float factor = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(factor, factor, 1));
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                label = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            }
            GUILayout.BeginArea(new Rect(32, 36, 405, 810), GUI.skin.box);
            GUILayout.Label("SHERRY / CHARACTER LAB", title);
            GUILayout.Label("89 original parts • original transforms & sorting", label);
            GUILayout.Space(15);
            if (sampleStory != null)
            {
                if (GUILayout.Button("Play dialogue sample", GUILayout.Height(32))) runner.Play(sampleStory);
                if (GUILayout.Button("Next dialogue line", GUILayout.Height(28)) && runner.State == StoryRunnerState.Dialogue) runner.Advance();
                GUILayout.Label(dialogue, label);
                GUILayout.Space(10);
            }
            foreach (var preset in presets ?? Array.Empty<CharacterPose>())
                if (preset != null && GUILayout.Button(preset.name, GUILayout.Height(32))) actor.Apply(preset);
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var group in actor.Groups)
            {
                var options = actor.Options(group);
                var current = actor.parts.FirstOrDefault(p => p.group == group && p.renderer.enabled && !p.optional);
                int index = current == null ? -1 : Array.IndexOf(options, current.id);
                GUILayout.Space(12);
                GUILayout.Label(group, label);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", GUILayout.Width(38), GUILayout.Height(30)))
                    actor.Select(group, options[(index - 1 + options.Length) % options.Length]);
                GUILayout.Label(current == null ? "None" : current.id, label, GUILayout.Width(270));
                if (GUILayout.Button(">", GUILayout.Width(38), GUILayout.Height(30)))
                    actor.Select(group, options[(index + 1) % options.Length]);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Reset appearance", GUILayout.Height(36))) actor.ResetAppearance();
            GUILayout.EndArea();
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
