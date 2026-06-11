using System.Collections;
using UnityEngine;

/// <summary>토템 공격 오라. 실제 적용은 TotemAuraSystem이 효과 타입으로 처리.</summary>
[CreateAssetMenu(fileName = "TotemAura", menuName = "Game/Effects/TotemAura")]
public class TotemAura : CardEffectBase
{
    // 실행 로직 없음 — 토템 시스템이 이 효과 타입을 직접 처리한다.
    public override IEnumerator Apply(EffectContext ctx)
    {
        yield break;
    }
}
