using System.Collections;
using UnityEngine;

/// <summary>플레이어 체력 회복.</summary>
[CreateAssetMenu(fileName = "HealEffect", menuName = "Game/Effects/Heal")]
public class HealEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.Heal;

    public override IEnumerator Apply(EffectContext ctx)
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.Player.Heal(ctx.IntValue);
        yield break;
    }
}
