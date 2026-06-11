using System.Collections;
using UnityEngine;

/// <summary>캐스팅 공격. 실제 적용은 캐스팅 시스템이 효과 타입으로 처리.</summary>
[CreateAssetMenu(fileName = "CastingDamageEffect", menuName = "Game/Effects/CastingDamage")]
public class CastingDamageEffect : CardEffectBase
{
    // 실행 로직 없음 — 캐스팅 시스템이 이 효과 타입을 직접 처리한다.
    public override IEnumerator Apply(EffectContext ctx)
    {
        yield break;
    }
}
