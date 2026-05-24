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

    public void StartBattle(int playerHp, int enemyHp, string enemyName = "Enemy")
    {
        _playerView.Setup("Player", playerHp);
        _enemyView.Setup(enemyName, enemyHp);

        _playerView.OnDied += () => EndBattle(false);
        _enemyView.OnDied += () => EndBattle(true);

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

    private void OnChainFinished()
    {
        if (CurrentPhase == BattlePhase.Turn)
            StartCoroutine(TurnRoutine());
    }

    // ── 자유 배치 확정 루틴 ────────────────────────────────

    private IEnumerator AttackRoutine()
    {
        IsChainRunning = true;

        _enemyView.TakeDamage(ChainExecutor.Instance.ActivatedCards.Count);
        yield return new WaitForSeconds(0.5f);

        if (_enemyView.IsDead) { IsChainRunning = false; yield break; }

        _playerView.TakeDamage(CalculateEnemyDamage());
        yield return new WaitForSeconds(0.5f);

        IsChainRunning = false;

        if (_playerView.IsDead) yield break;

        EnterPhase(BattlePhase.Turn);
        CardManager.Instance.DrawToHand(1);
    }

    // ── 턴 루틴 ────────────────────────────────────────────

    private IEnumerator TurnRoutine()
    {
        IsChainRunning = true;

        _enemyView.TakeDamage(ChainExecutor.Instance.ActivatedCards.Count);
        yield return new WaitForSeconds(0.5f);

        if (_enemyView.IsDead) { IsChainRunning = false; yield break; }

        _playerView.TakeDamage(CalculateEnemyDamage());
        yield return new WaitForSeconds(0.5f);

        IsChainRunning = false;

        if (_playerView.IsDead) yield break;

        // 다음 턴 드로우
        CardManager.Instance.DrawToHand(1);
    }

    // ── 내구도 처리 ────────────────────────────────────────

    public void ReduceAllCardDurability()
    {
        List<GridSlot> toRemove = new();

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.IsEmpty) continue;
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
        // TODO: EnemyDataSO.baseDamage로 대체
        return 5;
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