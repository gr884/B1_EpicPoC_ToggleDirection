using System.Collections.Generic;
using UnityEngine;

public class GameFlowManager : SingletonBehaviour<GameFlowManager>
{
    [Header("Battle Settings")]
    [SerializeField] private int _playerHp = 30;
    [SerializeField] private List<EnemyDataSO> _enemyList = new();

    private int _currentEnemyIndex = 0;

    public void Init()
    {
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        BattleManager.Instance.OnBattleEnded += OnBattleEnded;
        Debug.Log("[GameFlowManager] Init");
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Playing:
                _currentEnemyIndex = 0;
                StartBattle();
                break;

            case GameManager.GameState.GameOver:
                // TODO: 게임 오버 처리
                break;

            case GameManager.GameState.GameClear:
                // TODO: 게임 클리어 처리
                break;
        }
    }

    private void OnBattleEnded(bool victory)
    {
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

        // 그리드 리셋 후 다음 전투 시작
        GridManager.Instance.ResetCards();
        StartBattle();
    }

    private void StartBattle()
    {
        EnemyDataSO enemy = _currentEnemyIndex < _enemyList.Count
            ? _enemyList[_currentEnemyIndex]
            : null;

        // 첫 전투면 설정값, 이후엔 현재 HP 이어받기
        int playerHp = _currentEnemyIndex == 0
            ? _playerHp
            : BattleManager.Instance.PlayerHp;

        GridManager.Instance.BuildGrid();
        BattleManager.Instance.StartBattle(playerHp, enemy);
        Debug.Log($"[GameFlowManager] 전투 시작 — {_currentEnemyIndex + 1}/{_enemyList.Count}");
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnBattleEnded -= OnBattleEnded;
        base.Dispose();
    }
}