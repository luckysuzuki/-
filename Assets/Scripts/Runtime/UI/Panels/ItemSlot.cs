using System;
using WitchTrial.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static event Action onItemSlotClicked;
    public WitchBookEntry Entry { get; private set; }
    [SerializeField] private float hoverScale = 1.06f;
    private Action<WitchBookEntry> clicked;
    private Vector3 restScale;
    private bool hovered;
    private void Awake() { restScale = transform.localScale; }
    public void Bind(WitchBookEntry entry, Action<WitchBookEntry> callback) { Entry = entry; clicked = callback; }
    private void Update() { transform.localScale = Vector3.Lerp(transform.localScale, restScale * (hovered ? hoverScale : 1f), 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime)); }
    private void OnDisable() { hovered = false; transform.localScale = restScale; }
    public void OnPointerEnter(PointerEventData e) { hovered = Entry != null; }
    public void OnPointerExit(PointerEventData e) { hovered = false; }
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || Entry == null) return;
        clicked?.Invoke(Entry);
        onItemSlotClicked?.Invoke();
    }
}
