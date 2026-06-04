using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private BattleActorView _view;
    [SerializeField] private BattleActorMotionTarget _motionTarget;

    [SerializeField] private int _maxHp = 30;
    [SerializeField] private int _handSize = 5;
    [SerializeField] private int _maxHandSize = 12;
    [SerializeField] private int _maxCost = 5;

    public int HandSize => _handSize;
    public int MaxHandSize => _maxHandSize;
    public int MaxHp => _maxHp;
    public int DefaultHandSize { get; private set; }

    public void SetHandSize(int size)
    {
        _handSize = Mathf.Max(1, size);
    }
    public int CurrentHp => _view != null ? _view.CurrentHp : 0;
    public bool IsDead => _view != null && _view.IsDead;
    public BattleActorMotionTarget MotionTarget => _motionTarget;

    public int MaxCost => _maxCost;
    public int CurrentCost { get; private set; }
    public int CurrentDefense { get; private set; }
    public int CurrentPendingAttack { get; private set; }

    public event Action OnDied;
    public event Action OnCostChanged;
    public event Action OnDefenseChanged;
    public event Action OnPendingAttackChanged;

    private void Awake()
    {
        DefaultHandSize = _handSize;
    }

    // ── 초기화 ─────────────────────────────────────────────

    public void Setup(int currentHp)
    {
        _view.OnDied -= HandleDied;
        _view.OnDied += HandleDied;
        _view.Setup("Player", _maxHp, currentHp);

        CurrentDefense = 0;
        _view.SetDefense(0);
        CurrentPendingAttack = 0;
        CurrentCost = _maxCost;
        OnDefenseChanged?.Invoke();
        OnPendingAttackChanged?.Invoke();
        OnCostChanged?.Invoke();
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

    // ── Defense ────────────────────────────────────────────

    public void AddDefense(int amount)
    {
        CurrentDefense += Mathf.Max(0, amount);
        _view.SetDefense(CurrentDefense);
        OnDefenseChanged?.Invoke();
    }

    public void ResetDefense()
    {
        CurrentDefense = 0;
        _view.SetDefense(0);
        OnDefenseChanged?.Invoke();
    }

    // ── Pending Attack ────────────────────────────────────

    public void AddPendingAttack(int amount)
    {
        int attack = Mathf.Max(0, amount);
        if (attack <= 0) return;

        CurrentPendingAttack += attack;
        OnPendingAttackChanged?.Invoke();
    }

    public int ConsumePendingAttack()
    {
        int attack = CurrentPendingAttack;
        CurrentPendingAttack = 0;
        OnPendingAttackChanged?.Invoke();
        return attack;
    }

    public void ResetPendingAttack()
    {
        if (CurrentPendingAttack == 0) return;

        CurrentPendingAttack = 0;
        OnPendingAttackChanged?.Invoke();
    }

    // ── 전투 로직 ──────────────────────────────────────────

    public void TakeAttack(int damage)
    {
        int blocked = Mathf.Min(CurrentDefense, damage);
        CurrentDefense -= blocked;
        int remaining = damage - blocked;
        _view.SetDefense(CurrentDefense);
        _view.TakeDamage(Mathf.Max(0, remaining));
        OnDefenseChanged?.Invoke();
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
        OnDefenseChanged = null;
        OnPendingAttackChanged = null;
    }
}
