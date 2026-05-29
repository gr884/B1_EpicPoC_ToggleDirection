using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private BattleActorView _view;

    private EnemyDataSO _data;
    private int _intentIndex;

    public bool IsDead => _view != null && _view.IsDead;
    public int CurrentDefense { get; private set; }
    public EnemyIntentTurn CurrentIntentTurn { get; private set; }

    public event Action OnDied;
    public event Action<EnemyIntentTurn> OnIntentChanged;

    // ── 초기화 ─────────────────────────────────────────────

    public void Setup(EnemyDataSO data)
    {
        _data = data;
        _intentIndex = 0;
        CurrentDefense = 0;

        _view.OnDied -= HandleDied;
        _view.OnDied += HandleDied;
        _view.Setup(data.displayName, data.maxHp);

        // 첫 플레이어 턴에 표시할 Intent 로드 (실행 아님)
        RefreshIntent();
    }

    // ── 전투 로직 ──────────────────────────────────────────

    public void TakeAttack(int damage)
    {
        int blocked = Mathf.Min(CurrentDefense, damage);
        CurrentDefense -= blocked;
        int remaining = damage - blocked;
        _view.SetDefense(CurrentDefense);
        _view.TakeDamage(Mathf.Max(0, remaining));
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

    /// <summary>
    /// 적 턴에 현재 Intent를 실행합니다.
    /// Attack은 BattleManager에서 GetIntentValue로 처리.
    /// Defend는 여기서 Defense로 쌓음.
    /// </summary>
    public void ExecuteIntents()
    {
        if (CurrentIntentTurn == null) return;

        // 적 턴 시작 시 이전 Defense 리셋 후 새로 쌓음
        CurrentDefense = 0;
        foreach (EnemyIntentData intent in CurrentIntentTurn.intents)
        {
            if (intent.type == EnemyIntentType.Defend)
                CurrentDefense += intent.value;
        }

        _view.SetDefense(CurrentDefense);
        Debug.Log($"[Enemy] Intent 실행 완료 — Defense {CurrentDefense}");
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
    }
}