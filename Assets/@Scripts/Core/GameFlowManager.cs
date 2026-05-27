using UnityEngine;

public class GameFlowManager : SingletonBehaviour<GameFlowManager>
{
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
                GridManager.Instance.BuildGrid();
                CardManager.Instance.StartBattleDraw();
                BattleManager.Instance.StartBattle();
                break;

            case GameManager.GameState.GameOver:
                // TODO: 게임 오버 처리
                break;

            case GameManager.GameState.GameClear:
                // TODO: 게임 클리어 처리
                break;
        }
    }

    private void OnBattleEnded()
    {
        GridManager.Instance.BuildGrid();
        CardManager.Instance.StartBattleDraw();
        BattleManager.Instance.NextBattle();
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