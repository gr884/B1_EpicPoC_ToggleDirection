using System.Collections;
using UnityEngine;

/// <summary>마무리형: 그리드 전체 ON 카드 수 × value 데미지.</summary>
[CreateAssetMenu(fileName = "FinisherDamageEffect", menuName = "Game/Effects/FinisherDamage")]
public class FinisherDamageEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.FinisherDamage;

    public override IEnumerator Apply(EffectContext ctx)
    {
        int onCount = ctx.CountActivatedOnGrid();
        int modified = ctx.GetModifiedDamage(Mathf.RoundToInt(ctx.Value));
        int total = modified * onCount;

        if (total > 0)
            ctx.DealDamage(total);
        yield break;
    }
}
