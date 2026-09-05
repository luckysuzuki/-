using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WitchTrial.UI
{
    /// <summary>
    /// Owns the fullscreen and popup stacks. UI callers should use UIRouter
    /// instead of holding a reference to this component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour, IUIService
    {
        [Header("Lifetime")]
        [SerializeField] private bool persistentAcrossScenes = true;

        [Header("Canvas (created automatically when empty)")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private RectTransform fullscreenRoot;
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private Image popupMask;
        [SerializeField] private Color popupMaskColor = new Color(0f, 0f, 0f, 0.72f);

        [Header("Panel sources")]
        [SerializeField] private UIRegistry registry;
        [Tooltip("Optional panel instances already placed in the bootstrap scene.")]
        [SerializeField] private List<UIPanel> scenePanels = new List<UIPanel>();
        [SerializeField] private UIPanel initialPage;

        private readonly Dictionary<Type, UIPanel> _instances = new Dictionary<Type, UIPanel>();
        private readonly List<UIPanel> _fullscreenStack = new List<UIPanel>();
        private readonly List<UIPanel> _popupStack = new List<UIPanel>();
        private Button _popupMaskButton;

        public static UIManager Instance { get; private set; }

        public event Action<UIPanel> PanelOpened;
        public event Action<UIPanel> PanelClosed;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            // Unity invokes OnEnable again after an assembly/domain reload while
            // the Editor is playing. Rebuild the non-serialized runtime state so
            // the initial page is not left hidden after that reload.
            Initialize();
        }

        private void Initialize()
        {
            var hasExpectedRuntimeState = Instance == this
                && _instances.Count > 0
                && (initialPage == null || _fullscreenStack.Count > 0);
            if (hasExpectedRuntimeState)
            {
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistentAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            _instances.Clear();
            _fullscreenStack.Clear();
            _popupStack.Clear();
            EnsureHierarchy();
            RegisterScenePanels();
            UIRouter.Bind(this);
            OpenInitialPageIfNeeded();
        }

        private void Start()
        {
            OpenInitialPageIfNeeded();
        }

        private void OpenInitialPageIfNeeded()
        {
            if (_fullscreenStack.Count == 0 && initialPage != null)
            {
                Open(initialPage.GetType());
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UIRouter.Unbind(this);
                Instance = null;
            }

            if (_popupMaskButton != null)
            {
                _popupMaskButton.onClick.RemoveListener(HandleMaskClicked);
            }
        }

        public T Open<T>(object context = null) where T : UIPanel
        {
            return (T)Open(typeof(T), context);
        }

        public UIPanel Open(Type panelType, object context = null)
        {
            var panel = GetOrCreate(panelType);
            if (panel.Layer == UILayer.Popup)
            {
                OpenPopup(panel, context);
            }
            else
            {
                OpenFullscreen(panel, context);
            }

            return panel;
        }

        public T Replace<T>(object context = null) where T : UIPanel
        {
            return (T)Replace(typeof(T), context);
        }

        public UIPanel Replace(Type panelType, object context = null)
        {
            var panel = GetOrCreate(panelType);
            if (panel.Layer != UILayer.Fullscreen)
            {
                throw new InvalidOperationException(
                    $"Only fullscreen panels can replace the page stack. {panelType.Name} is {panel.Layer}.");
            }

            CloseAllPopups();
            ClearFullscreenStackExcept(panel);
            if (panel.IsOpen)
            {
                _fullscreenStack.Add(panel);
                if (!panel.IsVisible)
                {
                    panel.RevealInternal();
                }

                panel.RefreshInternal(context);
                return panel;
            }

            OpenFullscreen(panel, context);
            return panel;
        }

        public bool Close<T>() where T : UIPanel
        {
            if (!_instances.TryGetValue(typeof(T), out var panel))
            {
                return false;
            }

            return Close(panel);
        }

        public bool Close(UIPanel panel)
        {
            if (panel == null)
            {
                return false;
            }

            var popupIndex = _popupStack.IndexOf(panel);
            if (popupIndex >= 0)
            {
                _popupStack.RemoveAt(popupIndex);
                ClosePanelInstance(panel);
                RefreshPopupOrder();
                return true;
            }

            var pageIndex = _fullscreenStack.IndexOf(panel);
            if (pageIndex < 0)
            {
                return false;
            }

            // Keep at least one fullscreen page visible during normal navigation.
            if (_fullscreenStack.Count == 1)
            {
                return false;
            }

            var wasTop = pageIndex == _fullscreenStack.Count - 1;
            _fullscreenStack.RemoveAt(pageIndex);
            ClosePanelInstance(panel);

            if (wasTop)
            {
                _fullscreenStack[_fullscreenStack.Count - 1].RevealInternal();
            }

            return true;
        }

        public bool Back()
        {
            if (_popupStack.Count > 0)
            {
                return Close(_popupStack[_popupStack.Count - 1]);
            }

            if (_fullscreenStack.Count > 1)
            {
                return Close(_fullscreenStack[_fullscreenStack.Count - 1]);
            }

            return false;
        }

        public bool IsOpen<T>() where T : UIPanel
        {
            if (!_instances.TryGetValue(typeof(T), out var panel))
            {
                return false;
            }

            return _fullscreenStack.Contains(panel) || _popupStack.Contains(panel);
        }

        public T Get<T>() where T : UIPanel
        {
            return _instances.TryGetValue(typeof(T), out var panel) ? panel as T : null;
        }

        public void CloseAllPopups()
        {
            for (var i = _popupStack.Count - 1; i >= 0; i--)
            {
                ClosePanelInstance(_popupStack[i]);
            }

            _popupStack.Clear();
            RefreshPopupOrder();
        }

        private void OpenFullscreen(UIPanel panel, object context)
        {
            CloseAllPopups();

            var existingIndex = _fullscreenStack.IndexOf(panel);
            if (existingIndex >= 0 && existingIndex == _fullscreenStack.Count - 1)
            {
                panel.RefreshInternal(context);
                return;
            }

            if (existingIndex >= 0)
            {
                for (var i = _fullscreenStack.Count - 1; i > existingIndex; i--)
                {
                    var pageToClose = _fullscreenStack[i];
                    _fullscreenStack.RemoveAt(i);
                    ClosePanelInstance(pageToClose);
                }

                panel.RevealInternal();
                panel.RefreshInternal(context);
                return;
            }

            if (_fullscreenStack.Count > 0)
            {
                _fullscreenStack[_fullscreenStack.Count - 1].CoverInternal();
            }

            _fullscreenStack.Add(panel);
            panel.OpenInternal(context);
            PanelOpened?.Invoke(panel);
        }

        private void OpenPopup(UIPanel panel, object context)
        {
            var existingIndex = _popupStack.IndexOf(panel);
            if (existingIndex >= 0)
            {
                _popupStack.RemoveAt(existingIndex);
                _popupStack.Add(panel);
                panel.transform.SetAsLastSibling();
                panel.RefreshInternal(context);
                RefreshPopupOrder();
                return;
            }

            _popupStack.Add(panel);
            panel.OpenInternal(context);
            RefreshPopupOrder();
            PanelOpened?.Invoke(panel);
        }

        private UIPanel GetOrCreate(Type panelType)
        {
            if (panelType == null || !typeof(UIPanel).IsAssignableFrom(panelType) || panelType.IsAbstract)
            {
                throw new ArgumentException("panelType must be a concrete UIPanel type.", nameof(panelType));
            }

            if (_instances.TryGetValue(panelType, out var existing) && existing != null)
            {
                return existing;
            }

            if (registry == null || !registry.TryGetPrefab(panelType, out var prefab))
            {
                throw new InvalidOperationException(
                    $"No panel of type {panelType.Name} is registered. Add its prefab to UIRegistry " +
                    "or add a scene instance to UIManager.Scene Panels.");
            }

            var parent = prefab.Layer == UILayer.Popup ? popupRoot : fullscreenRoot;
            var instance = Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            RegisterPanel(instance, false);
            return instance;
        }

        private void RegisterScenePanels()
        {
            if (initialPage != null && !scenePanels.Contains(initialPage))
            {
                scenePanels.Insert(0, initialPage);
            }

            for (var i = 0; i < scenePanels.Count; i++)
            {
                var panel = scenePanels[i];
                if (panel != null)
                {
                    RegisterPanel(panel, true);
                }
            }
        }

        private void RegisterPanel(UIPanel panel, bool moveToLayerRoot)
        {
            var panelType = panel.GetType();
            if (_instances.TryGetValue(panelType, out var duplicate) && duplicate != panel)
            {
                throw new InvalidOperationException(
                    $"Only one cached instance per panel type is supported. Duplicate: {panelType.Name}.");
            }

            if (moveToLayerRoot)
            {
                var expectedRoot = panel.Layer == UILayer.Popup ? popupRoot : fullscreenRoot;
                panel.transform.SetParent(expectedRoot, false);
            }

            _instances[panelType] = panel;
            panel.PrepareForManager();
            panel.HideImmediately();
        }

        private void ClosePanelInstance(UIPanel panel)
        {
            var panelType = panel.GetType();
            panel.CloseInternal();
            PanelClosed?.Invoke(panel);

            if (!panel.KeepAlive)
            {
                _instances.Remove(panelType);
                Destroy(panel.gameObject);
            }
        }

        private void ClearFullscreenStackExcept(UIPanel panelToKeep)
        {
            for (var i = _fullscreenStack.Count - 1; i >= 0; i--)
            {
                var panel = _fullscreenStack[i];
                if (panel != panelToKeep)
                {
                    ClosePanelInstance(panel);
                }
            }

            _fullscreenStack.Clear();
        }

        private void RefreshPopupOrder()
        {
            var hasPopup = _popupStack.Count > 0;
            popupMask.gameObject.SetActive(hasPopup);
            if (!hasPopup)
            {
                return;
            }

            popupMask.transform.SetAsFirstSibling();
            for (var i = 0; i < _popupStack.Count; i++)
            {
                _popupStack[i].transform.SetAsLastSibling();
            }
        }

        private void HandleMaskClicked()
        {
            if (_popupStack.Count == 0)
            {
                return;
            }

            var topPopup = _popupStack[_popupStack.Count - 1];
            if (topPopup.CloseOnMaskClick)
            {
                Close(topPopup);
            }
        }

        private void EnsureHierarchy()
        {
            if (uiCanvas == null)
            {
                uiCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (uiCanvas == null)
            {
                var canvasObject = new GameObject(
                    "UICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                uiCanvas = canvasObject.GetComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            fullscreenRoot = EnsureRoot(fullscreenRoot, "FullscreenRoot", uiCanvas.transform);
            popupRoot = EnsureRoot(popupRoot, "PopupRoot", uiCanvas.transform);
            fullscreenRoot.SetAsFirstSibling();
            popupRoot.SetAsLastSibling();

            if (popupMask == null)
            {
                var maskObject = new GameObject("PopupMask", typeof(RectTransform), typeof(Image), typeof(Button));
                maskObject.transform.SetParent(popupRoot, false);
                Stretch(maskObject.GetComponent<RectTransform>());
                popupMask = maskObject.GetComponent<Image>();
            }
            else
            {
                popupMask.transform.SetParent(popupRoot, false);
                Stretch(popupMask.rectTransform);
            }

            popupMask.color = popupMaskColor;
            popupMask.raycastTarget = true;
            _popupMaskButton = popupMask.GetComponent<Button>();
            if (_popupMaskButton == null)
            {
                _popupMaskButton = popupMask.gameObject.AddComponent<Button>();
            }

            _popupMaskButton.transition = Selectable.Transition.None;
            _popupMaskButton.targetGraphic = popupMask;
            var maskNavigation = _popupMaskButton.navigation;
            maskNavigation.mode = Navigation.Mode.None;
            _popupMaskButton.navigation = maskNavigation;
            _popupMaskButton.onClick.RemoveListener(HandleMaskClicked);
            _popupMaskButton.onClick.AddListener(HandleMaskClicked);
            popupMask.gameObject.SetActive(false);
        }

        private static RectTransform EnsureRoot(RectTransform current, string rootName, Transform parent)
        {
            if (current == null)
            {
                var existing = parent.Find(rootName) as RectTransform;
                if (existing != null)
                {
                    current = existing;
                }
                else
                {
                    var rootObject = new GameObject(rootName, typeof(RectTransform));
                    rootObject.transform.SetParent(parent, false);
                    current = rootObject.GetComponent<RectTransform>();
                }
            }

            Stretch(current);
            return current;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
}
