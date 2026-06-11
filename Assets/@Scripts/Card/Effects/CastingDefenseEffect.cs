using System.Collections;
using UnityEngine;

/// <summary>캐스팅 수비. 실제 적용은 캐스팅 시스템이 처리.</summary>
[CreateAssetMenu(fileName = "CastingDefenseEffect", menuName = "Game/Effects/CastingDefense")]
public class CastingDefenseEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.CastingDefense;

    // 실행 로직 없음 — 식별자(EffectTypeId)로 다른 시스템이 처리한다.
    public override IEnumerator Apply(EffectContext ctx)
    {
        yield break;
    }
}
