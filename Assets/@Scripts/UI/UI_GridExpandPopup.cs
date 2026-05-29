using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GridExpandPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Button _expandButton;
    [SerializeField] private Button _keepButton;

    private void Start()
    {
        _expandButton.onClick.AddListener(HandleExpand);
        _keepButton.onClick.AddListener(HandleKeep);
        BattleManager.Instance.OnBattleEnded += Show;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnBattleEnded -= Show;
    }

    private void Show()
    {
        int nextRows = Mathf.Min(GridManager.Instance.Rows + 1, GridManager.Instance.MaxRows);
        int nextCols = Mathf.Min(GridManager.Instance.Columns + 1, GridManager.Instance.MaxColumns);
        bool canExpand = GridManager.Instance.CanExpand;

        if (_descriptionText != null)
        {
            _descriptionText.text = canExpand
                ? $"그리드를 {GridManager.Instance.Rows}x{GridManager.Instance.Columns} → {nextRows}x{nextCols}로 확장할까요?"
                : "그리드가 최대 크기입니다.";
        }

        _expandButton.interactable = canExpand;
        SetVisible(true);
    }

    private void HandleExpand()
    {
        SetVisible(false);
        GameFlowManager.Instance.OnExpandChosen();
    }

    private void HandleKeep()
    {
        SetVisible(false);
        GameFlowManager.Instance.OnKeepChosen();
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}