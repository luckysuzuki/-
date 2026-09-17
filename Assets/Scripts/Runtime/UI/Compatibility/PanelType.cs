using System;

namespace DefaultNamespace
{
    /// <summary>
    /// 列出旧版界面代码使用的面板类型，供兼容层转换。
    /// </summary>
    [Obsolete("Use WitchTrial.UI.UILayer instead.")]
    public enum PanelType
    {
        fullscreen,
        pop
    }
}
