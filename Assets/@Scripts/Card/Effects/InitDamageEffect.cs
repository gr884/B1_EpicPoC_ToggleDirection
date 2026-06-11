using System.Collections;
using UnityEngine;

/// <summary>소진형 초기화: 배치 시 누적 데미지를 value로 덮어쓴다(누적 아님).</summary>
[CreateAssetMenu(fileName = "InitDamageEffect", menuName = "Game/Effects/InitDamage")]
public class InitDamageEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        ctx.SetBonusDamage(Mathf.RoundToInt(ctx.Value));
        yield break;
    }
}
