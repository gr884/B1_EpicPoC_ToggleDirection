using UnityEngine;

[CreateAssetMenu(menuName = "EpicPoC/Card Effect Data", fileName = "CardEffectData")]
public class CardEffectData : ScriptableObject
{
    [Header("능력 수치")]
    public int attackPower;
    public int defensePower;
    public int healPower;
}
