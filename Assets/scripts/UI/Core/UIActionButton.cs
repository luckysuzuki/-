using UnityEngine;
using UnityEngine.UI;

namespace WitchTrial.UI
{
    public enum UIAction
    {
        Open,
        Replace,
        CloseCurrent,
        Back
    }

    /// <summary>
    /// Inspector-friendly button adapter. It lets a prefab navigate without a
    /// hard reference to another panel instance or to UIManager.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIActionButton : MonoBehaviour
    {
        [SerializeField] private UIAction action = UIAction.Open;
        [Tooltip("A prefab/component token. UIRegistry still owns the runtime instance.")]
        [SerializeField] private UIPanel destination;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            _button.onClick.AddListener(Execute);
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(Execute);
            }
        }

        public void Execute()
        {
            switch (action)
            {
                case UIAction.Open:
                    RequireDestination();
                    UIRouter.Open(destination.GetType());
                    break;

                case UIAction.Replace:
                    RequireDestination();
                    UIRouter.Replace(destination.GetType());
                    break;

                case UIAction.CloseCurrent:
                    var owner = GetComponentInParent<UIPanel>();
                    if (owner != null)
                    {
                        UIRouter.Close(owner);
                    }
                    break;

                case UIAction.Back:
                    UIRouter.Back();
                    break;
            }
        }

        private void RequireDestination()
        {
            if (destination == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(UIActionButton)} on {name} needs a destination panel prefab.");
            }
        }
    }
}
