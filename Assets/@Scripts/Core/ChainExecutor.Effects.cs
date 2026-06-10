using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ChainExecutor의 효과 적용 관련 로직 (partial).
// 체인 전파(ActivateChainFrom 등)는 ChainExecutor.cs에, 효과 처리는 이 파일에 둔다.
public partial class ChainExecutor
{
    // ── 효과 처리 ──────────────────────────────────────────

    private IEnumerator ApplyEffects(CardView card, EffectTrigger trigger = EffectTrigger.OnActivated)
    {
        if (card?.Data?.effects == null) yield break;

        var atLeastBest = new Dictionary<EffectType, (int threshold, float value)>();

        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.trigger != trigger) continue;

            if (effect.thresholdType == ThresholdType.Full)
            {
                if (IsFullActivated(effect.scope, card))
                    yield return ApplyEffectWithVisual(effect.effectType, effect.value, card, trigger);
                continue;
            }

            if (effect.scope == CountScope.None)
            {
                yield return ApplyEffectWithVisual(effect.effectType, effect.value, card, trigger);
                continue;
            }

            int count = CountByScope(effect.scope, card);
            if (count < effect.threshold) continue;

            if (!atLeastBest.ContainsKey(effect.effectType) ||
                effect.threshold > atLeastBest[effect.effectType].threshold)
            {
                atLeastBest[effect.effectType] = (effect.threshold, effect.value);
            }
        }

        foreach (var kv in atLeastBest)
            yield return ApplyEffectWithVisual(kv.Key, kv.Value.value, card, trigger);
    }

    private IEnumerator ApplyEffectWithVisual(
        EffectType type,
        float value,
        CardView card,
        EffectTrigger trigger)
    {
        float resolvedValue = GetTotemAdjustedValue(card, type, value);

        if (type == EffectType.InitDamage)
        {
            yield return ApplyEffect(type, resolvedValue, card);
            yield break;
        }

        if (_cardEffectPlaySystem != null && _cardEffectPlaySystem.HasAssignedVisual(card, type))
        {
            bool scheduledOnImpact = _cardEffectPlaySystem.PlayAssignedEffectDetached(
                card,
                type,
                ToQueuedEffectTiming(trigger),
                () => StartCoroutine(ApplyEffect(type, resolvedValue, card)),
                value: resolvedValue);

            if (scheduledOnImpact)
                yield break;
        }

        yield return ApplyEffect(type, resolvedValue, card);
    }

    private static CardEffectPlaySystem.QueuedEffectTiming ToQueuedEffectTiming(EffectTrigger trigger)
    {
        return trigger switch
        {
            EffectTrigger.OnActivated => CardEffectPlaySystem.QueuedEffectTiming.OnActivated,
            EffectTrigger.OnTurnEnd => CardEffectPlaySystem.QueuedEffectTiming.OnTurnEnd,
            EffectTrigger.OnTurnStart => CardEffectPlaySystem.QueuedEffectTiming.OnTurnStart,
            _ => CardEffectPlaySystem.QueuedEffectTiming.Direct,
        };
    }

    private IEnumerator ApplyEffect(EffectType type, float value, CardView card)
    {
        var runtime = card != null ? card.GetComponent<CardRuntimeState>() : null;
        switch (type)
        {
            case EffectType.Damage:
                int baseDamage = Mathf.Max(1, Mathf.RoundToInt(value));
                int damage = runtime != null ? runtime.GetModifiedDamage(baseDamage) : baseDamage;
                DealDamageToEnemy(damage);
                break;
            case EffectType.Defense:
                int defense = Mathf.Max(1, Mathf.RoundToInt(value));
                BattleManager.Instance.Player.AddDefense(defense);
                break;
            case EffectType.Heal:
                int heal = Mathf.Max(1, Mathf.RoundToInt(value));
                BattleManager.Instance.Player.Heal(heal);
                break;
            case EffectType.DirectionalDamageBonus:
                int totalDamage = 0;
                foreach (var dir in card.Data.GetAllDirections())
                {
                    GridSlot neighbor = GridManager.Instance.GetNeighbor(card.CurrentSlot, dir);
                    if (neighbor == null) continue;
                    var targetCard = neighbor.OccupiedCard;
                    if (targetCard == null || targetCard.Data == null) continue;
                    var targetRuntime = targetCard.GetComponent<CardRuntimeState>();

                    foreach (var e in targetCard.Data.effects)
                    {
                        bool isdmg = (e.effectType == EffectType.Damage) ||
                                        (e.effectType == EffectType.DefenseOnOff) ||
                                        (e.effectType == EffectType.CounterDamage);
                        if (!isdmg) continue;
                        float adjusted = GetTotemAdjustedValue(targetCard, e.effectType, e.value);
                        int targetBaseDamage = Mathf.Max(1, Mathf.RoundToInt(adjusted));
                        int finalDamage = targetRuntime != null
                            ? targetRuntime.GetModifiedDamage(targetBaseDamage)
                            : targetBaseDamage;
                        // 카운트 기물이라면 적용될 카운트 횟수를 가져와 추가
                        if (e.effectType == EffectType.CounterDamage)
                            targetBaseDamage += _turnToggleCount;
                        // 인싸 기물이라면 증가된 횟수를 가져와 증가
                        else if (e.effectType == EffectType.PopularityDamage)
                        {
                            // 인싸 주위 기물의 개수에 따라 계산
                            int popCount = 0;
                            foreach (CardDirection d in Enum.GetValues(typeof(CardDirection)))
                            {
                                if (d == CardDirection.None) continue;
                                GridSlot popNeighbor = GridManager.Instance.GetNeighbor(targetCard.CurrentSlot, d);
                                if (popNeighbor != null && !popNeighbor.IsEmpty) popCount++;
                            }
                            finalDamage *= popCount;
                        }

                        totalDamage += finalDamage;
                    }
                }
                DealDamageToEnemy(totalDamage);
                break;
            case EffectType.Draw:
                int drawCount = Mathf.Max(1, Mathf.RoundToInt(value));
                CardManager.Instance.DrawToHand(drawCount);
                break;
            case EffectType.GainCost:
                {
                    int gainAmount = Mathf.Max(1, Mathf.RoundToInt(value));
                    BattleManager.Instance.Player.GainCost(gainAmount);
                    break;
                }
            case EffectType.Preserve:
                int preserveAmount = Mathf.Max(1, Mathf.RoundToInt(value));
                ApplyPreserveToNeighbors(card, preserveAmount);
                break;
            case EffectType.GainDamage:
                if (runtime != null)
                    runtime.AddBonusDamage(Mathf.RoundToInt(value));
                break;
            case EffectType.DecayDamage:
                if (runtime != null)
                    runtime.DeductBonusDamage(Mathf.RoundToInt(value));
                break;
            case EffectType.DefenseOnOff:
                // 기본 데미지 + 토템 데미지
                int baseDualDamage = Mathf.Max(1, Mathf.RoundToInt(value));
                // 최종 데미지
                int modifiedDualDamage = runtime != null ?
                    runtime.GetModifiedDamage(baseDualDamage) : baseDualDamage;
                DealDamageToEnemy(modifiedDualDamage);
                break;
            case EffectType.CounterDamage:
                // 토템 보너스가 합산된 데미지
                int baseCounterDamage = Mathf.Max(1, Mathf.RoundToInt(value));
                // 토글 횟수 추가
                int finalCounterDamage = baseCounterDamage + _turnToggleCount;
                DealDamageToEnemy(finalCounterDamage);
                break;
            case EffectType.Explode:
                yield return ApplyExplodeEffect(card);
                break;
            case EffectType.PopularityDamage:
                if (card.CurrentSlot != null)
                {
                    int neighborCount = 0;
                    foreach (CardDirection dir in Enum.GetValues(typeof(CardDirection)))
                    {
                        if (dir == CardDirection.None) continue;
                        GridSlot neighborSlot = GridManager.Instance.GetNeighbor(card.CurrentSlot, dir);
                        if (neighborSlot != null && !neighborSlot.IsEmpty)
                            neighborCount++;
                    }
                    // 토템 데미지가 합산되어 들어온 데미지
                    int basePopDamage = Mathf.Max(1, Mathf.RoundToInt(value));
                    // 그를 기반으로 한 영구 누적 데미지 합산
                    int modifiedPopDamage = runtime != null ?
                        runtime.GetModifiedDamage(basePopDamage) : basePopDamage;
                    // 주위 블럭들을 기반으로 한 총합 데미지
                    int popularityDamage = modifiedPopDamage * neighborCount;

                    if (popularityDamage > 0)
                        DealDamageToEnemy(popularityDamage);
                }
                break;
            case EffectType.Replay:
                // ActivateChainFrom에서 직접 처리 — 여기선 무시
                break;
            case EffectType.FinisherDamage:
                if (GridManager.Instance != null)
                {
                    int onCount = 0;
                    foreach (GridSlot s in GridManager.Instance.Slots.Values)
                        if (s.OccupiedCard != null && s.OccupiedCard.IsActivated)
                            onCount++;
                    // 기본 데미지
                    int baseFinisher = Mathf.RoundToInt(value);
                    // 런타임 값이 적용된 데미지
                    int modifiedFinisher = runtime != null ?
                        runtime.GetModifiedDamage(baseFinisher) : baseFinisher;
                    // 모든 버프가 더해진 데미지 * 켜진 횟수를 합산 후 적용
                    int finisherDamage = modifiedFinisher * onCount;

                    if (finisherDamage > 0)
                        DealDamageToEnemy(finisherDamage);
                }
                break;
            case EffectType.TotemAura:
                break;
            case EffectType.Devour:
                {
                    if (card.CurrentSlot == null) break;

                    int devourCount = 0;
                    List<CardView> targetsToDevour = new(); // 루프 도중 파괴로 인한 에러 방지용 리스트

                    // 범위 내의 먹잇감 스캔
                    foreach (CardDirection dir in card.Data.GetAllDirections())
                    {
                        GridSlot current = card.CurrentSlot;
                        for (int i = 0; i < card.Data.range; i++)
                        {
                            GridSlot neighbor = GridManager.Instance.GetNeighbor(current, dir);
                            if (neighbor == null) break;

                            CardView targetCard = neighbor.OccupiedCard;

                            // 적이 아니고, 빈 칸이 아니며, 아직 먹기로 예약되지 않은 아군/특수 기물이라면!
                            if (targetCard != null && !targetCard.IsEnemy && !targetsToDevour.Contains(targetCard))
                            {
                                targetsToDevour.Add(targetCard);
                            }
                            current = neighbor;
                        }
                    }

                    // 일괄 포식
                    foreach (CardView target in targetsToDevour)
                    {
                        devourCount++;
                        CardManager.Instance.ExileCard(target); // 알아서 토템 장판 등도 갱신해줌
                    }

                    // 먹은 개수만큼 스탯 상승 (이번 전투 내내 유지)
                    if (devourCount > 0)
                    {
                        int gainAmount = Mathf.Max(1, Mathf.RoundToInt(value)) * devourCount;
                        if (runtime != null)
                        {
                            runtime.AddBonusDamage(gainAmount); // 영구 공격력 증가
                        }
                    }
                    break;
                }
            case EffectType.CastingDamage:
            case EffectType.CastingDefense:
                break;
        }

        yield break;
    }

    private static int DealDamageToEnemy(int damage)
    {
        if (damage <= 0 || BattleManager.Instance == null)
            return 0;

        return BattleManager.Instance.DealDamageToEnemy(damage);
    }

    private IEnumerator ApplyDefenseOnOffEffects(CardView card)
    {
        if (card?.Data?.effects == null) yield break;

        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.effectType != EffectType.DefenseOnOff) continue;
            yield return ApplyEffectWithVisual(EffectType.Defense, effect.secondaryValue, card, EffectTrigger.OnActivated);
        }
    }

    // ── 배치/턴 효과 ──────────────────────────────────────

    private IEnumerator ApplyOnPlacedEffects(CardView card)
    {
        if (card?.Data?.effects == null || card.IsEnemy) yield break;

        var runtime = card.GetComponent<CardRuntimeState>();
        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.trigger != EffectTrigger.OnPlaced) continue;

            switch (effect.effectType)
            {
                case EffectType.InitDamage:
                    // 배치 시 초기값 세팅 — 누적이 아닌 덮어쓰기
                    if (runtime != null)
                        runtime.SetBonusDamage(Mathf.RoundToInt(effect.value));
                    break;
                default:
                    yield return ApplyEffectWithVisual(effect.effectType, effect.value, card, EffectTrigger.OnPlaced);
                    break;
            }
        }

        RefreshTotemAuras();
    }

    public IEnumerator ApplyTurnEndEffects()
    {
        if (GridManager.Instance == null) yield break;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            if (card == null || card.IsEnemy) continue;
            if (card.Data != null && card.Data.isCastingCard)
                card.GetComponent<CardRuntimeState>()?.InitializeCasting(card.Data.castingRequiredCount);
            yield return ApplyEffects(card, EffectTrigger.OnTurnEnd);
        }
    }

    public IEnumerator ApplyTurnStartEffects()
    {
        if (GridManager.Instance == null) yield break;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            if (card == null || card.IsEnemy) continue;
            if (!card.IsActivated) continue;
            yield return ApplyEffects(card, EffectTrigger.OnTurnStart);
        }
    }

    private void ApplyPreserveToNeighbors(CardView card, int amount)
    {
        if (card?.Data == null || card.CurrentSlot == null) return;

        foreach (CardDirection dir in card.Data.GetAllDirections())
        {
            GridSlot neighbor = GridManager.Instance.GetNeighbor(card.CurrentSlot, dir);
            if (neighbor != null && neighbor.OccupiedCard != null && !neighbor.OccupiedCard.IsEnemy)
                neighbor.OccupiedCard.AddPreserve(amount);
        }
    }

    // ── 카운트 집계 ───────────────────────────────────────

    private bool IsFullActivated(CountScope scope, CardView card)
    {
        if (card?.CurrentSlot == null) return false;
        Vector2Int pos = card.CurrentSlot.Position;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            bool inScope = scope switch
            {
                CountScope.Row => slot.Position.y == pos.y,
                CountScope.Column => slot.Position.x == pos.x,
                CountScope.Cross => slot.Position.y == pos.y || slot.Position.x == pos.x,
                CountScope.Total => true,
                _ => false
            };

            if (inScope && (slot.IsEmpty || !slot.OccupiedCard.IsActivated))
                return false;
        }
        return true;
    }

    private int CountByScope(CountScope scope, CardView card)
    {
        if (card?.CurrentSlot == null) return 0;
        Vector2Int pos = card.CurrentSlot.Position;

        switch (scope)
        {
            case CountScope.Row:
                int rowCount = 0;
                foreach (GridSlot slot in GridManager.Instance.Slots.Values)
                    if (slot.OccupiedCard != null && slot.OccupiedCard.IsActivated
                        && slot.Position.y == pos.y)
                        rowCount++;
                return rowCount;

            case CountScope.Column:
                int colCount = 0;
                foreach (GridSlot slot in GridManager.Instance.Slots.Values)
                    if (slot.OccupiedCard != null && slot.OccupiedCard.IsActivated
                        && slot.Position.x == pos.x)
                        colCount++;
                return colCount;

            case CountScope.Cross:
                int crossCount = 0;
                foreach (GridSlot slot in GridManager.Instance.Slots.Values)
                    if (slot.OccupiedCard != null && slot.OccupiedCard.IsActivated
                        && (slot.Position.y == pos.y || slot.Position.x == pos.x))
                        crossCount++;
                return crossCount;

            case CountScope.Total:
                int total = 0;
                foreach (GridSlot slot in GridManager.Instance.Slots.Values)
                    if (slot.OccupiedCard != null && slot.OccupiedCard.IsActivated)
                        total++;
                return total;

            case CountScope.Self:
                return card.Instance?.PersistentState.TurnOnCount ?? 0;

            case CountScope.GridTotal:
                return _turnToggleCount;

            default:
                return 0;
        }
    }
}