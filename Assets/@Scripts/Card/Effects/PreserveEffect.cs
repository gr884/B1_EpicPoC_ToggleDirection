using System.Collections;
using UnityEngine;

/// <summary>방향으로 연결된 이웃 카드에 보존 스택을 쌓는다.</summary>
[CreateAssetMenu(fileName = "PreserveEffect", menuName = "Game/Effects/Preserve")]
public class PreserveEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        ctx.ApplyPreserveToNeighbors(ctx.IntValue);
        yield break;
    }
}
