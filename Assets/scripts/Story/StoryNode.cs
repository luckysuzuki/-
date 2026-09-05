using UnityEngine;

namespace WitchTrial.Story
{
    /// <summary>剧情数据资产；运行进度由 StoryRunner 持有，不写回资产。</summary>
    public abstract class StoryNode : ScriptableObject
    {
        [TextArea] public string editorNotes;
    }
}
