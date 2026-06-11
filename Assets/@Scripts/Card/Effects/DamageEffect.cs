using System.Collections;
using UnityEngine;

/// <summary>적에게 데미지. 런타임 누적 보너스가 적용된다.</summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/Effects/Damage")]
public class DamageEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.Damage;

    public override IEnumerator Apply(EffectContext ctx)
    {
        int damage = ctx.GetModifiedDamage(ctx.IntValue);
        ctx.DealDamage(damage);
        yield break;
    }
}
