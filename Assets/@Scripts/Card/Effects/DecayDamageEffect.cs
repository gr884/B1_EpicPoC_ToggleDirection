using System.Collections;
using UnityEngine;

/// <summary>누적 보너스 데미지 -N (0 아래로 내려가지 않음).</summary>
[CreateAssetMenu(fileName = "DecayDamageEffect", menuName = "Game/Effects/DecayDamage")]
public class DecayDamageEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        ctx.DeductBonusDamage(Mathf.RoundToInt(ctx.Value));
        yield break;
    }
}
