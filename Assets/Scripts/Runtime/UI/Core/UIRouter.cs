using System;

namespace WitchTrial.UI
{
    /// <summary>
    /// Stable entry point used by views and game systems. Callers do not need a
    /// reference to UIManager and the implementation can be replaced in tests.
    /// </summary>
    public static class UIRouter
    {
        private static IUIService _service;

        public static bool IsReady => _service != null;

        internal static void Bind(IUIService service)
        {
            _service = service;
        }

        internal static void Unbind(IUIService service)
        {
            if (ReferenceEquals(_service, service))
            {
                _service = null;
            }
        }

        public static T Open<T>(object context = null) where T : UIPanel
        {
            return RequireService().Open<T>(context);
        }

        public static UIPanel Open(Type panelType, object context = null)
        {
            return RequireService().Open(panelType, context);
        }

        public static T Replace<T>(object context = null) where T : UIPanel
        {
            return RequireService().Replace<T>(context);
        }

        public static UIPanel Replace(Type panelType, object context = null)
        {
            return RequireService().Replace(panelType, context);
        }

        public static bool Close<T>() where T : UIPanel
        {
            return RequireService().Close<T>();
        }

        public static bool Close(UIPanel panel)
        {
            return panel != null && RequireService().Close(panel);
        }

        public static bool Back()
        {
            return RequireService().Back();
        }

        public static bool IsOpen<T>() where T : UIPanel
        {
            return RequireService().IsOpen<T>();
        }

        public static T Get<T>() where T : UIPanel
        {
            return RequireService().Get<T>();
        }

        private static IUIService RequireService()
        {
            if (_service == null)
            {
                throw new InvalidOperationException(
                    "UIRouter is not ready. Add an active UIManager to the bootstrap scene.");
            }

            return _service;
        }
    }
}
