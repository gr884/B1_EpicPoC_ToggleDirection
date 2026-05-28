using TMPro;
using UnityEngine;

public class UI_PlaceCountView : MonoBehaviour
{
    [SerializeField] private TMP_Text _statusText;

    private void Start()
    {
        BattleManager.Instance.OnTurnStateChanged += OnTurnStateChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnTurnStateChanged -= OnTurnStateChanged;
    }

    private void OnTurnStateChanged(BattleManager.TurnState _) => Refresh();

    private void Refresh()
    {
        if (_statusText == null) return;

        _statusText.text = BattleManager.Instance.CurrentTurn == BattleManager.TurnState.EnemyTurn
            ? "ENEMY"
            : "PLACE";
    }
}
