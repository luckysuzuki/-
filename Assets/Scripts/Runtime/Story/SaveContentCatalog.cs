using System;
using System.Collections.Generic;
using UnityEngine;

namespace WitchTrial.Story
{
    // Serialized asset references work in players; saves contain GUID/local-file-ID keys, never instance IDs.
    /// <summary>
    /// 保存可持久化剧情资源与稳定标识之间的映射，并负责运行时解析。
    /// </summary>
    public sealed class SaveContentCatalog : ScriptableObject
    {
        /// <summary>
        /// 表示存档资源目录中的一个稳定标识与 Unity 资源引用。
        /// </summary>
        [Serializable] public sealed class Entry { public string id; public UnityEngine.Object asset; }
        public List<Entry> entries = new List<Entry>();
        public static SaveContentCatalog Load() => Resources.Load<SaveContentCatalog>("SaveContentCatalog");
        public string Id(UnityEngine.Object asset)
        {
            if (asset == null) return null;
            var entry = entries.Find(e => e.asset == asset);
            if (entry == null) throw new InvalidOperationException("存档资源目录中缺少：" + asset.name);
            return entry.id;
        }
        public T Resolve<T>(string id) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(id)) return null;
            var entry = entries.Find(e => e.id == id);
            var asset = entry?.asset as T;
            if (asset == null) throw new InvalidOperationException("存档引用的资源已不存在：" + id);
            return asset;
        }
    }
}
