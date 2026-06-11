using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 포식형: 화살표 방향 range 내의 아군/특수 기물을 모두 소멸시키고,
/// 먹은 수 × value 만큼 영구 공격력(BonusDamage)을 얻는다.
/// </summary>
[CreateAssetMenu(fileName = "DevourEffect", menuName = "Game/Effects/Devour")]
public class DevourEffect : CardEffectBase
{
    public override IEnumerator Apply(EffectContext ctx)
    {
        CardView card = ctx.Card;
        if (card?.Data == null || card.CurrentSlot == null || GridManager.Instance == null)
            yield break;

        // 범위 내의 먹잇감 스캔 (루프 도중 파괴로 인한 에러 방지용 리스트)
        List<CardView> targetsToDevour = new();
        foreach (CardDirection dir in card.Data.GetAllDirections())
        {
            GridSlot current = card.CurrentSlot;
            for (int i = 0; i < card.Data.range; i++)
            {
                GridSlot neighbor = GridManager.Instance.GetNeighbor(current, dir);
                if (neighbor == null) break;

                CardView targetCard = neighbor.OccupiedCard;
                // 적이 아니고, 빈 칸이 아니며, 아직 예약되지 않은 아군/특수 기물이라면
                if (targetCard != null && !targetCard.IsEnemy && !targetsToDevour.Contains(targetCard))
                    targetsToDevour.Add(targetCard);

                current = neighbor;
            }
        }

        // 일괄 포식
        int devourCount = 0;
        foreach (CardView target in targetsToDevour)
        {
            devourCount++;
            CardManager.Instance.ExileCard(target); // 토템 장판 등도 알아서 갱신
        }

        // 먹은 개수만큼 영구 공격력 상승
        if (devourCount > 0)
        {
            int gainAmount = ctx.IntValue * devourCount;
            ctx.AddBonusDamage(gainAmount);
        }
    }
}
