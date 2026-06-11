using System.Collections;
using UnityEngine;

/// <summary>덱에서 카드를 손으로 드로우.</summary>
[CreateAssetMenu(fileName = "DrawEffect", menuName = "Game/Effects/Draw")]
public class DrawEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.Draw;

    public override IEnumerator Apply(EffectContext ctx)
    {
        if (CardManager.Instance != null)
            CardManager.Instance.DrawToHand(ctx.IntValue);
        yield break;
    }
}
