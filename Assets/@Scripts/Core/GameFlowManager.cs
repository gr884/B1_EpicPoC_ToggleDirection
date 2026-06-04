using UnityEngine;

public class GameFlowManager : SingletonBehaviour<GameFlowManager>
{
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
                CardManager.Instance.ClearCombatPersistentStates();
                CardManager.Instance.DiscardHand();
                CardManager.Instance.DiscardGrid();
                CardManager.Instance.ResetDeck();
                GridManager.Instance.BuildGrid();
                CardManager.Instance.StartBattleDraw();
                BattleManager.Instance.StartBattle();
                break;

            case GameManager.GameState.Tutorial:
                // TutorialManager가 OnStateChanged 구독해서 직접 처리
                break;

            case GameManager.GameState.GameOver:
            case GameManager.GameState.GameClear:
                // TODO
                break;
        }
    }

    // UI_RewardPopup이 OnBattleEnded 구독 후 선택 완료 시 호출
    public void OnRewardClosed()
    {
        CardManager.Instance.ClearCombatPersistentStates();
        CardManager.Instance.DiscardHand();
        CardManager.Instance.DiscardGrid();
        GridManager.Instance.BuildGrid();
        CardManager.Instance.StartBattleDraw();
        BattleManager.Instance.NextBattle();
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        base.Dispose();
    }
}
