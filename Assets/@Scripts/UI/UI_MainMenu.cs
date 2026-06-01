using UnityEngine;
using UnityEngine.UI;

public class UI_MainMenu : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _tutorialButton;

    private void Start()
    {
        _startButton.onClick.AddListener(OnStartClicked);
        _tutorialButton.onClick.AddListener(OnTutorialClicked);
        GameManager.Instance.OnStateChanged += OnStateChanged;
        SetVisible(GameManager.Instance.CurrentState == GameManager.GameState.Idle);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        SetVisible(state == GameManager.GameState.Idle);
    }

    private void OnStartClicked()
    {
        GameManager.Instance.GameStart();
    }

    private void OnTutorialClicked()
    {
        // TODO: 튜토리얼 구현
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}