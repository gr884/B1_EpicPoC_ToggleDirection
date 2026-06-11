using System.Collections;
using UnityEngine;

/// <summary>인싸형: 8방향 인접 카드 수 × value 데미지.</summary>
[CreateAssetMenu(fileName = "PopularityDamageEffect", menuName = "Game/Effects/PopularityDamage")]
public class PopularityDamageEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.PopularityDamage;

    public override IEnumerator Apply(EffectContext ctx)
    {
        if (ctx.Card?.CurrentSlot == null) yield break;

        int neighborCount = ctx.CountAdjacentOccupied();
        int modified = ctx.GetModifiedDamage(ctx.IntValue);
        int total = modified * neighborCount;

        if (total > 0)
            ctx.DealDamage(total);
    }
}
