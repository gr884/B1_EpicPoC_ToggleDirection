using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private BattleActorView _view;

    [SerializeField] private int _maxHp = 30;
    [SerializeField] private int _handSize = 5;
    [SerializeField] private int _maxHandSize = 12;
    [SerializeField] private int _maxCost = 5;

    public int HandSize => _handSize;
    public int MaxHandSize => _maxHandSize;
    public int MaxHp => _maxHp;
    public int CurrentHp => _view != null ? _view.CurrentHp : 0;
    public bool IsDead => _view != null && _view.IsDead;

    public int MaxCost => _maxCost;
    public int CurrentCost { get; private set; }

    public event Action OnDied;
    public event Action OnCostChanged;

    // ── 초기화 ─────────────────────────────────────────────

    public void Setup(int currentHp)
    {
        _view.OnDied -= HandleDied;
        _view.OnDied += HandleDied;
        _view.Setup("Player", _maxHp, currentHp);
    }

    public void RestoreCost()
    {
        CurrentCost = _maxCost;
        OnCostChanged?.Invoke();
    }

    // ── Cost ───────────────────────────────────────────────

    public bool CanSpend(int amount) => CurrentCost >= amount;

    public bool SpendCost(int amount)
    {
        if (!CanSpend(amount)) return false;
        CurrentCost -= amount;
        OnCostChanged?.Invoke();
        return true;
    }

    public void GainCost(int amount)
    {
        CurrentCost += Mathf.Max(0, amount);
        OnCostChanged?.Invoke();
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
        OnCostChanged = null;
    }
}