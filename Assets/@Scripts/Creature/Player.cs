using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private BattleActorView _view;

    [SerializeField] private int _maxHp = 30;
    [SerializeField] private int _handSize = 5;

    public int HandSize => _handSize;
    public int MaxHp => _maxHp;
    public int CurrentHp => _view != null ? _view.CurrentHp : 0;
    public bool IsDead => _view != null && _view.IsDead;

    public event Action OnDied;

    // ── 초기화 ─────────────────────────────────────────────

    public void Setup(int currentHp)
    {

        _view.OnDied -= HandleDied;
        _view.OnDied += HandleDied;
        _view.Setup("Player", _maxHp, currentHp);
    }

    // ── 전투 로직 ──────────────────────────────────────────

    public void TakeAttack(int damage)
    {
        _view.TakeDamage(Mathf.Max(0, damage));
    }

    public void Heal(int amount)
    {
        _view.Heal(amount);
    }

    // ── 내부 ───────────────────────────────────────────────

    private void HandleDied() => OnDied?.Invoke();

    private void OnDestroy()
    {
        if (_view != null)
            _view.OnDied -= HandleDied;

        OnDied = null;
    }
}