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

        var atLeastBest = new Dictionary<Type, CardEffect>();

        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.trigger != trigger) continue;
            CardEffectBase resolvedEffect = effect.ResolvedEffect;
            if (resolvedEffect == null) continue;

            if (effect.thresholdType == ThresholdType.Full)
            {
                if (IsFullActivated(effect.scope, card))
                    yield return ApplyEffectWithVisual(effect, card, trigger);
                continue;
            }

            if (effect.scope == CountScope.None)
            {
                yield return ApplyEffectWithVisual(effect, card, trigger);
                continue;
            }

            int count = CountByScope(effect.scope, card);
            if (count < effect.threshold) continue;

            Type effectKey = resolvedEffect.GetType();
            if (!atLeastBest.TryGetValue(effectKey, out CardEffect best) ||
                effect.threshold > best.threshold)
            {
                atLeastBest[effectKey] = effect;
            }
        }

        foreach (var kv in atLeastBest)
            yield return ApplyEffectWithVisual(kv.Value, card, trigger);
    }

    private IEnumerator ApplyEffectWithVisual(CardEffect entry, CardView card, EffectTrigger trigger)
    {
        CardEffectBase resolvedEffect = entry.ResolvedEffect;
        if (resolvedEffect == null) yield break;

        float resolvedValue = GetTotemAdjustedValue(card, resolvedEffect, entry.value);

        if (resolvedEffect.IsInitDamage)
        {
            yield return RunEffect(entry, resolvedValue, card, trigger);
            yield break;
        }

        if (_cardEffectPlaySystem != null && _cardEffectPlaySystem.HasAssignedVisual(card, resolvedEffect))
        {
            bool scheduledOnImpact = _cardEffectPlaySystem.PlayAssignedEffectDetached(
                card,
                resolvedEffect,
                ToQueuedEffectTiming(trigger),
                () => StartCoroutine(RunEffect(entry, resolvedValue, card, trigger)),
                value: resolvedValue);

            if (scheduledOnImpact)
                yield break;
        }

        yield return RunEffect(entry, resolvedValue, card, trigger);
    }

    // 효과 SO 실행 — 컨텍스트를 세팅하고 다형성으로 위임
    private IEnumerator RunEffect(CardEffect entry, float resolvedValue, CardView card, EffectTrigger trigger)
    {
        CardEffectBase resolvedEffect = entry.ResolvedEffect;
        if (resolvedEffect == null) yield break;
        EffectCtx.Setup(card, resolvedValue, entry.secondaryValue, entry.scope, trigger, _activatedCards);
        yield return resolvedEffect.Apply(EffectCtx);
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

    private static int DealDamageToEnemy(int damage)
    {
        if (damage <= 0 || BattleManager.Instance == null)
            return 0;

        return BattleManager.Instance.DealDamageToEnemy(damage);
    }

    // EffectContext용 public 위임 래퍼
    public void DealDamageToEnemyPublic(int damage) => DealDamageToEnemy(damage);
    public void ApplyPreserveToNeighborsPublic(CardView card, int amount) => ApplyPreserveToNeighbors(card, amount);

    private IEnumerator ApplyDefenseOnOffEffects(CardView card)
    {
        if (card?.Data?.effects == null) yield break;

        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.ResolvedEffect == null || !effect.ResolvedEffect.IsDefenseOnOff) continue;
            // OFF 전환 시: secondaryValue만큼 방어 부여
            int defense = Mathf.Max(1, Mathf.RoundToInt(effect.secondaryValue));
            if (BattleManager.Instance != null)
                BattleManager.Instance.Player.AddDefense(defense);
        }
        yield break;
    }

    // ── 배치/턴 효과 ──────────────────────────────────────

    private IEnumerator ApplyOnPlacedEffects(CardView card)
    {
        if (card?.Data?.effects == null || card.IsEnemy) yield break;

        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.trigger != EffectTrigger.OnPlaced) continue;
            // InitDamage 등 배치 효과는 각 효과 SO가 처리 (InitDamageEffect가 SetBonusDamage 수행)
            yield return ApplyEffectWithVisual(effect, card, EffectTrigger.OnPlaced);
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
