using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPointEnter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("要切换 Sprite 的图片。留空时使用当前 GameObject 上的 Image。")]
    [SerializeField] private Image targetImage;

    public Sprite EnterSprite;
    public bool should_hide = false;

    private Sprite defaultSprite;
    private Color hoverColor;
    private Color hiddenColor;
    private bool initialized;
    private bool eventsHandledBySelectableRelay;

    private void Awake()
    {
        Initialize();
        InstallSelectableRelayIfNeeded();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventsHandledBySelectableRelay || !Initialize())
        {
            return;
        }

        if (EnterSprite != null)
        {
            targetImage.sprite = EnterSprite;
        }

        if (should_hide)
        {
            targetImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventsHandledBySelectableRelay || !Initialize())
        {
            return;
        }

        targetImage.sprite = defaultSprite;
        if (should_hide)
        {
            targetImage.color = hiddenColor;
        }
    }

    private bool Initialize()
    {
        if (initialized && targetImage != null)
        {
            return true;
        }

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage == null)
        {
            return false;
        }

        defaultSprite = targetImage.sprite;
        hoverColor = targetImage.color;
        hoverColor.a = 1f;
        hiddenColor = targetImage.color;
        should_hide = should_hide || hiddenColor.a <= 0.001f;
        hiddenColor.a = 0f;
        initialized = true;
        return true;
    }

    private void InstallSelectableRelayIfNeeded()
    {
        if (!Initialize())
        {
            return;
        }

        var selectable = GetComponentInParent<Selectable>();
        if (selectable == null
            || selectable.gameObject == gameObject
            || selectable.targetGraphic != targetImage)
        {
            return;
        }

        var relay = selectable.GetComponent<UIPointEnter>();
        if (relay == null)
        {
            relay = selectable.gameObject.AddComponent<UIPointEnter>();
        }

        relay.ConfigureAsRelay(targetImage, EnterSprite, should_hide);
        eventsHandledBySelectableRelay = true;
    }

    private void ConfigureAsRelay(Image image, Sprite enterSprite, bool hideWhenIdle)
    {
        targetImage = image;
        EnterSprite = enterSprite;
        should_hide = hideWhenIdle;
        initialized = false;
        eventsHandledBySelectableRelay = false;
        Initialize();
    }
}
