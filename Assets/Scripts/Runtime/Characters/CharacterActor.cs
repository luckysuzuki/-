using System;
using System.Linq;
using UnityEngine;

namespace WitchTrial.Characters
{
    [Serializable]
    public sealed class CharacterPart
    {
        public string id;
        public string group;
        public SpriteRenderer renderer;
        public bool defaultVisible;
        public bool optional;
    }

    [Serializable]
    public sealed class CharacterBranch
    {
        public string id;
        public string group;
        public GameObject root;
        public bool defaultVisible;
    }

    /// <summary>Preserves each part's authored transform, sorting and material; switches visibility only.</summary>
    public sealed class CharacterActor : MonoBehaviour
    {
        public string characterId;
        public CharacterPart[] parts = Array.Empty<CharacterPart>();
        public CharacterBranch[] branches = Array.Empty<CharacterBranch>();

        public string[] Groups => parts.Where(p => !p.optional && (p.group != "Body" || parts.Count(x => x.group == "Body") > 1))
            .Select(p => p.group).Distinct().OrderBy(x => x).ToArray();
        public string[] VisibleParts => parts.Where(p => p.renderer != null && p.renderer.enabled).Select(p => p.id).ToArray();
        public string[] VisibleBranches => branches.Where(b => b.root.activeSelf).Select(b => b.id).ToArray();
        public string[] Options(string group) => parts.Where(p => p.group == group && !p.optional).Select(p => p.id).OrderBy(x => x).ToArray();

        public void Select(string group, string id)
        {
            if (!string.IsNullOrEmpty(id) && !parts.Any(p => p.group == group && p.id == id))
                throw new ArgumentException("Unknown character part: " + id);
            foreach (var part in parts.Where(p => p.group == group && !p.optional))
                part.renderer.enabled = part.id == id;
            if (!string.IsNullOrEmpty(id))
            {
                if (group == "Arms") { Select("ArmL", null); Select("ArmR", null); }
                else if (group == "ArmL" || group == "ArmR") Select("Arms", null);
            }
        }

        public void SelectBranch(string group, string id)
        {
            if (!branches.Any(b => b.group == group && b.id == id)) throw new ArgumentException("Unknown branch: " + id);
            foreach (var branch in branches.Where(b => b.group == group)) branch.root.SetActive(branch.id == id);
        }

        public void SetVisible(string id, bool visible)
        {
            var part = parts.FirstOrDefault(p => p.id == id);
            if (part == null) throw new ArgumentException("Unknown character part: " + id);
            part.renderer.enabled = visible;
        }

        public void ResetAppearance()
        {
            foreach (var part in parts) if (part.renderer != null) part.renderer.enabled = part.defaultVisible;
            foreach (var branch in branches) branch.root.SetActive(branch.defaultVisible);
        }

        public void Apply(CharacterPose pose)
        {
            if (pose == null || pose.characterPrefab == null || pose.characterPrefab.characterId != characterId)
                throw new ArgumentException("Pose does not belong to this character.");
            ApplyParts(pose.visibleParts);
            foreach (var branch in branches) branch.root.SetActive(pose.visibleBranches == null || pose.visibleBranches.Length == 0 ? branch.defaultVisible : pose.visibleBranches.Contains(branch.id));
        }

        public void ApplyParts(string[] ids)
        {
            ids = ids ?? Array.Empty<string>();
            foreach (var id in ids)
                if (!parts.Any(p => p.id == id)) throw new ArgumentException("Unknown character part: " + id);
            foreach (var group in Groups)
                if (parts.Count(p => p.group == group && !p.optional && ids.Contains(p.id)) > 1)
                    throw new ArgumentException("Select only one part in group: " + group);
            foreach (var part in parts) part.renderer.enabled = ids.Contains(part.id);
        }
    }
}
