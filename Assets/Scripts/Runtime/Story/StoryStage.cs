using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WitchTrial.Characters;

namespace WitchTrial.Story
{
    /// <summary>
    /// 指定一句对话对当前背景执行保留、替换或清除操作。
    /// </summary>
    public enum BackgroundChange { Keep, Replace, Clear }
    // Zero preserves the meaning of appearance on existing serialized DialogueLine assets.
    /// <summary>
    /// 指定一句对话对当前舞台人物阵容执行的更新方式。
    /// </summary>
    public enum CastChange { LegacyAppearance, Keep, Replace, Clear }

    /// <summary>
    /// 描述舞台中的一个角色实例及其外观、变换、镜像和渲染顺序。
    /// </summary>
    [Serializable]
    public sealed class StageCharacter
    {
        [Tooltip("舞台实例 ID，同一阵容内唯一；不必与说话人相同。")]
        public string instanceId;
        public CharacterPose appearance;
        [Tooltip("相对角色舞台根节点的 Unity 局部坐标。")]
        public Vector3 position;
        public Vector3 scale = Vector3.one;
        public bool mirror;
        [Tooltip("角色整体前后顺序，越大越靠前；保留角色内部部件排序。")]
        public int sortingOrder;

        internal StageCharacter Copy() => new StageCharacter { instanceId = instanceId,
            appearance = appearance, position = position, scale = scale, mirror = mirror, sortingOrder = sortingOrder };
    }

    /// <summary>Resolved current picture, owned by the runner. Does not instantiate scene objects.</summary>
    public sealed class StoryStage
    {
        public Sprite Background { get; }
        public IReadOnlyList<StageCharacter> Characters { get; }
        public StoryStage(Sprite background = null, IEnumerable<StageCharacter> characters = null)
        {
            Background = background;
            Characters = Array.AsReadOnly((characters ?? Array.Empty<StageCharacter>()).Select(c => c.Copy()).ToArray());
        }

        public static StoryStage Resolve(StoryStage previous, DialogueLine line)
        {
            previous = previous ?? new StoryStage();
            var background = line.backgroundChange == BackgroundChange.Keep ? previous.Background
                : line.backgroundChange == BackgroundChange.Replace ? line.background : null;
            IEnumerable<StageCharacter> cast = previous.Characters;
            switch (line.castChange)
            {
                case CastChange.LegacyAppearance:
                    cast = line.appearance == null ? Array.Empty<StageCharacter>() : new[] {
                        new StageCharacter { instanceId = "legacy", appearance = line.appearance } };
                    break;
                case CastChange.Replace: cast = line.characters ?? Array.Empty<StageCharacter>(); break;
                case CastChange.Clear: cast = Array.Empty<StageCharacter>(); break;
            }
            return new StoryStage(background, cast);
        }

        public static void ValidateLine(DialogueLine line, string label, List<string> errors)
        {
            if (!Enum.IsDefined(typeof(CastChange), line.castChange) || !Enum.IsDefined(typeof(BackgroundChange), line.backgroundChange))
                errors.Add(label + ": 不支持的画面操作。");
            if (line.backgroundChange == BackgroundChange.Replace && line.background == null)
                errors.Add(label + ": 替换背景时必须指定图片。");
            if (line.castChange == CastChange.LegacyAppearance && line.appearance != null)
                ValidatePose(line.appearance, label, errors);
            if (line.castChange != CastChange.Replace) return;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var actor in line.characters ?? Array.Empty<StageCharacter>())
            {
                if (actor == null) { errors.Add(label + ": 阵容包含空项。"); continue; }
                if (string.IsNullOrWhiteSpace(actor.instanceId) || !ids.Add(actor.instanceId))
                    errors.Add(label + ": 舞台实例 ID 为空或重复。");
                ValidatePose(actor.appearance, label + "/" + actor.instanceId, errors);
                if (!Finite(actor.position) || !Finite(actor.scale) || actor.scale.x <= 0 || actor.scale.y <= 0 || actor.scale.z <= 0)
                    errors.Add(label + ": 位置必须有限，缩放必须为正；镜像请使用 Mirror。");
                if (actor.sortingOrder < -32768 || actor.sortingOrder > 32767)
                    errors.Add(label + ": sortingOrder 超出 Unity 支持范围。");
            }
        }
        private static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsInfinity(v.x)
            || float.IsNaN(v.y) || float.IsInfinity(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.z));
        private static void ValidatePose(CharacterPose pose, string label, List<string> errors)
        {
            if (pose == null || pose.characterPrefab == null) { errors.Add(label + ": 缺少外观预设或角色预制体。"); return; }
            var actor = pose.characterPrefab;
            var visible = new HashSet<string>(pose.visibleParts ?? Array.Empty<string>());
            if (visible.Any(id => !actor.parts.Any(p => p.id == id))) errors.Add(label + ": 预设引用了不存在的部件。");
            foreach (var group in actor.Groups)
                if (actor.parts.Count(p => p.group == group && !p.optional && visible.Contains(p.id)) > 1)
                    errors.Add(label + ": 同组启用了多个部件：" + group);
            if ((pose.visibleBranches ?? Array.Empty<string>()).Any(id => !actor.branches.Any(b => b.id == id)))
                errors.Add(label + ": 预设引用了不存在的头部分支。");
        }
    }
}
