using System;
using UnityEngine;

namespace WitchTrial.Characters
{
    [CreateAssetMenu(fileName = "CharacterPose", menuName = "Witch Trial/Characters/Appearance Preset")]
    public sealed class CharacterPose : ScriptableObject
    {
        public CharacterActor characterPrefab;
        public string[] visibleParts = Array.Empty<string>();
        public string[] visibleBranches = Array.Empty<string>();
    }
}
