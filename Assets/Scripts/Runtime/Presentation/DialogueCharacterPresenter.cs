using UnityEngine;
using WitchTrial.Story;

namespace WitchTrial.Characters
{
    /// <summary>Attach to a character stage and assign StoryRunner. Each line contains a complete pose.</summary>
    public sealed class DialogueCharacterPresenter : MonoBehaviour
    {
        public StoryRunner runner;
        public CharacterActor Actor { get; private set; }
        private CharacterActor sourcePrefab;
        private StoryRunner subscribedRunner;

        private void OnEnable()
        {
            subscribedRunner = runner;
            if (subscribedRunner == null) return;
            subscribedRunner.DialogueLineChanged += Show;
            subscribedRunner.Stopped += Clear;
            if (subscribedRunner.CurrentLine != null) Show(subscribedRunner.CurrentLine);
        }
        private void OnDisable()
        {
            if (subscribedRunner != null)
            {
                subscribedRunner.DialogueLineChanged -= Show;
                subscribedRunner.Stopped -= Clear;
            }
            subscribedRunner = null;
            Clear();
        }
        public void Show(DialogueLine line)
        {
            var pose = line != null ? line.appearance : null;
            if (pose == null || pose.characterPrefab == null) { Clear(); return; }
            if (Actor == null || sourcePrefab != pose.characterPrefab)
            {
                Clear();
                sourcePrefab = pose.characterPrefab;
                Actor = Instantiate(sourcePrefab, transform);
                Actor.transform.localPosition = Vector3.zero;
                Actor.transform.localRotation = Quaternion.identity;
            }
            Actor.Apply(pose);
        }
        public void Clear()
        {
            if (Actor != null)
            {
                Actor.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(Actor.gameObject); else DestroyImmediate(Actor.gameObject);
            }
            Actor = null;
            sourcePrefab = null;
        }
    }
}
