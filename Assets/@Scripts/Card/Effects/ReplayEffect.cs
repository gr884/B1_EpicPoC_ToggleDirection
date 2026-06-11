using System.Collections;
using UnityEngine;

/// <summary>재발동형. 실제 발동은 ChainExecutor가 체인 흐름에서 처리.</summary>
[CreateAssetMenu(fileName = "ReplayEffect", menuName = "Game/Effects/Replay")]
public class ReplayEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.Replay;

    // 실행 로직 없음 — 식별자(EffectTypeId)로 다른 시스템이 처리한다.
    public override IEnumerator Apply(EffectContext ctx)
    {
        yield break;
    }
}
