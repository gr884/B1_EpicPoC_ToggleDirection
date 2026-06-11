using System.Collections;
using UnityEngine;

/// <summary>폭발형: directions 방향 카드를 강제 ON시킨 뒤 자신은 소멸. 체인 재귀는 컨텍스트에 위임.</summary>
[CreateAssetMenu(fileName = "ExplodeEffect", menuName = "Game/Effects/Explode")]
public class ExplodeEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        yield return ctx.RunExplodeChain();
    }
}
