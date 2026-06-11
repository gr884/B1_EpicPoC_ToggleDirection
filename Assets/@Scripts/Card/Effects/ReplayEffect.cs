using System.Collections;
using UnityEngine;

/// <summary>재발동형. 실제 발동은 ChainExecutor가 체인 흐름에서 처리.</summary>
[CreateAssetMenu(fileName = "ReplayEffect", menuName = "Game/Effects/Replay")]
public class ReplayEffect : CardEffectBase
{
    // 실행 로직 없음 — 체인 시스템이 이 효과 타입을 직접 처리한다.
    public override IEnumerator Apply(EffectContext ctx)
    {
        yield break;
    }
}
