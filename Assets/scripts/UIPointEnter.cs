using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UIPointEnter : MonoBehaviour , IPointerEnterHandler,IPointerExitHandler
{
  private Sprite defautSprite;
  public Sprite EnterSprite;
 private Image image;
  void Start()
  {
      image = GetComponent<Image>();
      defautSprite = image.sprite;
  }
  public void OnPointerEnter(PointerEventData eventData)
  {
    image.sprite = EnterSprite;
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    image.sprite = defautSprite;
  }
}
