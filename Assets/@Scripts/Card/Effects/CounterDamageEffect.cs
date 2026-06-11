using System.Collections;
using UnityEngine;

/// <summary>카운터형: value + 이번 턴 그리드 전체 ON 횟수만큼 데미지.</summary>
[CreateAssetMenu(fileName = "CounterDamageEffect", menuName = "Game/Effects/CounterDamage")]
public class CounterDamageEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        int finalDamage = ctx.IntValue + ctx.TurnToggleCount;
        ctx.DealDamage(finalDamage);
        yield break;
    }
}
