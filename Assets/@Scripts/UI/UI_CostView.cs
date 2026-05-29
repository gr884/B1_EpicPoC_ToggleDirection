using TMPro;
using UnityEngine;

public class UI_CostView : MonoBehaviour
{
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private Player _player;

    private void Start()
    {
        _player.OnCostChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.OnCostChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_costText != null)
            _costText.text = $"{_player.CurrentCost} / {_player.MaxCost}";
    }
}