using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainExecutor : SingletonBehaviour<ChainExecutor>
{
    [Header("Chain Settings")]
    [SerializeField] private int _maxActivationSteps = 2048;
    [SerializeField] private float _cardFeedbackDuration = 0.22f;

    public event Action OnChainStarted;
    public event Action<ChainResult> OnChainFinished;
    public event Action<ChainResult> OnStatsUpdated;

    private readonly HashSet<CardView> _activatedCards = new();
    public IReadOnlyCollection<CardView> ActivatedCards => _activatedCards;

    // 누적 결과
    private ChainResult _accumulatedResult = new();

    public void Init()
    {
        Debug.Log("[ChainExecutor] Init");
    }

    public void ResetAccumulatedResult()
    {
        _accumulatedResult = new ChainResult();
        OnStatsUpdated?.Invoke(_accumulatedResult);
    }

    public void CancelCurrentChain()
    {
        StopAllCoroutines();
    }

    public void ExecuteFrom(CardView rootCard)
    {
        StartCoroutine(ExecuteChain(rootCard));
    }

    private IEnumerator ExecuteChain(CardView rootCard)
    {
        if (rootCard == null) yield break;

        OnChainStarted?.Invoke();
        _activatedCards.Clear();

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null)
                slot.OccupiedCard.SetDraggable(false);

        yield return ActivateChainFrom(rootCard, _activatedCards);

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null)
                slot.OccupiedCard.SetDraggable(false);

        OnChainFinished?.Invoke(_accumulatedResult);
    }

    // ── 효과 누적 ──────────────────────────────────────────

    private void ApplyEffectsImmediately(CardView card)
    {
        if (card?.Data?.effects == null) return;

        // AtLeast: EffectType별로 가장 높은 threshold를 만족하는 것 하나만 적용
        // Full: 조건 만족 시 적용
        var atLeastBest = new Dictionary<EffectType, (int threshold, float value)>();

        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.thresholdType == ThresholdType.Full)
            {
                if (IsFullActivated(effect.scope, card))
                    ApplyEffect(effect.effectType, effect.value);
                continue;
            }

            // AtLeast
            if (effect.scope == CountScope.None)
            {
                ApplyEffect(effect.effectType, effect.value);
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
            ApplyEffect(kv.Key, kv.Value.value);
    }

    private void ApplyEffect(EffectType type, float value)
    {
        if (BattleManager.Instance == null) return;
        BattleManager.Instance.ApplyImmediateEffect(type, value);
    }

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

            default:
                return 0;
        }
    }

    private IEnumerator ActivateChainFrom(CardView root, HashSet<CardView> activatedCards)
    {
        List<CardView> currentWave = new() { root };
        int step = 0;

        while (currentWave.Count > 0)
        {
            List<CardView> emitters = new();

            foreach (CardView current in currentWave)
            {
                if (current == null || current.CurrentSlot == null) continue;

                bool nextState = !current.IsActivated;
                current.SetActivated(nextState);

                step++;
                if (step > _maxActivationSteps)
                {
                    Debug.LogWarning("[ChainExecutor] 안전 한도 초과로 체인 중단.");
                    yield break;
                }

                if (nextState)
                {
                    emitters.Add(current);
                    activatedCards.Add(current);

                    // On될 때 그 시점 기준으로 효과 누적
                    if (!current.IsEnemy)
                        ApplyEffectsImmediately(current);
                }
                else
                {
                    activatedCards.Remove(current);
                    // Off돼도 누적값 차감 없음
                }
            }

            foreach (CardView current in currentWave)
                if (current != null)
                    StartCoroutine(current.PlayActivationFeedback(_cardFeedbackDuration));

            yield return new WaitForSeconds(_cardFeedbackDuration);

            HashSet<CardView> nextWaveSet = new();
            foreach (CardView emitter in emitters)
            {
                if (emitter.Data == null) continue;

                foreach (CardDirection dir in emitter.Data.GetAllDirections())
                {
                    GridSlot neighbor = GridManager.Instance.GetNeighbor(emitter.CurrentSlot, dir);
                    if (neighbor != null && neighbor.OccupiedCard != null)
                        nextWaveSet.Add(neighbor.OccupiedCard);
                }
            }

            currentWave = new List<CardView>(nextWaveSet);
        }
    }

    protected override void Dispose()
    {
        OnChainStarted = null;
        OnChainFinished = null;
        OnStatsUpdated = null;
        base.Dispose();
    }
}

public class ChainResult
{
    public float damage;
    public float defense;
    public float heal;
}
