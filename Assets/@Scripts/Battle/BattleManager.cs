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
    private bool _isBattleActive;

    public BattlePhase CurrentPhase { get; private set; }
    public bool IsProcessing { get; private set; }
    public Player Player => _player;
    public Enemy Enemy => _enemy;

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action OnBattleEnded;

    // ── 초기화 ─────────────────────────────────────────────

    public void Init()
    {
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

        _isBattleActive = true;
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

    // ── 전투 액션 ──────────────────────────────────────────

    public void DealDamageToEnemy(int damage)
    {
        _enemy.TakeAttack(damage);
    }

    // ── 턴 루틴 ────────────────────────────────────────────

    private IEnumerator PlayerTurnRoutine()
    {
        IsProcessing = true;

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        _player.ResetDefense();

        IsProcessing = false;
        EnterPhase(BattlePhase.EnemyTurn);
        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        IsProcessing = true;

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        // 현재 Intent 실행: Defend → Defense 쌓음, Attack → 플레이어 피격
        _enemy.ExecuteIntents();
        int enemyAttack = _enemy.GetIntentValue(EnemyIntentType.Attack);
        if (enemyAttack > 0)
            _player.TakeAttack(enemyAttack);

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        // 다음 플레이어 턴에 보여줄 Intent로 갱신 (실행 아님)
        _enemy.AdvanceIntent();

        CardManager.Instance.DiscardGrid();
        IsProcessing = false;
        EnterPhase(BattlePhase.PlayerTurn);
        CardManager.Instance.DiscardAndDraw();
    }

    // ── 전투 종료 ──────────────────────────────────────────

    private void HandlePlayerDied() => StartCoroutine(EndBattleRoutine(false));
    private void HandleEnemyDied() => StartCoroutine(EndBattleRoutine(true));

    private IEnumerator EndBattleRoutine(bool victory)
    {
        _isBattleActive = false;
        IsProcessing = false;
        Debug.Log($"[BattleManager] 전투 종료 — {(victory ? "승리" : "패배")}");

        yield return new WaitForSeconds(1f);

        if (!victory)
        {
            GameManager.Instance.GameOver();
            yield break;
        }

        _currentEnemyIndex++;

        if (_currentEnemyIndex >= _enemyList.Count)
        {
            GameManager.Instance.GameClear();
            yield break;
        }

        OnBattleEnded?.Invoke();
    }

    // ── 유틸 ───────────────────────────────────────────────

    private void EnterPhase(BattlePhase phase)
    {
        CurrentPhase = phase;

        if (phase == BattlePhase.PlayerTurn)
            _player.RestoreCost();

        OnPhaseChanged?.Invoke(phase);
        Debug.Log($"[BattleManager] Phase → {phase}");
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;

        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied -= HandleEnemyDied;

        OnPhaseChanged = null;
        OnBattleEnded = null;
        base.Dispose();
    }
}