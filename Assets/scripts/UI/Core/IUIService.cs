using System;

namespace WitchTrial.UI
{
    public interface IUIService
    {
        event Action<UIPanel> PanelOpened;
        event Action<UIPanel> PanelClosed;

        T Open<T>(object context = null) where T : UIPanel;
        UIPanel Open(Type panelType, object context = null);
        T Replace<T>(object context = null) where T : UIPanel;
        UIPanel Replace(Type panelType, object context = null);
        bool Close<T>() where T : UIPanel;
        bool Close(UIPanel panel);
        bool Back();
        bool IsOpen<T>() where T : UIPanel;
        T Get<T>() where T : UIPanel;
    }
}
