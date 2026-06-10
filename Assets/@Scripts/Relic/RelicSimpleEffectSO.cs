using UnityEngine;

public enum RelicSimpleEffectType
{
    Damage,
    Defense,
    Heal,
    Draw,
    GainCost
}

[CreateAssetMenu(menuName = "Game/Relic Effect/Simple Effect", fileName = "RelicSimpleEffect")]
public class RelicSimpleEffectSO : RelicEffectSO
{
    [Header("Effect")]
    public RelicSimpleEffectType effectType;
    public int value = 1;

    public override void Apply(RelicEffectContext context)
    {
        int amount = Mathf.Max(1, value);

        switch (effectType)
        {
            case RelicSimpleEffectType.Damage:
                BattleManager.Instance?.DealDamageToEnemy(amount);
                break;
            case RelicSimpleEffectType.Defense:
                BattleManager.Instance?.Player?.AddDefense(amount);
                break;
            case RelicSimpleEffectType.Heal:
                BattleManager.Instance?.Player?.Heal(amount);
                break;
            case RelicSimpleEffectType.Draw:
                CardManager.Instance?.DrawToHand(amount);
                break;
            case RelicSimpleEffectType.GainCost:
                BattleManager.Instance?.Player?.GainCost(amount);
                break;
        }
    }
}
