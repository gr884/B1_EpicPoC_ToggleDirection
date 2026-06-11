using System.Collections;
using UnityEngine;

/// <summary>코스트 획득.</summary>
[CreateAssetMenu(fileName = "GainCostEffect", menuName = "Game/Effects/GainCost")]
public class GainCostEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.Player.GainCost(ctx.IntValue);
        yield break;
    }
}
