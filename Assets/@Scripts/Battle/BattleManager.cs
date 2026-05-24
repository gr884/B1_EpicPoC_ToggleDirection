using System;
using System.Collections;
using UnityEngine;

public class BattleManager : SingletonBehaviour<BattleManager>
{
    public enum Phase { Phase1, Phase2 }

    [Header("Battle Settings")]
    [SerializeField] private int _phase2TurnsPerCycle = 5;

    [Header("Actor Views")]
    [SerializeField] private BattleActorView _playerView;
    [SerializeField] private BattleActorView _enemyView;

    // ── 전투 상태 ──────────────────────────────────────────
    public Phase CurrentPhase { get; private set; }
    public int CurrentTurn { get; private set; }
    public bool IsChainRunning { get; private set; }

    // ── 이벤트 ────────────────────────────────────────────
    public event Action<Phase> OnPhaseChanged;
    public event Action OnCycleReset;
    public event Action<bool> OnBattleEnded; // true = 승리

    public void Init()
    {
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        Debug.Log("[BattleManager] Init");
    }

    // ── 전투 시작 ──────────────────────────────────────────

    public void StartBattle(int playerHp, int enemyHp, string enemyName = "Enemy")
    {
        CurrentTurn = 0;

        _playerView.Setup("Player", playerHp);
        _enemyView.Setup(enemyName, enemyHp);

        _playerView.OnDied += () => EndBattle(false);
        _enemyView.OnDied += () => EndBattle(true);

        EnterPhase(Phase.Phase1);

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

        _enemyView.TakeDamage(ChainExecutor.Instance.ActivatedCards.Count);
        yield return new WaitForSeconds(0.5f);

        if (_enemyView.IsDead) { IsChainRunning = false; yield break; }

        _playerView.TakeDamage(CalculateEnemyDamage());
        yield return new WaitForSeconds(0.5f);

        IsChainRunning = false;

        if (_playerView.IsDead) yield break;

        EnterPhase(Phase.Phase2);
    }

    // ── Phase2 루틴 ────────────────────────────────────────

    private IEnumerator Phase2TurnRoutine()
    {
        IsChainRunning = true;

        _enemyView.TakeDamage(ChainExecutor.Instance.ActivatedCards.Count);
        yield return new WaitForSeconds(0.5f);

        if (_enemyView.IsDead) { IsChainRunning = false; yield break; }

        _playerView.TakeDamage(CalculateEnemyDamage());
        yield return new WaitForSeconds(0.5f);

        IsChainRunning = false;

        if (_playerView.IsDead) yield break;

        CurrentTurn++;
        if (CurrentTurn >= _phase2TurnsPerCycle)
            StartCycleReset();
    }

    // ── 사이클 리셋 ────────────────────────────────────────

    private void StartCycleReset()
    {
        CurrentTurn = 0;
        GridManager.Instance.ResetCards();
        CardManager.Instance.DiscardHand();
        OnCycleReset?.Invoke();
        EnterPhase(Phase.Phase1);
        CardManager.Instance.StartBattleDraw();

        Debug.Log("[BattleManager] 사이클 리셋");
    }

    // ── 데미지 계산 ────────────────────────────────────────

    private int CalculateEnemyDamage()
    {
        // TODO: EnemyDataSO.baseDamage로 대체
        return 5;
    }

    // ── 유틸 ───────────────────────────────────────────────

    private void EnterPhase(Phase phase)
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
        OnCycleReset = null;
        OnBattleEnded = null;
        base.Dispose();
    }
}