using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_RelicPlacementConfirmPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private Action _onConfirm;
    private Action _onCancel;

    private void Awake()
    {
        _confirmButton?.onClick.AddListener(HandleConfirm);
        _cancelButton?.onClick.AddListener(HandleCancel);
        SetVisible(false);
    }

    public void Show(RelicData relic, Action onConfirm, Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (_titleText != null)
            _titleText.text = relic != null ? relic.displayName : "";
        if (_descriptionText != null)
            _descriptionText.text = "이 위치에 배치할까요?";

        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
        _onConfirm = null;
        _onCancel = null;
    }

    private void HandleConfirm()
    {
        _onConfirm?.Invoke();
    }

    private void HandleCancel()
    {
        _onCancel?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
