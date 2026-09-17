using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using WitchTrial.Story;

namespace WitchTrial.Characters
{
    /// <summary>Multi-character SpriteRenderer stage; bind to a world-space stage root.</summary>
    public sealed class DialogueCharacterPresenter : MonoBehaviour
    {
        public StoryRunner runner;
        public string sortingLayerName = "Default";
        /// <summary>
        /// 表示存档资源目录中的一个稳定标识与 Unity 资源引用。
        /// </summary>
        private sealed class Entry { public CharacterActor actor; public CharacterActor prefab; }
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private StoryRunner subscribedRunner;
        private StoryStage displayed = new StoryStage();
        public CharacterActor Actor => entries.Values.Select(e => e.actor).FirstOrDefault();
        public IReadOnlyList<CharacterActor> Actors => entries.Values.Select(e => e.actor).ToArray();
        private void OnEnable()
        {
            subscribedRunner = runner;
            if (subscribedRunner == null) return;
            subscribedRunner.StageChanged += ShowStage;
            subscribedRunner.Stopped += Clear;
            ShowStage(subscribedRunner.CurrentStage);
        }
        private void OnDisable()
        {
            if (subscribedRunner != null)
            { subscribedRunner.StageChanged -= ShowStage; subscribedRunner.Stopped -= Clear; }
            subscribedRunner = null;
            Clear();
        }
        public void Show(DialogueLine line)
        {
            if (line == null) { Clear(); return; }
            ShowStage(StoryStage.Resolve(displayed, line));
        }
        public void ShowStage(StoryStage stage)
        {
            stage = stage ?? new StoryStage();
            var desired = new HashSet<string>(stage.Characters.Select(c => c.instanceId));
            foreach (var id in entries.Keys.Where(id => !desired.Contains(id)).ToArray()) Remove(id);
            foreach (var character in stage.Characters)
            {
                var prefab = character.appearance.characterPrefab;
                if (entries.TryGetValue(character.instanceId, out var entry) && (entry.prefab != prefab || entry.actor == null))
                { Remove(character.instanceId); entry = null; }
                if (entry == null)
                {
                    entry = new Entry { prefab = prefab, actor = Instantiate(prefab, transform) };
                    entry.actor.name = character.instanceId;
                    entries.Add(character.instanceId, entry);
                }
                entry.actor.Apply(character.appearance);
                var tr = entry.actor.transform;
                tr.localPosition = character.position;
                tr.localRotation = Quaternion.identity;
                var scale = Vector3.Scale(prefab.transform.localScale, character.scale);
                if (character.mirror) scale.x = -scale.x;
                tr.localScale = scale;
                var group = entry.actor.GetComponent<SortingGroup>();
                if (group == null) group = entry.actor.gameObject.AddComponent<SortingGroup>();
                group.sortingLayerName = sortingLayerName;
                group.sortingOrder = character.sortingOrder;
            }
            displayed = stage;
        }
        private void Remove(string id)
        {
            var actor = entries[id].actor;
            if (actor != null)
            {
                actor.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(actor.gameObject); else DestroyImmediate(actor.gameObject);
            }
            entries.Remove(id);
        }
        public void Clear()
        {
            foreach (var id in entries.Keys.ToArray()) Remove(id);
            displayed = new StoryStage();
        }
    }
}
