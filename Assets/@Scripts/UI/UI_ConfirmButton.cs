using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ConfirmButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _labelText;

    private void Awake()
    {
        _button.onClick.AddListener(OnClick);
    }

    private void Start()
    {
        BattleManager.Instance.OnTurnStateChanged += OnTurnStateChanged;
        if (_labelText != null) _labelText.text = "END";
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnTurnStateChanged -= OnTurnStateChanged;
    }

    private void OnTurnStateChanged(BattleManager.TurnState state)
    {
        SetVisible(state == BattleManager.TurnState.PlayerTurn);
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
