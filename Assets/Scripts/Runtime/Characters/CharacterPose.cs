using System;
using UnityEngine;

namespace WitchTrial.Characters
{
    /// <summary>
    /// 保存角色预制体及其可见部件、分支组合，作为可复用外观预设。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterPose", menuName = "Witch Trial/Characters/Appearance Preset")]
    public sealed class CharacterPose : ScriptableObject
    {
        public CharacterActor characterPrefab;
        public string[] visibleParts = Array.Empty<string>();
        public string[] visibleBranches = Array.Empty<string>();
    }
}
