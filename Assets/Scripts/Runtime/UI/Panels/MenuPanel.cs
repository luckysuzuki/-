using UnityEngine;
using UnityEngine.UI;
using WitchTrial.UI;
using WitchTrial.UI.Panels;

/// <summary>
/// 提供游戏内保存、读取、设置、返回标题和关闭菜单的入口。
/// </summary>
public class MenuPanel : UIPanel
{
    public override UILayer Layer => UILayer.Popup;
    public override bool CloseOnMaskClick => true;
    [SerializeField] private Button save, load, exit2Title, log, option, close;
    private void OnEnable()
    {
        save?.onClick.AddListener(OpenSavePanel); load?.onClick.AddListener(OpenLoadPanel);
        exit2Title?.onClick.AddListener(BacktoTitle); option?.onClick.AddListener(OpenOptionPanel);
        close?.onClick.AddListener(CloseCurrent);
    }
    private void OnDisable()
    {
        save?.onClick.RemoveListener(OpenSavePanel); load?.onClick.RemoveListener(OpenLoadPanel);
        exit2Title?.onClick.RemoveListener(BacktoTitle); option?.onClick.RemoveListener(OpenOptionPanel);
        close?.onClick.RemoveListener(CloseCurrent);
    }
    public void OpenSavePanel() { UIRouter.Open<SaveAndLoadPanel>(SaveAndLoadMode.Save); }
    public void OpenLoadPanel() { UIRouter.Open<SaveAndLoadPanel>(SaveAndLoadMode.Load); }
    private void OpenOptionPanel() { UIRouter.Open<OptionsPanel>(); }
    private void BacktoTitle()
    {
        UIRouter.Open<ConfirmPopUp>(new ConfirmContext { Message="即将返回标题页面。", ConfirmText="确认",
            OnConfirmed=()=>UIRouter.Replace<MainPanel>() });
    }
    private void CloseCurrent() { RequestClose(); }
}
