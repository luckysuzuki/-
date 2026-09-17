using UnityEngine;
using UnityEngine.UI;
using WitchTrial.Story;

namespace WitchTrial.Presentation
{
    /// <summary>
    /// 监听剧情舞台变化，并把当前背景精灵显示到场景中的渲染器。
    /// </summary>
    public sealed class StoryBackgroundPresenter : MonoBehaviour
    {
        public StoryRunner runner;
        [Tooltip("二选一。使用 Image 时确保 Canvas 在人物之后绘制。")]
        public Image backgroundImage;
        public SpriteRenderer backgroundRenderer;
        private StoryRunner subscribedRunner;
        private void OnEnable()
        {
            subscribedRunner = runner;
            if (subscribedRunner == null) return;
            subscribedRunner.StageChanged += Show;
            Show(subscribedRunner.CurrentStage);
        }
        private void OnDisable()
        {
            if (subscribedRunner != null) subscribedRunner.StageChanged -= Show;
            subscribedRunner = null;
            Show(new StoryStage());
        }
        private void Show(StoryStage stage)
        {
            if (backgroundImage != null) { backgroundImage.sprite = stage.Background; backgroundImage.enabled = stage.Background != null; }
            if (backgroundRenderer != null) { backgroundRenderer.sprite = stage.Background; backgroundRenderer.enabled = stage.Background != null; }
        }
    }
}
