using UnityEngine;

[CreateAssetMenu(menuName = "EpicPoC/Battle Round", fileName = "BattleRound")]
public class BattleRoundSO : ScriptableObject
{
    public GridDataSO gridData;
    public EnemyDeckSO enemyDeck;
    [Min(0)] public int enemyCardsPerTurn = 1;
    public UserCardPool playerDeckOverride;
}
