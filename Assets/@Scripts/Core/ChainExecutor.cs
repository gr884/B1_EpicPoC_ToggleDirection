using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainExecutor : SingletonBehaviour<ChainExecutor>
{
    [Header("Chain Settings")]
    [SerializeField] private int _maxActivationSteps = 2048;
    [SerializeField] private float _cardFeedbackDuration = 0.22f;
    [SerializeField] private int _maxLoopCount = 3;

    public event Action OnChainStarted;
    public event Action OnChainFinished;

    private readonly HashSet<CardView> _activatedCards = new();
    public IReadOnlyCollection<CardView> ActivatedCards => _activatedCards;

    public void Init()
    {
        Debug.Log("[ChainExecutor] Init");
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

        // 그리드 카드 드래그 차단
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null)
                slot.OccupiedCard.SetDraggable(false);

        // 손패 카드 드래그 차단
        foreach (CardView card in CardManager.Instance.Hand)
            if (card != null) card.SetDraggable(false);

        yield return ActivateChainFrom(rootCard, _activatedCards);

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null)
                slot.OccupiedCard.SetDraggable(false);

        OnChainFinished?.Invoke();
    }

    // ── 효과 처리 ──────────────────────────────────────────

    private int ApplyEffects(CardView card, bool deferDamage = false)
    {
        int deferredDamage = 0;
        if (card?.Data?.effects == null) return deferredDamage;

        var atLeastBest = new Dictionary<EffectType, (int threshold, float value)>();

        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.thresholdType == ThresholdType.Full)
            {
                if (IsFullActivated(effect.scope, card))
                    ApplyEffect(effect.effectType, effect.value, card, deferDamage, ref deferredDamage);
                continue;
            }

            if (effect.scope == CountScope.None)
            {
                ApplyEffect(effect.effectType, effect.value, card, deferDamage, ref deferredDamage);
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
            ApplyEffect(kv.Key, kv.Value.value, card, deferDamage, ref deferredDamage);

        return deferredDamage;
    }

    private void ApplyEffect(EffectType type, float value, CardView card, bool deferDamage, ref int deferredDamage)
    {
        switch (type)
        {
            case EffectType.Damage:
                int damage = Mathf.Max(1, Mathf.RoundToInt(value));
                if (deferDamage)
                    deferredDamage += damage;
                else
                    BattleManager.Instance.DealDamageToEnemy(damage);
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

                    foreach (var e in targetCard.Data.effects)
                    {
                        if (e.effectType == EffectType.Damage)
                            totalDamage += (int)e.value;
                    }
                }
                BattleManager.Instance.DealDamageToEnemy(totalDamage);
                break;
            case EffectType.Draw:
                int drawCount = Mathf.Max(1, Mathf.RoundToInt(value));
                CardManager.Instance.DrawToHand(drawCount);
                break;
            case EffectType.GainCost:
                int gainAmount = Mathf.Max(1, Mathf.RoundToInt(value));
                BattleManager.Instance.Player.GainCost(gainAmount);
                break;
            case EffectType.Preserve:
                int preserveAmount = Mathf.Max(1, Mathf.RoundToInt(value));
                ApplyPreserveToNeighbors(card, preserveAmount);
                break;
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

        List<HashSet<CardView>> waveHistory = new();
        int loopCount = 0;

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

                    if (!current.IsEnemy)
                    {
                        bool hasMotionRequest = TryCreateMotionRequest(current, out CharacterMotionRequest motionRequest);
                        bool deferDamage = hasMotionRequest
                            && motionRequest.MotionType == CharacterMotionType.Attack
                            && CharacterMotionEvents.HasCardMotionListeners;
                        int deferredDamage = ApplyEffects(current, deferDamage);

                        if (hasMotionRequest)
                        {
                            if (deferDamage)
                                motionRequest = motionRequest.WithDamageAmount(deferredDamage);

                            CharacterMotionEvents.RequestCardMotion(motionRequest);
                        }
                    }

                    if (BattleManager.Instance.Enemy.IsDead)
                    {
                        Debug.Log("[ChainExecutor] 적 사망 — 체인 중단");
                        yield break;
                    }
                }
                else
                {
                    activatedCards.Remove(current);
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
                    GridSlot current = emitter.CurrentSlot;
                    for (int i = 0; i < emitter.Data.range; i++)
                    {
                        GridSlot neighbor = GridManager.Instance.GetNeighbor(current, dir);
                        if (neighbor == null) break;

                        if (neighbor.OccupiedCard != null)
                            nextWaveSet.Add(neighbor.OccupiedCard);

                        current = neighbor;
                    }
                }
            }

            if (nextWaveSet.Count > 0)
            {
                foreach (HashSet<CardView> pastWave in waveHistory)
                {
                    if (pastWave.SetEquals(nextWaveSet))
                    {
                        loopCount++;
                        Debug.Log($"[ChainExecutor] 루프 감지 — {loopCount}/{_maxLoopCount}");

                        if (loopCount >= _maxLoopCount)
                        {
                            Debug.Log("[ChainExecutor] 최대 루프 횟수 도달 — 체인 중단");
                            OnInfiniteLoopDetected();
                            yield break;
                        }

                        waveHistory.Clear();
                        break;
                    }
                }

                waveHistory.Add(nextWaveSet);
            }

            currentWave = new List<CardView>(nextWaveSet);
        }
    }

    private bool TryCreateMotionRequest(CardView card, out CharacterMotionRequest request)
    {
        request = default;
        if (card?.Data?.effects == null || card.CurrentSlot == null) return false;

        if (ContainsEffect(card.Data, EffectType.Damage))
        {
            request = new CharacterMotionRequest(
                CharacterMotionType.Attack,
                card,
                card.Data,
                EffectType.Damage,
                card.CurrentSlot.Position);
            return true;
        }

        if (ContainsEffect(card.Data, EffectType.Defense))
        {
            request = new CharacterMotionRequest(
                CharacterMotionType.Defend,
                card,
                card.Data,
                EffectType.Defense,
                card.CurrentSlot.Position);
            return true;
        }

        if (ContainsEffect(card.Data, EffectType.Preserve))
        {
            request = new CharacterMotionRequest(
                CharacterMotionType.Defend,
                card,
                card.Data,
                EffectType.Preserve,
                card.CurrentSlot.Position);
            return true;
        }

        return false;
    }

    private bool ContainsEffect(CardData data, EffectType effectType)
    {
        if (data?.effects == null) return false;

        foreach (CardEffect effect in data.effects)
            if (effect.effectType == effectType)
                return true;

        return false;
    }

    /// <summary>무한 루프가 감지됐을 때 호출됩니다. 처리 방식은 추후 결정.</summary>
    private void OnInfiniteLoopDetected()
    {
        // TODO: 무한 루프 처리 구현
    }

    protected override void Dispose()
    {
        OnChainStarted = null;
        OnChainFinished = null;
        base.Dispose();
    }
}