using System.Collections;
using UnityEngine;

/// <summary>누적 보너스 데미지 +N (영구).</summary>
[CreateAssetMenu(fileName = "GainDamageEffect", menuName = "Game/Effects/GainDamage")]
public class GainDamageEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.GainDamage;

    public override IEnumerator Apply(EffectContext ctx)
    {
        ctx.AddBonusDamage(Mathf.RoundToInt(ctx.Value));
        yield break;
    }
}
