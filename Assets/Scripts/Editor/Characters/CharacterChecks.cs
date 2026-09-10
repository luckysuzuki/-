using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using WitchTrial.Story;

namespace WitchTrial.Characters.Editor
{
    public static class CharacterChecks
    {
        [MenuItem("Witch Trial/Characters/Run Character Checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode before running character integration checks.");
            var prefab = AssetDatabase.LoadAssetAtPath<CharacterActor>(CharacterImporter.Root + "/sherry/sherry.prefab");
            if (prefab == null || prefab.parts.Length != 89) throw new Exception("Sherry prefab must contain 89 parts.");
            var host = new GameObject("Temporary Character Checks");
            host.SetActive(false);
            var actor = UnityEngine.Object.Instantiate(prefab, host.transform);
            var graph = ScriptableObject.CreateInstance<StoryGraph>();
            var dialogue = ScriptableObject.CreateInstance<DialogueNode>();
            var ending = ScriptableObject.CreateInstance<EndNode>();
            ending.endingId = "character-check";
            try
            {
                var data = JObject.Parse(File.ReadAllText(CharacterImporter.Root + "/sherry/layout.json"));
                foreach (var p in (JArray)data["parts"])
                {
                    var part = actor.parts.Single(x => x.id == (string)p["name"]);
                    var position = p["localPosition"];
                    var expected = new Vector3((float)position["x"], (float)position["y"], (float)position["z"]);
                    if (Vector3.Distance(part.renderer.transform.localPosition, expected) > .00001f) throw new Exception("Position mismatch: " + part.id);
                    if (part.renderer.sortingOrder != (int)p["sortingOrder"]) throw new Exception("Order mismatch: " + part.id);
                    if (part.renderer.sprite == null || !Mathf.Approximately(part.renderer.sprite.pixelsPerUnit, (float)p["sprite"]["pixelsToUnits"])) throw new Exception("Sprite import mismatch: " + part.id);
                }
                foreach (var group in actor.Groups)
                {
                    foreach (var id in actor.Options(group))
                    {
                        actor.Select(group,id);
                        if (actor.parts.Count(p => p.group == group && !p.optional && p.renderer.enabled) != 1)
                            throw new Exception("Exclusive part selection failed: " + id);
                    }
                }
                var normal = AssetDatabase.LoadAssetAtPath<CharacterPose>(CharacterImporter.Root + "/sherry/Poses/sherry_Default.asset");
                var angry = AssetDatabase.LoadAssetAtPath<CharacterPose>(CharacterImporter.Root + "/sherry/Poses/sherry_Angry.asset");
                actor.Apply(normal);
                if (!actor.VisibleParts.OrderBy(x=>x).SequenceEqual(normal.visibleParts.OrderBy(x=>x))) throw new Exception("Preset application mismatch");
                var runner = host.AddComponent<StoryRunner>();
                var stage = new GameObject("Stage"); stage.transform.SetParent(host.transform);
                var presenter = stage.AddComponent<DialogueCharacterPresenter>(); presenter.runner = runner;
                dialogue.lines = new[] { new DialogueLine{ speaker="Sherry",text="Default",appearance=normal }, new DialogueLine{speaker="Sherry",text="Angry",appearance=angry}, new DialogueLine{speaker="Narrator",text="No actor"} };
                dialogue.next = ending; graph.entry = dialogue;
                host.SetActive(true);
                if (!runner.Play(graph) || presenter.Actor == null) throw new Exception("Dialogue presenter did not create actor");
                runner.Advance();
                if (!presenter.Actor.VisibleParts.Contains("Eyes_Angry_Open01")) throw new Exception("Dialogue appearance did not change");
                runner.Advance();
                if (presenter.Actor != null) throw new Exception("Null appearance should hide character");
                Debug.Log("Character checks PASS: 89 transforms / sprite PPU / sorting orders, all exclusive choices, preset restore, StoryRunner default-to-angry-to-hidden.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(dialogue);
                UnityEngine.Object.DestroyImmediate(ending);
            }
        }
    }
}
