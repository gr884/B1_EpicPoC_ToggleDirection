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
        // FreePlace, Turn 모두 확정 버튼 표시
        SetVisible(true);
    }

    private void OnClick()
    {
        switch (BattleManager.Instance.CurrentPhase)
        {
            case BattleManager.BattlePhase.FreePlace:
                BattleManager.Instance.ConfirmFreePlace();
                break;
            case BattleManager.BattlePhase.Turn:
                BattleManager.Instance.ConfirmTurn();
                break;
        }
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}