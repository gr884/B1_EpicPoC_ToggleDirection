using UnityEngine;

public class GameFlowManager : SingletonBehaviour<GameFlowManager>
{
    [Header("Battle Settings")]
    [SerializeField] private int _playerHp = 30;
    [SerializeField] private EnemyDataSO _enemyData;

    public void Init()
    {
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        Debug.Log("[GameFlowManager] Init");
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Playing:
                GridManager.Instance.BuildGrid();
                int enemyHp = _enemyData != null ? _enemyData.maxHp : 50;
                string enemyName = _enemyData != null ? _enemyData.displayName : "Enemy";
                BattleManager.Instance.StartBattle(_playerHp, enemyHp, enemyName);
                break;

            case GameManager.GameState.GameOver:
                // TODO: 게임 오버 처리
                break;

            case GameManager.GameState.GameClear:
                // TODO: 게임 클리어 처리
                break;
        }
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        base.Dispose();
    }
}