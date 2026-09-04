using UnityEngine;

namespace WitchTrial.UI
{
    [DisallowMultipleComponent]
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] private UILayer layer = UILayer.Fullscreen;
        [SerializeField] private bool closeOnMaskClick = true;
        [SerializeField] private bool keepAlive = true;
        [SerializeField] private CanvasGroup canvasGroup;

        private bool _isOpen;
        private bool _isCovered;

        public virtual UILayer Layer => layer;
        public virtual bool CloseOnMaskClick => closeOnMaskClick;
        public bool KeepAlive => keepAlive;
        public bool IsOpen => _isOpen;
        public bool IsVisible => _isOpen && !_isCovered;

        public void RequestClose()
        {
            UIRouter.Close(this);
        }

        protected virtual void OnOpening(object context) { }
        protected virtual void OnOpened() { }
        protected virtual void OnRefreshed(object context) { }
        protected virtual void OnCovered() { }
        protected virtual void OnRevealed() { }
        protected virtual void OnClosing() { }
        protected virtual void OnClosed() { }

        internal void PrepareForManager()
        {
            EnsureCanvasGroup();
        }

        internal void HideImmediately()
        {
            EnsureCanvasGroup();
            _isOpen = false;
            _isCovered = false;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        internal void OpenInternal(object context)
        {
            EnsureCanvasGroup();
            _isOpen = true;
            _isCovered = false;
            OnOpening(context);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            OnOpened();
        }

        internal void RefreshInternal(object context)
        {
            OnRefreshed(context);
        }

        internal void CoverInternal()
        {
            if (!_isOpen || _isCovered)
            {
                return;
            }

            _isCovered = true;
            OnCovered();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        internal void RevealInternal()
        {
            if (!_isOpen || !_isCovered)
            {
                return;
            }

            _isCovered = false;
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            transform.SetAsLastSibling();
            OnRevealed();
        }

        internal void CloseInternal()
        {
            if (!_isOpen)
            {
                return;
            }

            OnClosing();
            _isOpen = false;
            _isCovered = false;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            OnClosed();
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }
}
