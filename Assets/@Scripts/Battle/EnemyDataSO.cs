using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    public string enemyId;
    public string displayName = "Enemy";
    [Min(1)] public int maxHp = 20;
    [Min(0)] public int baseDamage = 5;
    public Sprite sprite;
}