using System.Collections;
using UnityEngine;

/// <summary>토템 수비 오라. 실제 적용은 TotemAuraSystem이 처리.</summary>
[CreateAssetMenu(fileName = "TotemAuraDefense", menuName = "Game/Effects/TotemAuraDefense")]
public class TotemAuraDefense : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.TotemAura_Defense;

    // 실행 로직 없음 — 식별자(EffectTypeId)로 다른 시스템이 처리한다.
    public override IEnumerator Apply(EffectContext ctx)
    {
        yield break;
    }
}
