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
    private bool _isWaitingForNextBattle;

    public BattlePhase CurrentPhase { get; private set; }
    public bool IsProcessing { get; private set; }
    public bool IsWaitingForNextBattle => _isWaitingForNextBattle;

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action OnBattleEnded;

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

    public void StartBattle()
    {
        _currentEnemyIndex = 0;
        _isWaitingForNextBattle = false;
        StartBattleInternal();
    }

    public void NextBattle()
    {
        _isWaitingForNextBattle = false;
        StartBattleInternal();
    }

    public bool TryConsumeNextBattleRequest()
    {
        if (!_isWaitingForNextBattle) return false;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return false;

        _isWaitingForNextBattle = false;
        return true;
    }

    private void StartBattleInternal()
    {
        EnemyDataSO enemyData = GetCurrentEnemyData();
        if (enemyData == null)
        {
            GameManager.Instance.GameClear();
            return;
        }

        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied -= HandleEnemyDied;
        _player.OnDied += HandlePlayerDied;
        _enemy.OnDied += HandleEnemyDied;

        _enemy.Setup(enemyData);

        EnterPhase(BattlePhase.PlayerTurn);

        Debug.Log($"[BattleManager] Battle Start {_currentEnemyIndex + 1}/{_enemyList.Count}");
    }

    public void ConfirmPlayerTurn()
    {
        if (_isWaitingForNextBattle) return;
        if (CurrentPhase != BattlePhase.PlayerTurn || IsProcessing) return;
        StartCoroutine(PlayerTurnRoutine());
    }

    public void ApplyImmediateEffect(EffectType type, float value)
    {
        if (_isWaitingForNextBattle) return;
        if (CurrentPhase != BattlePhase.PlayerTurn || IsProcessing) return;

        int amount = Mathf.RoundToInt(value);
        if (amount <= 0) return;

        switch (type)
        {
            case EffectType.Damage:
                _enemy.TakeAttack(amount);
                break;
            case EffectType.Defense:
                _player.GainBlock(amount);
                break;
            case EffectType.Heal:
                _player.Heal(amount);
                break;
        }
    }

    private IEnumerator PlayerTurnRoutine()
    {
        IsProcessing = true;
        EnterPhase(BattlePhase.EnemyTurn);

        // Enemy's previously-held block expires when its turn starts.
        _enemy.ClearBlock();

        int enemyAttack = _enemy.GetIntentValue(EnemyIntentType.Attack);
        int enemyDefend = _enemy.GetIntentValue(EnemyIntentType.Defend);

        _player.TakeAttackWithBlock(enemyAttack);
        yield return new WaitForSeconds(0.35f);

        if (_player.IsDead)
        {
            IsProcessing = false;
            yield break;
        }

        // Enemy intent defend is executed now and stays during the next player turn.
        _enemy.GainBlock(enemyDefend);
        yield return new WaitForSeconds(0.15f);

        _enemy.AdvanceIntent();
        CardManager.Instance.DiscardGrid();

        IsProcessing = false;
        EnterPhase(BattlePhase.PlayerTurn);
        CardManager.Instance.DiscardAndDraw();
    }

    private void HandlePlayerDied() => EndBattle(false);
    private void HandleEnemyDied() => EndBattle(true);

    private void EndBattle(bool victory)
    {
        // Stop in-flight turn coroutines to avoid applying stale enemy attacks after death.
        StopAllCoroutines();
        IsProcessing = false;
        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.CancelCurrentChain();

        Debug.Log($"[BattleManager] Battle End {(victory ? "Victory" : "Defeat")}");

        if (!victory)
        {
            GameManager.Instance.GameOver();
            return;
        }

        _currentEnemyIndex++;

        if (GetCurrentEnemyData() == null)
        {
            GameManager.Instance.GameClear();
            return;
        }

        _isWaitingForNextBattle = true;
        EnterPhase(BattlePhase.EnemyTurn); // hide confirm button while waiting for next battle
        OnBattleEnded?.Invoke();
    }

    private void EnterPhase(BattlePhase phase)
    {
        CurrentPhase = phase;

        if (phase == BattlePhase.PlayerTurn)
        {
            _player.ClearBlock();
            ChainExecutor.Instance.ResetAccumulatedResult();
            CardManager.Instance.ResetTurnCost();
        }

        OnPhaseChanged?.Invoke(phase);
        Debug.Log($"[BattleManager] Phase {phase}");
    }

    private EnemyDataSO GetCurrentEnemyData()
    {
        while (_currentEnemyIndex < _enemyList.Count)
        {
            EnemyDataSO data = _enemyList[_currentEnemyIndex];
            if (data != null) return data;
            _currentEnemyIndex++;
        }
        return null;
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
