using System.Collections;
using UnityEngine;

/// <summary>
/// 흡수형: 화살표 방향 이웃 카드들의 데미지 계열 효과값을 합산해 적에게 가한다.
/// 이웃 카드의 effects를 직접 순회하므로 그리드에 직접 접근한다.
/// </summary>
[CreateAssetMenu(fileName = "DirectionalDamageBonusEffect", menuName = "Game/Effects/DirectionalDamageBonus")]
public class DirectionalDamageBonusEffect : CardEffectBase
{
    public override EffectType EffectTypeId => EffectType.DirectionalDamageBonus;

    public override IEnumerator Apply(EffectContext ctx)
    {
        CardView card = ctx.Card;
        if (card?.Data == null || card.CurrentSlot == null || GridManager.Instance == null)
            yield break;

        int totalDamage = 0;
        foreach (CardDirection dir in card.Data.GetAllDirections())
        {
            GridSlot neighbor = GridManager.Instance.GetNeighbor(card.CurrentSlot, dir);
            if (neighbor == null) continue;

            CardView targetCard = neighbor.OccupiedCard;
            if (targetCard == null || targetCard.Data == null) continue;
            var targetRuntime = targetCard.GetComponent<CardRuntimeState>();

            foreach (CardEffect e in targetCard.Data.effects)
            {
                if (e.effect == null) continue;
                EffectType typeId = e.effect.EffectTypeId;

                bool isdmg = typeId == EffectType.Damage
                          || typeId == EffectType.DefenseOnOff
                          || typeId == EffectType.CounterDamage;
                if (!isdmg) continue;

                float adjusted = ctx.GetTotemAdjustedFor(targetCard, typeId, e.value);
                int targetBaseDamage = Mathf.Max(1, Mathf.RoundToInt(adjusted));
                int finalDamage = targetRuntime != null
                    ? targetRuntime.GetModifiedDamage(targetBaseDamage)
                    : targetBaseDamage;

                // 카운트 기물이라면 적용될 카운트 횟수를 추가
                if (typeId == EffectType.CounterDamage)
                    targetBaseDamage += ctx.TurnToggleCount;

                totalDamage += finalDamage;
            }
        }

        ctx.DealDamage(totalDamage);
    }
}
