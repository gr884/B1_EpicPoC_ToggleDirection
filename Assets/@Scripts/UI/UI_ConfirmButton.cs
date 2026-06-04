using UnityEngine;
using UnityEngine.UI;

public class UI_ConfirmButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _button;

    private void Awake()
    {
        _button.onClick.AddListener(OnClick);
    }

    private void Start()
    {
        BattleManager.Instance.OnPhaseChanged += OnPhaseChanged;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(BattleManager.BattlePhase phase)
    {
        SetVisible(phase == BattleManager.BattlePhase.PlayerTurn);
    }

    private void OnClick()
    {
        BattleManager.Instance.ConfirmPlayerTurn();
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}