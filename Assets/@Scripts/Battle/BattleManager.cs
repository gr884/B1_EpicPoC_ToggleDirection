using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : SingletonBehaviour<BattleManager>
{
    public enum BattlePhase { FreePlace, Turn }

    [Header("Actor Views")]
    [SerializeField] private BattleActorView _playerView;
    [SerializeField] private BattleActorView _enemyView;

    [Header("Enemy Card")]
    [SerializeField] private GameObject _cardPrefab;

    [Header("Debug")]
    [SerializeField] private bool _reduceAllOnTurn = false; // true: 놓인 카드 전부 / false: 활성화된 카드만

    // ── 전투 상태 ──────────────────────────────────────────
    public BattlePhase CurrentPhase { get; private set; }
    public bool IsChainRunning { get; private set; }

    // ── 이벤트 ────────────────────────────────────────────
    public event Action<BattlePhase> OnPhaseChanged;
    public event Action<bool> OnBattleEnded; // true = 승리

    public void Init()
    {
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        Debug.Log("[BattleManager] Init");
    }

    // ── 전투 시작 ──────────────────────────────────────────

    private EnemyDataSO _currentEnemyData;

    public void StartBattle(int playerHp, EnemyDataSO enemyData)
    {
        _currentEnemyData = enemyData;

        int enemyHp = enemyData != null ? enemyData.maxHp : 20;
        string enemyName = enemyData != null ? enemyData.displayName : "Enemy";

        _playerView.Setup("Player", playerHp);
        _enemyView.Setup(enemyName, enemyHp);

        _playerView.OnDied += () => EndBattle(false);
        _enemyView.OnDied += () => EndBattle(true);

        // 적 카드 배치
        if (enemyData != null && _cardPrefab != null)
            GridManager.Instance.PlaceEnemyCards(enemyData, _cardPrefab);

        EnterPhase(BattlePhase.FreePlace);
        CardManager.Instance.StartBattleDraw();

        Debug.Log($"[BattleManager] 전투 시작 — 플레이어 HP: {playerHp} / 적 HP: {enemyHp}");
    }

    // ── 자유 배치 확정 버튼 ────────────────────────────────

    public void ConfirmFreePlace()
    {
        if (CurrentPhase != BattlePhase.FreePlace || IsChainRunning) return;
        StartCoroutine(AttackRoutine());
    }

    // ── 체인 결과 처리 ─────────────────────────────────────

    private ChainResult _lastChainResult;

    private void OnChainFinished(ChainResult result)
    {
        _lastChainResult = result;

        if (CurrentPhase == BattlePhase.Turn)
            StartCoroutine(TurnRoutine());
        else if (CurrentPhase == BattlePhase.FreePlace)
            StartCoroutine(FreePlaceChainRoutine());
    }

    // FreePlace 중 카드 놓을 때마다 체인 후 처리
    private IEnumerator FreePlaceChainRoutine()
    {
        yield return null; // 내구도 제거 한 프레임 대기
    }

    // ── 자유 배치 확정 루틴 ────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        IsChainRunning = true;
        yield return StartCoroutine(ProcessCombat());
        IsChainRunning = false;
        if (_playerView.IsDead) yield break;
        EnterPhase(BattlePhase.Turn);
        CardManager.Instance.DrawToHand(1);
    }

    // ── 턴 루틴 ────────────────────────────────────────────

    private IEnumerator TurnRoutine()
    {
        IsChainRunning = true;
        yield return StartCoroutine(ProcessCombat());
        IsChainRunning = false;
        if (_playerView.IsDead) yield break;
        CardManager.Instance.DrawToHand(1);
    }

    // ── 공통 전투 처리 ─────────────────────────────────────

    private IEnumerator ProcessCombat()
    {
        int damage = Mathf.RoundToInt(_lastChainResult?.damage ?? 0);
        int defense = Mathf.RoundToInt(_lastChainResult?.defense ?? 0);
        int heal = Mathf.RoundToInt(_lastChainResult?.heal ?? 0);

        _enemyView.TakeDamage(damage);
        yield return new WaitForSeconds(0.5f);

        if (_enemyView.IsDead) yield break;

        if (heal > 0) _playerView.Heal(heal);

        int enemyRaw = CalculateEnemyDamage();
        int remaining = Mathf.Max(0, enemyRaw - defense);
        _playerView.TakeDamage(remaining);

        yield return new WaitForSeconds(0.5f);

        // 실제 적용 후 On된 카드 내구도 감소
        ReduceAllCardDurability();
    }

    // ── 내구도 처리 ────────────────────────────────────────

    public void ReduceAllCardDurability()
    {
        List<GridSlot> toRemove = new();

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.IsEmpty) continue;

            // _reduceAllOnTurn: 놓인 카드 전부 / false: 활성화된 카드만
            if (!_reduceAllOnTurn && !slot.OccupiedCard.IsActivated) continue;

            bool expired = slot.OccupiedCard.ReduceDurability();
            if (expired) toRemove.Add(slot);
        }

        foreach (GridSlot slot in toRemove)
        {
            CardView card = slot.OccupiedCard;
            slot.ClearCard();
            PoolManager.Instance.Return(card.gameObject);
            Debug.Log("[BattleManager] 내구도 소진으로 카드 제거");
        }
    }

    // ── 데미지 계산 ────────────────────────────────────────

    private int CalculateEnemyDamage()
    {
        int base_ = _currentEnemyData != null ? _currentEnemyData.baseDamage : 5;
        ChainResult enemyResult = new();

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            if (card == null || !card.IsEnemy || !card.IsActivated) continue;
            if (card.Data?.effects == null) continue;

            foreach (CardEffect effect in card.Data.effects)
            {
                if (effect.scope != CountScope.None) continue; // 일단 None만 처리
                switch (effect.effectType)
                {
                    case EffectType.Damage: enemyResult.damage += effect.value; break;
                    case EffectType.Defense: enemyResult.defense += effect.value; break;
                }
            }
        }

        return base_ + Mathf.RoundToInt(enemyResult.damage);
    }

    // ── 유틸 ───────────────────────────────────────────────

    private void EnterPhase(BattlePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
        Debug.Log($"[BattleManager] Phase → {phase}");
    }

    private void EndBattle(bool victory)
    {
        OnBattleEnded?.Invoke(victory);
        Debug.Log($"[BattleManager] 전투 종료 — {(victory ? "승리" : "패배")}");
    }

    protected override void Dispose()
    {
        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnChainFinished -= OnChainFinished;

        OnPhaseChanged = null;
        OnBattleEnded = null;
        base.Dispose();
    }
}