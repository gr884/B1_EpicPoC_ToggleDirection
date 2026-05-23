using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이그라운드 전용 UIManager.
/// Undo / Restart 버튼만 관리한다. 클리어/실패 패널 없음.
/// </summary>
public class Ryeol_UIManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button undoButton;
    [SerializeField] private Button restartButton;

    private Ryeol_GameManager gameManager;

    public void Initialize(Ryeol_GameManager owner)
    {
        gameManager = owner;
        AutoBindRefs();
        BindButtons();
        RefreshUndoButton();
    }

    public void RefreshUndoButton()
    {
        if (undoButton != null)
        {
            undoButton.interactable = gameManager != null && gameManager.CanUndo();
        }
    }

    private void AutoBindRefs()
    {
        if (undoButton == null)
        {
            GameObject obj = GameObject.Find("UndoButton");
            if (obj != null) undoButton = obj.GetComponent<Button>();
        }

        if (restartButton == null)
        {
            GameObject obj = GameObject.Find("RestartButton");
            if (obj != null) restartButton = obj.GetComponent<Button>();
        }
    }

    private void BindButtons()
    {
        if (undoButton != null)
        {
            undoButton.onClick.RemoveAllListeners();
            undoButton.onClick.AddListener(() =>
            {
                gameManager?.UndoLastMove();
                RefreshUndoButton();
            });
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                gameManager?.Restart();
                RefreshUndoButton();
            });
        }
    }
}
