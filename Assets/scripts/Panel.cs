using System;
using WitchTrial.UI;

namespace DefaultNamespace
{
    /// <summary>
    /// Temporary source-compatibility bridge for scripts based on the first draft.
    /// New panels should inherit UIPanel directly.
    /// </summary>
    [Obsolete("Use WitchTrial.UI.UIPanel instead.")]
    public abstract class Panel : UIPanel
    {
        public PanelType PanelType => Layer == UILayer.Popup
            ? DefaultNamespace.PanelType.pop
            : DefaultNamespace.PanelType.fullscreen;
    }
}
