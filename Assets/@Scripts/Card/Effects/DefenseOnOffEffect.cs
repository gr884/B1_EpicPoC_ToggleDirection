using System.Collections;
using UnityEngine;

/// <summary>반전형: ON일 때 Damage(value). OFF 시 방어는 ChainExecutor가 secondaryValue로 별도 처리.</summary>
[CreateAssetMenu(fileName = "DefenseOnOffEffect", menuName = "Game/Effects/DefenseOnOff")]
public class DefenseOnOffEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        int damage = ctx.GetModifiedDamage(ctx.IntValue);
        ctx.DealDamage(damage);
        yield break;
    }
}
