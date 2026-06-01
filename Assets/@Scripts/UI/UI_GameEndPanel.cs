using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameEndPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Button _restartButton;

    [Header("Settings")]
    [SerializeField] private string _clearText = "Victory!";
    [SerializeField] private string _overText = "Game Over";
    [SerializeField] private Color _clearColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _overColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private void Start()
    {
        GameManager.Instance.OnStateChanged += OnStateChanged;
        _restartButton.onClick.AddListener(OnRestart);
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.GameClear:
                Show(true);
                break;
            case GameManager.GameState.GameOver:
                Show(false);
                break;
            default:
                SetVisible(false);
                break;
        }
    }

    private void Show(bool victory)
    {
        _titleText.text = victory ? _clearText : _overText;
        _titleText.color = victory ? _clearColor : _overColor;
        SetVisible(true);
    }

    private void OnRestart()
    {
        SetVisible(false);
        GameManager.Instance.GoToMainMenu();
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}