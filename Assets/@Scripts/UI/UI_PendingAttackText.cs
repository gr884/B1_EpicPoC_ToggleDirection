using TMPro;
using UnityEngine;

public class UI_PendingAttackText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    private Player _player;

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        _player = BattleManager.Instance != null ? BattleManager.Instance.Player : null;
        if (_player != null)
            _player.OnPendingAttackChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.OnPendingAttackChanged -= Refresh;
    }

    private void Refresh()
    {
        int pendingAttack = _player != null ? _player.CurrentPendingAttack : 0;
        if (_text != null)
            _text.text = pendingAttack.ToString();

        SetChildrenVisible(pendingAttack > 0);
    }

    private void SetChildrenVisible(bool visible)
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(visible);
    }
}
