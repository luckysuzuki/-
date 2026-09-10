using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WitchTrial.Characters.Editor
{
    [CustomEditor(typeof(CharacterActor))]
    public sealed class CharacterActorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var actor = (CharacterActor)target;
            EditorGUILayout.LabelField(actor.characterId + " / 表情与动作", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("选择部件后立即预览。位置与渲染顺序已按原始 bundle 恢复。保存外观后，可拖到对话行 Appearance 字段。", MessageType.Info);
            EditorGUILayout.HelpBox("RootBlending 的 Overlay / Softlight 材质尚未完整还原，默认关闭。", MessageType.Warning);
            foreach (var group in actor.branches.Select(b => b.group).Distinct())
            {
                var branches = actor.branches.Where(b => b.group == group).ToArray();
                int current = Array.FindIndex(branches,b => b.root.activeSelf);
                int next = EditorGUILayout.Popup(group, Math.Max(0,current), branches.Select(b => b.id).ToArray());
                if (next != current)
                {
                    Undo.RecordObjects(branches.Select(b => (UnityEngine.Object)b.root).ToArray(), "Change character branch");
                    actor.SelectBranch(group, branches[next].id);
                    foreach(var branch in branches) PrefabUtility.RecordPrefabInstancePropertyModifications(branch.root);
                }
            }
            foreach (var group in actor.Groups)
            {
                var options = new[] { "(无)" }.Concat(actor.Options(group)).ToArray();
                var current = actor.parts.FirstOrDefault(p => p.group == group && p.renderer.enabled && !p.optional);
                int index = current == null ? 0 : Array.IndexOf(options, current.id);
                int next = EditorGUILayout.Popup(group, Math.Max(0, index), options);
                if (next != index) Change(actor, () => actor.Select(group, next == 0 ? null : options[next]));
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("效果 / 遮罩", EditorStyles.boldLabel);
            foreach (var part in actor.parts.Where(p => p.optional))
            {
                bool visible = EditorGUILayout.ToggleLeft(part.id, part.renderer.enabled);
                if (visible != part.renderer.enabled) Change(actor, () => actor.SetVisible(part.id, visible));
            }
            if (GUILayout.Button("恢复默认")) Change(actor, actor.ResetAppearance);
            if (GUILayout.Button("保存为对话外观预设…"))
            {
                string path = EditorUtility.SaveFilePanelInProject("保存外观", actor.characterId + "_Pose", "asset", "选择保存位置");
                if (!string.IsNullOrEmpty(path))
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(actor);
                    if (source == null && EditorUtility.IsPersistent(actor)) source = actor;
                    if (source == null) { Debug.LogError("Please use a character prefab instance."); return; }
                    var pose = CreateInstance<CharacterPose>();
                    pose.characterPrefab = source;
                    pose.visibleParts = actor.VisibleParts;
                    pose.visibleBranches = actor.VisibleBranches;
                    AssetDatabase.CreateAsset(pose, path);
                    AssetDatabase.SaveAssets();
                    EditorGUIUtility.PingObject(pose);
                }
            }
        }
        private static void Change(CharacterActor actor, Action action)
        {
            Undo.RecordObjects(actor.parts.Select(p => (UnityEngine.Object)p.renderer).ToArray(), "Change character appearance");
            action();
            foreach (var part in actor.parts)
            {
                EditorUtility.SetDirty(part.renderer);
                if (PrefabUtility.IsPartOfPrefabInstance(part.renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(part.renderer);
            }
            SceneView.RepaintAll();
        }
    }
}
