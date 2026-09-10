using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WitchTrial.UI;

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
        var data = context as ConfirmContext;

        messageText.text = data.Message;
        confirmButtonText.text = data.ConfirmText;
        _onConfirmed = data.OnConfirmed;
    }

    private void OnEnable()
    {
        cancelButton.onClick.AddListener(Cancel);
        confirmButton.onClick.AddListener(Confirm);
    }

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

public sealed class ConfirmContext
{
    public string Message;
    public string ConfirmText;
    public System.Action OnConfirmed;
}