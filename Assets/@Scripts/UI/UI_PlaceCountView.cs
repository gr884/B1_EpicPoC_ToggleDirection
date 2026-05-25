using TMPro;
using UnityEngine;

public class UI_PlaceCountView : MonoBehaviour
{
    [SerializeField] private TMP_Text _placeCountText;

    private void Start()
    {
        BattleManager.Instance.OnPhaseChanged += OnPhaseChanged;
        CardManager.Instance.OnHandChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        if (CardManager.Instance != null)
            CardManager.Instance.OnHandChanged -= Refresh;
    }

    private void OnPhaseChanged(BattleManager.BattlePhase phase)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_placeCountText == null) return;

        if (BattleManager.Instance.CurrentPhase == BattleManager.BattlePhase.FreePlace)
        {
            _placeCountText.text = "무제한";
            return;
        }

        int remaining = BattleManager.Instance.MaxCardsPerTurn - CardManager.Instance.CardsPlacedThisTurn;
        _placeCountText.text = Mathf.Max(0, remaining).ToString();
    }
}