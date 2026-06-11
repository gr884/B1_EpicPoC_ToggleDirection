using System.Collections;
using UnityEngine;

/// <summary>토템 공격 오라. 실제 적용은 TotemAuraSystem이 EffectTypeId로 처리.</summary>
[CreateAssetMenu(fileName = "TotemAura", menuName = "Game/Effects/TotemAura")]
public class TotemAura : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.TotemAura;

    // 실행 로직 없음 — 식별자(EffectTypeId)로 다른 시스템이 처리한다.
    public override IEnumerator Apply(EffectContext ctx)
    {
        yield break;
    }
}
