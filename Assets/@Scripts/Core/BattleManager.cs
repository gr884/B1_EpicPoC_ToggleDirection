using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : SingletonBehaviour<BattleManager>
{
    public enum Phase { Phase1, Phase2 }

    [Header("Battle Settings")]
    [SerializeField] private int _phase2TurnsPerCycle = 5;
    [SerializeField] private int _enemyBaseDamage = 5; // TODO: 적 데이터로 관리

    // ── 전투 상태 ──────────────────────────────────────────
    public Phase CurrentPhase { get; private set; }
    public int PlayerHp { get; private set; }
    public int EnemyHp { get; private set; }
    public int CurrentTurn { get; private set; }
    public bool IsChainRunning { get; private set; }

    // ── 이벤트 ────────────────────────────────────────────
    public event Action<Phase> OnPhaseChanged;
    public event Action<int, int> OnHpChanged;  // (playerHp, enemyHp)
    public event Action OnCycleReset;
    public event Action OnBattleEnded;

    public void Init()
    {
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        Debug.Log("[BattleManager] Init");
    }

    // ── 전투 시작 ──────────────────────────────────────────

    public void StartBattle(int playerHp, int enemyHp)
    {
        PlayerHp = playerHp;
        EnemyHp = enemyHp;
        CurrentTurn = 0;

        EnterPhase(Phase.Phase1);
        OnHpChanged?.Invoke(PlayerHp, EnemyHp);

        Debug.Log($"[BattleManager] 전투 시작 — 플레이어 HP: {playerHp} / 적 HP: {enemyHp}");
    }

    // Phase1 배치 확정 — UI 버튼에서 호출
    public void ConfirmPhase1()
    {
        if (CurrentPhase != Phase.Phase1 || IsChainRunning) return;
        StartCoroutine(Phase1AttackRoutine());
    }

    // ── 체인 결과 처리 ─────────────────────────────────────

    private void OnChainFinished()
    {
        if (CurrentPhase == Phase.Phase2)
            StartCoroutine(Phase2TurnRoutine());
    }

    // ── Phase1 루틴 ────────────────────────────────────────

    private IEnumerator Phase1AttackRoutine()
    {
        IsChainRunning = true;

        int playerDamage = ChainExecutor.Instance.ActivatedCards.Count;
        ApplyDamageToEnemy(playerDamage);

        yield return new WaitForSeconds(0.5f);

        if (CheckBattleEnd()) { IsChainRunning = false; yield break; }

        ApplyDamageToPlayer(CalculateEnemyDamage());

        yield return new WaitForSeconds(0.5f);

        IsChainRunning = false;

        if (CheckBattleEnd()) yield break;

        EnterPhase(Phase.Phase2);
    }

    // ── Phase2 루틴 ────────────────────────────────────────

    private IEnumerator Phase2TurnRoutine()
    {
        IsChainRunning = true;

        int playerDamage = ChainExecutor.Instance.ActivatedCards.Count;
        ApplyDamageToEnemy(playerDamage);

        yield return new WaitForSeconds(0.5f);

        if (CheckBattleEnd()) { IsChainRunning = false; yield break; }

        ApplyDamageToPlayer(CalculateEnemyDamage());

        yield return new WaitForSeconds(0.5f);

        IsChainRunning = false;

        if (CheckBattleEnd()) yield break;

        CurrentTurn++;
        if (CurrentTurn >= _phase2TurnsPerCycle)
            StartCycleReset();
    }

    // ── 사이클 리셋 ────────────────────────────────────────

    private void StartCycleReset()
    {
        CurrentTurn = 0;
        GridManager.Instance.ResetCards();
        OnCycleReset?.Invoke();
        EnterPhase(Phase.Phase1);

        Debug.Log("[BattleManager] 사이클 리셋");
    }

    // ── 데미지 계산 ────────────────────────────────────────

    private int CalculateEnemyDamage()
    {
        // TODO: 적 카드 활성화 여부에 따른 보너스는 추후 추가
        return _enemyBaseDamage;
    }

    // ── HP 적용 ────────────────────────────────────────────

    private void ApplyDamageToEnemy(int damage)
    {
        EnemyHp = Mathf.Max(0, EnemyHp - damage);
        OnHpChanged?.Invoke(PlayerHp, EnemyHp);
        Debug.Log($"[BattleManager] 적에게 {damage} 데미지 → 적 HP: {EnemyHp}");
    }

    private void ApplyDamageToPlayer(int damage)
    {
        PlayerHp = Mathf.Max(0, PlayerHp - damage);
        OnHpChanged?.Invoke(PlayerHp, EnemyHp);
        Debug.Log($"[BattleManager] 플레이어에게 {damage} 데미지 → 플레이어 HP: {PlayerHp}");
    }

    // ── 유틸 ───────────────────────────────────────────────

    private void EnterPhase(Phase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
        Debug.Log($"[BattleManager] Phase → {phase}");
    }

    private bool CheckBattleEnd()
    {
        if (PlayerHp <= 0 || EnemyHp <= 0)
        {
            OnBattleEnded?.Invoke();
            Debug.Log($"[BattleManager] 전투 종료 — {(PlayerHp <= 0 ? "패배" : "승리")}");
            return true;
        }
        return false;
    }

    protected override void Dispose()
    {
        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnChainFinished -= OnChainFinished;

        OnPhaseChanged = null;
        OnHpChanged = null;
        OnCycleReset = null;
        OnBattleEnded = null;
        base.Dispose();
    }
}