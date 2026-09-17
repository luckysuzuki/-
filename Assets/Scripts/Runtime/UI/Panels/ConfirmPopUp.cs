using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchTrial.UI;

/// <summary>
/// 显示确认消息，并在玩家确认后执行调用方提供的回调。
/// </summary>
public class ConfirmPopUp : UIPanel
{
    public override UILayer Layer => UILayer.Popup;

    // 破坏性操作最好不要点击遮罩就直接关闭
    public override bool CloseOnMaskClick => false;
    [Header("GameObjectsRef")]
  [SerializeField]  private TMP_Text confirmButtonText;
  [SerializeField]  private TMP_Text messageText;
  [SerializeField]  private Button cancelButton;
  [SerializeField]   private Button confirmButton;
   private Action _onConfirmed;
    protected override void OnOpening(object context)
    {
        var data = context as ConfirmContext ?? new ConfirmContext { Message = "请确认操作", ConfirmText = "确认" };

        messageText.text = data.Message;
        confirmButtonText.text = data.ConfirmText;
        _onConfirmed = data.OnConfirmed;
    }

    private void OnEnable()
    {
        cancelButton.onClick.AddListener(Cancel);
        confirmButton.onClick.AddListener(Confirm);
    }
    protected override void OnRefreshed(object context) { OnOpening(context); }

    private void OnDisable()
    {
        cancelButton.onClick.RemoveListener(Cancel);
        confirmButton.onClick.RemoveListener(Confirm);
    }
    private void Cancel()
    {
        RequestClose();
    }

    private void Confirm()
    {
        var callback = _onConfirmed;
        RequestClose();
        callback?.Invoke();
    }
    protected override void OnClosing()
    {
        _onConfirmed = null;
    }
}

/// <summary>
/// 携带确认弹窗要显示的消息、按钮文字和确认回调。
/// </summary>
public sealed class ConfirmContext
{
    public string Message;
    public string ConfirmText;
    public System.Action OnConfirmed;
}
