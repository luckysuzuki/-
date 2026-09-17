using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WitchTrial.Story;

namespace WitchTrial.UI.Panels
{
    /// <summary>
    /// 显示单个存档的时间和缩略图，并转发读取、保存或删除操作。
    /// </summary>
    public class SaveAndLoadSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text timestampText;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Image saveimage;
        [SerializeField] private Sprite DefaultImage;
        private Action clicked, deleted;
        private Texture2D thumbnail;
        private Sprite thumbnailSprite;
        private void Awake() { deleteButton?.onClick.AddListener(Delete); }
        public void Bind(int index, StorySaveRecord record, bool exists, string error, Action click, Action delete)
        {
            clicked=click; deleted=delete; ReleaseThumbnail();
            if(timestampText!=null)timestampText.text=error!=null ? "存档损坏 · 可删除" : record==null ? "空存档" :
                DateTimeOffset.Parse(record.savedAtUtc).ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
            if(deleteButton!=null)deleteButton.gameObject.SetActive(exists);
            if(saveimage!=null)saveimage.sprite=DefaultImage;
            if(record==null || string.IsNullOrEmpty(record.screenshotBase64) || saveimage==null)return;
            try {
                thumbnail=new Texture2D(2,2,TextureFormat.RGB24,false);
                if(!thumbnail.LoadImage(Convert.FromBase64String(record.screenshotBase64))) { ReleaseThumbnail(); return; }
                thumbnailSprite=Sprite.Create(thumbnail,new Rect(0,0,thumbnail.width,thumbnail.height),new Vector2(.5f,.5f));
                saveimage.sprite=thumbnailSprite;
            } catch { ReleaseThumbnail(); }
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.button!=PointerEventData.InputButton.Left)return;
            var hit=eventData.pointerCurrentRaycast.gameObject;
            if(hit!=null && deleteButton!=null && hit.transform.IsChildOf(deleteButton.transform))return;
            clicked?.Invoke();
        }
        private void Delete() { deleted?.Invoke(); }
        private void ReleaseThumbnail()
        {
            if(saveimage!=null)saveimage.sprite=DefaultImage;
            if(thumbnailSprite!=null)Destroy(thumbnailSprite);
            if(thumbnail!=null)Destroy(thumbnail);
            thumbnailSprite=null; thumbnail=null;
        }
        private void OnDestroy() { deleteButton?.onClick.RemoveListener(Delete); ReleaseThumbnail(); }
    }
}
