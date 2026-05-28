using UnityEngine;
using UnityEngine.UI;

public class UI_NextButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _button;

    private void Awake()
    {
        _button.onClick.AddListener(OnClick);
    }

    private void Start()
    {
        BattleManager.Instance.OnBattleEnded += OnBattleEnded;
        GameManager.Instance.OnStateChanged += OnStateChanged;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnBattleEnded -= OnBattleEnded;
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnBattleEnded()
    {
        SetVisible(BattleManager.Instance.IsWaitingForNextBattle);
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Playing)
            SetVisible(false);
    }

    private void OnClick()
    {
        SetVisible(false);
        GameFlowManager.Instance.ProceedToNextBattle();
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
