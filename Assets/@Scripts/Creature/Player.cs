using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private BattleActorView _view;

    [SerializeField] private int _maxHp = 30;
    [SerializeField] private int _handSize = 5;
    [SerializeField, Min(0)] private int _currentBlock = 0;

    public int HandSize => _handSize;
    public int MaxHp => _maxHp;
    public int CurrentHp => _view != null ? _view.CurrentHp : 0;
    public int CurrentBlock => _currentBlock;
    public bool IsDead => _view != null && _view.IsDead;

    public event Action OnDied;
    public event Action<int> OnBlockChanged;
    public event Action<int, int> OnBlockConsumed;

    // ── 초기화 ─────────────────────────────────────────────

    public void Setup(int currentHp)
    {

        _view.OnDied -= HandleDied;
        _view.OnDied += HandleDied;
        _view.Setup("Player", _maxHp, currentHp);
        _currentBlock = 0;
        OnBlockChanged?.Invoke(_currentBlock);
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

    public void GainBlock(int amount)
    {
        int value = Mathf.Max(0, amount);
        if (value == 0) return;

        _currentBlock += value;
        OnBlockChanged?.Invoke(_currentBlock);
    }

    public void ClearBlock()
    {
        if (_currentBlock == 0) return;
        _currentBlock = 0;
        OnBlockChanged?.Invoke(_currentBlock);
    }

    public void TakeAttackWithBlock(int incomingDamage)
    {
        int damage = Mathf.Max(0, incomingDamage);
        int usedBlock = Mathf.Min(_currentBlock, damage);
        _currentBlock -= usedBlock;

        int hpDamage = damage - usedBlock;

        OnBlockConsumed?.Invoke(usedBlock, _currentBlock);
        OnBlockChanged?.Invoke(_currentBlock);

        if (hpDamage > 0)
            _view.TakeDamage(hpDamage);
    }

    // ── 내부 ───────────────────────────────────────────────

    private void HandleDied() => OnDied?.Invoke();

    private void OnDestroy()
    {
        if (_view != null)
            _view.OnDied -= HandleDied;

        OnDied = null;
        OnBlockChanged = null;
        OnBlockConsumed = null;
    }
}
