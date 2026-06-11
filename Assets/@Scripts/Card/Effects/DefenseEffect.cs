using System.Collections;
using UnityEngine;

/// <summary>플레이어에게 방어막 부여.</summary>
[CreateAssetMenu(fileName = "DefenseEffect", menuName = "Game/Effects/Defense")]
public class DefenseEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.Player.AddDefense(ctx.IntValue);
        yield break;
    }
}
