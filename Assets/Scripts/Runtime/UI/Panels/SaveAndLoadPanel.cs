using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchTrial.UI;
using WitchTrial.UI.Panels;
using WitchTrial.Story;

/// <summary>
/// 指定共用存读档面板当前执行读取还是保存操作。
/// </summary>
public enum SaveAndLoadMode { Load, Save }

/// <summary>
/// 管理十页存档槽、截图保存、进度读取、覆盖与删除确认。
/// </summary>
public class SaveAndLoadPanel : UIPanel
{
    public override UILayer Layer => UILayer.Popup;
    public override bool CloseOnMaskClick => false;
    [SerializeField] private Sprite TitleSprite_Load;
    [SerializeField] private Sprite TitleSprite_Save;
    [SerializeField] private Image TitleImage;
    [SerializeField] private TMP_Text currentPageText;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button lastPageButton;
    [SerializeField] private SaveAndLoadSlot[] slots;
    [SerializeField] private Button backButton;
    private StorySaveStore store;
    private SaveContentCatalog catalog;
    private SaveAndLoadMode mode;
    private bool busy;
    public int CurrentPage { get; private set; }
    public bool Busy => busy;
    public SaveAndLoadMode Mode => mode;
    private void OnEnable()
    {
        nextPageButton?.onClick.AddListener(NextPage);
        lastPageButton?.onClick.AddListener(PreviousPage);
        backButton?.onClick.AddListener(ClosePanel);
    }
    private void OnDisable()
    {
        nextPageButton?.onClick.RemoveListener(NextPage);
        lastPageButton?.onClick.RemoveListener(PreviousPage);
        backButton?.onClick.RemoveListener(ClosePanel);
    }
    protected override void OnOpening(object context)
    {
        store=new StorySaveStore(); catalog=SaveContentCatalog.Load();
        mode=context is SaveAndLoadMode value ? value : context is bool saving && saving ? SaveAndLoadMode.Save : SaveAndLoadMode.Load;
        CurrentPage=0;
        if(TitleImage!=null)TitleImage.sprite=mode==SaveAndLoadMode.Save?TitleSprite_Save:TitleSprite_Load;
        RefreshPage();
    }
    protected override void OnRefreshed(object context) { if(!busy)OnOpening(context); }
    public void NextPage() { if(!busy && CurrentPage<StorySaveStore.MaxPages-1){CurrentPage++;RefreshPage();} }
    public void PreviousPage() { if(!busy && CurrentPage>0){CurrentPage--;RefreshPage();} }
    private void ClosePanel() { if(!busy)RequestClose(); }
    public void RefreshPage()
    {
        if(currentPageText!=null)currentPageText.text=(CurrentPage+1)+" / "+StorySaveStore.MaxPages;
        if(nextPageButton!=null)nextPageButton.interactable=!busy && CurrentPage<StorySaveStore.MaxPages-1;
        if(lastPageButton!=null)lastPageButton.interactable=!busy && CurrentPage>0;
        if(backButton!=null)backButton.interactable=!busy;
        for(int i=0;slots!=null && i<slots.Length;i++) {
            int index=CurrentPage*StorySaveStore.SlotsPerPage+i;
            StorySaveRecord record=null; string error=null; bool exists=false;
            try { exists=store.Exists(index); record=store.Read(index); } catch(Exception e) {error=e.Message;}
            slots[i]?.Bind(index,record,exists,error,()=>SelectSlot(index),()=>DeleteSlot(index));
        }
    }
    private bool CanInteract => IsVisible && !busy && !UIRouter.IsOpen<ConfirmPopUp>();
    public void SelectSlot(int index)
    {
        if(!CanInteract)return;
        try {
            if(mode==SaveAndLoadMode.Load) {
                var record=store.Read(index); if(record==null)return;
                var resolved=record.progress.Resolve(catalog);
                UIRouter.Replace<GamePanel>(resolved);
            } else {
                var game=UIRouter.Get<GamePanel>();
                if(game==null || !game.IsVisible || game.runner==null || !game.runner.CanSave) {
                    ShowMessage("当前没有可以保存的剧情，请等待转场结束。"); return;
                }
                if(store.Exists(index)) UIRouter.Open<ConfirmPopUp>(new ConfirmContext {
                    Message="要覆盖这个存档吗？", ConfirmText="覆盖", OnConfirmed=()=>BeginSave(index) });
                else BeginSave(index);
            }
        } catch(Exception e) { ShowMessage("读取失败："+e.Message); }
    }
    private void BeginSave(int index)
    {
        if(busy || !IsOpen)return;
        try {
            var game=UIRouter.Get<GamePanel>();
            var data=StorySaveData.Capture(game?.runner,catalog);
            var node=game.runner.CurrentNode as DialogueNode;
            StartCoroutine(Save(index,data,node!=null?node.title:game.runner.CurrentNode.name));
        } catch(Exception e){ShowMessage("保存失败："+e.Message);}
    }
    private IEnumerator Save(int index, StorySaveData data, string title)
    {
        busy=true; RefreshPage();
        var popup=transform.parent;
        var group=popup.GetComponent<CanvasGroup>();
        bool added=group==null; if(added)group=popup.gameObject.AddComponent<CanvasGroup>();
        float alpha=group.alpha; bool interactable=group.interactable;
        Texture2D screenshot=null;
        string failure=null;
        try {
            // Hide the entire popup layer, including menu and mask, while keeping story input blocked.
            group.alpha=0; group.interactable=false;
            yield return new WaitForEndOfFrame();
            try {
                screenshot=ScreenCapture.CaptureScreenshotAsTexture();
                store.Write(index,data,EncodeThumbnail(screenshot),title);
            } catch(Exception e) { failure=e.Message; }
        } finally {
            if(screenshot!=null)Destroy(screenshot);
            if(group!=null){group.alpha=alpha;group.interactable=interactable;if(added)Destroy(group);}
            busy=false;
        }
        RefreshPage();
        if(failure!=null)ShowMessage("保存失败："+failure);
    }
    public static byte[] EncodeThumbnail(Texture2D screenshot)
    {
        if(screenshot==null)throw new InvalidOperationException("未能捕获游戏画面。");
        int width=Mathf.Min(384,screenshot.width);
        int height=Mathf.Max(1,Mathf.RoundToInt((float)width*screenshot.height/screenshot.width));
        var source=screenshot.GetPixels32();
        var pixels=new Color32[width*height];
        // ScreenCapture already contains display-encoded pixels. Average their raw bytes;
        // Graphics.Blit into an sRGB target can encode them a second time in Linear projects.
        for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
            int x0=x*screenshot.width/width, x1=(x+1)*screenshot.width/width;
            int y0=y*screenshot.height/height, y1=(y+1)*screenshot.height/height;
            long r=0,g=0,b=0; int count=(x1-x0)*(y1-y0);
            for(int sy=y0;sy<y1;sy++)for(int sx=x0;sx<x1;sx++) {
                var c=source[sy*screenshot.width+sx];r+=c.r;g+=c.g;b+=c.b;
            }
            pixels[y*width+x]=new Color32((byte)((r+count/2)/count),
                (byte)((g+count/2)/count),(byte)((b+count/2)/count),255);
        }
        var small=new Texture2D(width,height,TextureFormat.RGB24,false);
        try {small.SetPixels32(pixels);small.Apply();return small.EncodeToPNG();}
        finally {if(Application.isPlaying)Destroy(small);else DestroyImmediate(small);}
    }
    public void DeleteSlot(int index)
    {
        if(!CanInteract)return;
        try {
            if(!store.Exists(index))return;
            UIRouter.Open<ConfirmPopUp>(new ConfirmContext {
                Message="确定删除这个存档吗？删除后无法恢复。", ConfirmText="删除",
                OnConfirmed=()=>{try{store.Delete(index);RefreshPage();}catch(Exception e){ShowMessage("删除失败："+e.Message);}}
            });
        } catch(Exception e){ShowMessage(e.Message);}
    }
    private void ShowMessage(string message) { UIRouter.Open<ConfirmPopUp>(new ConfirmContext { Message=message,ConfirmText="确定" }); }
}
