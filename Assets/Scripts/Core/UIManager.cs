using UnityEngine;
using UnityEngine.UI;

public class UIManager : SingletonBehaviour<UIManager>
{
    [Header("Refs")]
    [SerializeField] private Button _undoButton;
    [SerializeField] private Button _restartButton;

    public void Init()
    {
        AutoBindRefs();
        BindButtons();

        GameManager.Instance.OnUndoStackChanged += RefreshUndoButton;
        GameManager.Instance.OnChainStateChanged += OnChainStateChanged;

        RefreshUndoButton();

        Debug.Log("[UIManager] Init");
    }

    private void RefreshUndoButton()
    {
        if (_undoButton != null)
            _undoButton.interactable = GameManager.Instance.CanUndo;
    }

    private void OnChainStateChanged(bool isResolving)
    {
        // 체인 진행 중엔 버튼 비활성화
        if (_restartButton != null) _restartButton.interactable = !isResolving;
        if (_undoButton != null && !isResolving) RefreshUndoButton();
        else if (_undoButton != null) _undoButton.interactable = false;
    }

    private void AutoBindRefs()
    {
        if (_undoButton == null)
        {
            GameObject obj = GameObject.Find("UndoButton");
            if (obj != null) _undoButton = obj.GetComponent<Button>();
        }

        if (_restartButton == null)
        {
            GameObject obj = GameObject.Find("RestartButton");
            if (obj != null) _restartButton = obj.GetComponent<Button>();
        }
    }

    private void BindButtons()
    {
        if (_undoButton != null)
        {
            _undoButton.onClick.RemoveAllListeners();
            _undoButton.onClick.AddListener(() => GameManager.Instance?.RequestUndo());
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(() => GameManager.Instance?.RequestRestart());
        }
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnUndoStackChanged -= RefreshUndoButton;
            GameManager.Instance.OnChainStateChanged -= OnChainStateChanged;
        }

        base.Dispose();
    }
}