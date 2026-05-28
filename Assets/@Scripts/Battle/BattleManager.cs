using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : SingletonBehaviour<BattleManager>
{
    public enum BattlePhase { PlayerTurn, EnemyTurn }

    [Header("Actors")]
    [SerializeField] private Player _player;
    [SerializeField] private Enemy _enemy;

    [Header("Battle Settings")]
    [SerializeField] private List<EnemyDataSO> _enemyList = new();

    private int _currentEnemyIndex = 0;

    public BattlePhase CurrentPhase { get; private set; }
    public bool IsProcessing { get; private set; }

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action OnBattleEnded;

    private ChainResult _lastChainResult;

    private void OnChainFinished(ChainResult result) => _lastChainResult = result;

    // ── 초기화 ─────────────────────────────────────────────

    public void Init()
    {
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        Debug.Log("[BattleManager] Init");
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
            _player.Setup(_player.MaxHp);
    }

    // ── 전투 시작 ──────────────────────────────────────────

    public void StartBattle()
    {
        _currentEnemyIndex = 0;
        StartBattleInternal();
    }

    public void NextBattle()
    {
        StartBattleInternal();
    }

    private void StartBattleInternal()
    {
        EnemyDataSO enemyData = _currentEnemyIndex < _enemyList.Count
            ? _enemyList[_currentEnemyIndex]
            : null;

        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied -= HandleEnemyDied;
        _player.OnDied += HandlePlayerDied;
        _enemy.OnDied += HandleEnemyDied;

        _enemy.Setup(enemyData);

        EnterPhase(BattlePhase.PlayerTurn);

        Debug.Log($"[BattleManager] 전투 시작 — {_currentEnemyIndex + 1}/{_enemyList.Count}");
    }

    // ── 플레이어 턴 확정 ───────────────────────────────────

    public void ConfirmPlayerTurn()
    {
        if (CurrentPhase != BattlePhase.PlayerTurn || IsProcessing) return;
        StartCoroutine(PlayerTurnRoutine());
    }

    // ── 턴 루틴 ────────────────────────────────────────────

    private IEnumerator PlayerTurnRoutine()
    {
        IsProcessing = true;

        ChainResult result = _lastChainResult ?? new ChainResult();

        int playerDamage = Mathf.RoundToInt(result.damage);
        int playerDefense = Mathf.RoundToInt(result.defense);
        int playerHeal = Mathf.RoundToInt(result.heal);

        // 플레이어 공격 - 적 방어
        _enemy.TakeAttack(playerDamage);
        yield return new WaitForSeconds(0.5f);

        if (_enemy.IsDead) { IsProcessing = false; yield break; }

        // 힐
        if (playerHeal > 0)
            _player.Heal(playerHeal);

        // 적 공격 - 플레이어 방어
        int enemyAttack = _enemy.GetIntentValue(EnemyIntentType.Attack);
        int remaining = Mathf.Max(0, enemyAttack - playerDefense);
        _player.TakeAttack(remaining);
        yield return new WaitForSeconds(0.5f);

        if (_player.IsDead) { IsProcessing = false; yield break; }

        // Intent 갱신
        _enemy.AdvanceIntent();

        IsProcessing = false;
        EnterPhase(BattlePhase.EnemyTurn);
        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        IsProcessing = true;

        // EnemyTurn은 짧게 — Intent 표시 후 PlayerTurn으로
        yield return new WaitForSeconds(0.5f);

        IsProcessing = false;
        EnterPhase(BattlePhase.PlayerTurn);
        CardManager.Instance.DiscardAndDraw();
    }

    // ── 전투 종료 ──────────────────────────────────────────

    private void HandlePlayerDied() => EndBattle(false);
    private void HandleEnemyDied() => EndBattle(true);

    private void EndBattle(bool victory)
    {
        Debug.Log($"[BattleManager] 전투 종료 — {(victory ? "승리" : "패배")}");

        if (!victory)
        {
            GameManager.Instance.GameOver();
            return;
        }

        _currentEnemyIndex++;

        if (_currentEnemyIndex >= _enemyList.Count)
        {
            GameManager.Instance.GameClear();
            return;
        }

        OnBattleEnded?.Invoke();
    }

    // ── 유틸 ───────────────────────────────────────────────

    private void EnterPhase(BattlePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
        Debug.Log($"[BattleManager] Phase → {phase}");
    }

    protected override void Dispose()
    {
        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnChainFinished -= OnChainFinished;
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;

        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied -= HandleEnemyDied;

        OnPhaseChanged = null;
        OnBattleEnded = null;
        base.Dispose();
    }
}