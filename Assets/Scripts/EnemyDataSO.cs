using UnityEngine;

[CreateAssetMenu(menuName = "EpicPoC/Enemy Data", fileName = "EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    public string enemyId;
    public string displayName = "Enemy";
    [Min(1)] public int maxHp = 20;
    public Sprite sprite;
    public Vector2 worldSize = new(2f, 2f);
}
