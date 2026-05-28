using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private BattleActorView _view;
    [SerializeField, Min(0)] private int _currentBlock = 0;

    private EnemyDataSO _data;
    private int _intentIndex;

    public bool IsDead => _view != null && _view.IsDead;
    public int CurrentBlock => _currentBlock;
    public EnemyIntentTurn CurrentIntentTurn { get; private set; }

    public event Action OnDied;
    public event Action<EnemyIntentTurn> OnIntentChanged;
    public event Action<int> OnBlockChanged;
    public event Action<int, int> OnBlockConsumed;

    // ── 초기화 ─────────────────────────────────────────────

    public void Setup(EnemyDataSO data)
    {
        _data = data;
        _intentIndex = 0;
        _currentBlock = 0;

        _view.OnDied -= HandleDied;
        _view.OnDied += HandleDied;
        _view.Setup(data.displayName, data.maxHp);
        OnBlockChanged?.Invoke(_currentBlock);

        RefreshIntent();
    }

    // ── 전투 로직 ──────────────────────────────────────────

    public void TakeAttack(int playerDamage)
    {
        int damage = Mathf.Max(0, playerDamage);
        int usedBlock = Mathf.Min(_currentBlock, damage);
        _currentBlock -= usedBlock;

        int hpDamage = damage - usedBlock;
        OnBlockConsumed?.Invoke(usedBlock, _currentBlock);
        OnBlockChanged?.Invoke(_currentBlock);

        if (hpDamage > 0)
            _view.TakeDamage(hpDamage);
    }

    public void ClearBlock()
    {
        if (_currentBlock == 0) return;
        _currentBlock = 0;
        OnBlockChanged?.Invoke(_currentBlock);
    }

    public void GainBlock(int amount)
    {
        int value = Mathf.Max(0, amount);
        if (value == 0) return;

        _currentBlock += value;
        OnBlockChanged?.Invoke(_currentBlock);
    }

    public int GetIntentValue(EnemyIntentType type)
    {
        if (CurrentIntentTurn == null) return 0;

        int total = 0;
        foreach (EnemyIntentData intent in CurrentIntentTurn.intents)
            if (intent.type == type)
                total += intent.value;
        return total;
    }

    public void AdvanceIntent()
    {
        if (_data?.intentPattern == null || _data.intentPattern.Count == 0) return;
        _intentIndex = (_intentIndex + 1) % _data.intentPattern.Count;
        RefreshIntent();
    }

    // ── 내부 ───────────────────────────────────────────────

    private void RefreshIntent()
    {
        if (_data?.intentPattern == null || _data.intentPattern.Count == 0)
        {
            CurrentIntentTurn = null;
            OnIntentChanged?.Invoke(null);
            return;
        }

        CurrentIntentTurn = _data.intentPattern[_intentIndex];
        OnIntentChanged?.Invoke(CurrentIntentTurn);
    }

    private void HandleDied() => OnDied?.Invoke();

    private void OnDestroy()
    {
        if (_view != null)
            _view.OnDied -= HandleDied;

        OnDied = null;
        OnIntentChanged = null;
        OnBlockChanged = null;
        OnBlockConsumed = null;
    }
}
