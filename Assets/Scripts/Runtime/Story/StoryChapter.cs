using System.Collections.Generic;
using UnityEngine;

namespace WitchTrial.Story
{
    /// <summary>
    /// 定义一个章节的标识、标题、剧情图以及可选的下一章节。
    /// </summary>
    [CreateAssetMenu(fileName = "Chapter", menuName = "Witch Trial/Story/Chapter")]
    public sealed class StoryChapter : ScriptableObject
    {
        public string chapterId;
        public string title;
        public StoryGraph graph;
        [Tooltip("只记录下一章，不自动播放。由 Game Panel / 游戏流程决定何时进入。")]
        public StoryChapter nextChapter;

        public List<string> Validate()
        {
            var errors = graph == null ? new List<string> { "Chapter: 未配置剧情图。" } : graph.Validate();
            if (string.IsNullOrWhiteSpace(chapterId)) errors.Add("Chapter: chapterId 不能为空。");
            return errors;
        }
    }
}
