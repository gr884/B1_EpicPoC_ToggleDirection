using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainExecutor : SingletonBehaviour<ChainExecutor>
{
    [Header("Chain Settings")]
    [SerializeField] private int _maxActivationSteps = 2048;
    [SerializeField] private float _cardFeedbackDuration = 0.22f;
    [SerializeField] private float _effectApplyDelay = 0.5f;

    public event Action OnChainStarted;
    public event Action<ChainResult> OnChainFinished;
    public event Action<ChainResult> OnStatsUpdated;
    public event Action<IReadOnlyList<CardView>> OnActivationOrderResolved;
    public event Action<int> OnEffectStepProcessed;

    public IReadOnlyCollection<CardView> ActivatedCards => _activatedCards;
    private readonly HashSet<CardView> _activatedCards = new();

    public IReadOnlyList<CardView> LastActivationOrder => _lastActivationOrder;
    private List<CardView> _lastActivationOrder = new();

    // 턴 중 쌓인 효과 큐 (END 버튼 시 순차 실행)
    private readonly List<List<CardView>>                  _pendingOrders  = new();
    private readonly List<Dictionary<CardView, BuffBonus>> _pendingBonuses = new();

    // 배율 카드가 설정한 다음 Active 카드 효과 배율
    private float _pendingMultiplier = 1f;

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

        List<CardView> activationOrder = new();
        Dictionary<CardView, HashSet<CardView>> successorMap = new();

        yield return ActivateChainFrom(rootCard, activationOrder, successorMap);

        _lastActivationOrder = activationOrder;

        // 즉시 발동 / 지연 발동 분리
        List<CardView> immediateOrder = new();
        List<CardView> deferredOrder  = new();
        foreach (CardView card in activationOrder)
        {
            // Draw cards are always immediate regardless of triggerTiming setting
            bool isImmediate = card.Data?.triggerTiming == TriggerTiming.Immediate
                            || card.Data?.cardType == CardType.Draw;
            if (isImmediate)
                immediateOrder.Add(card);
            else
                deferredOrder.Add(card);
        }

        Dictionary<CardView, BuffBonus> buffBonuses = ResolveBuffBonuses(activationOrder, successorMap);

        // 즉시 발동 효과 실행 (Draw 등 — UI 큐 미표시)
        if (immediateOrder.Count > 0)
        {
            ChainResult immediateResult = new ChainResult();
            yield return ExecuteEffectsSequentially(immediateOrder, new Dictionary<CardView, BuffBonus>(), immediateResult);
        }

        // 지연 발동 효과 큐에 저장 → END 버튼 시 실행
        if (deferredOrder.Count > 0)
        {
            _pendingOrders.Add(deferredOrder);
            _pendingBonuses.Add(buffBonuses);
        }

        // UI: 지연 카드만 큐에 표시
        OnActivationOrderResolved?.Invoke(deferredOrder);
        OnChainFinished?.Invoke(new ChainResult());
    }

    // END 버튼 시 BattleManager가 호출 — 쌓인 효과 전체를 순차 실행
    public IEnumerator ExecutePendingEffects(ChainResult result)
    {
        List<List<CardView>>                  orders  = new(_pendingOrders);
        List<Dictionary<CardView, BuffBonus>> bonuses = new(_pendingBonuses);
        _pendingOrders.Clear();
        _pendingBonuses.Clear();

        for (int i = 0; i < orders.Count; i++)
            yield return ExecuteEffectsSequentially(orders[i], bonuses[i], result);
    }

    public void ClearPendingEffects()
    {
        _pendingOrders.Clear();
        _pendingBonuses.Clear();
        _pendingMultiplier = 1f;
    }

    // ── 1단계: 체인 전파 ──────────────────────────────────────

    private IEnumerator ActivateChainFrom(CardView root, List<CardView> activationOrder,
        Dictionary<CardView, HashSet<CardView>> successorMap)
    {
        // 루트(배치 카드): 자신의 효과 즉시 발동 + 화살표 이웃으로 신호 전파
        if (root.Data != null && root.CurrentSlot != null)
        {
            _activatedCards.Add(root);
            activationOrder.Add(root);
            StartCoroutine(root.PlayActivationFeedback(_cardFeedbackDuration));
        }

        HashSet<CardView> firstWave = new();
        if (root.Data != null && root.CurrentSlot != null)
        {
            root.ReduceDurability();

            if (root.CanPropagate)
            {
                foreach (CardDirection dir in root.GetRuntimeDirections())
                {
                    GridSlot neighbor = GridManager.Instance.GetNeighbor(root.CurrentSlot, dir);
                    CardView neighborCard = neighbor?.OccupiedCard;
                    if (neighborCard == null) continue;

                    firstWave.Add(neighborCard);

                    if (!successorMap.TryGetValue(root, out HashSet<CardView> rootSuccs))
                    {
                        rootSuccs = new HashSet<CardView>();
                        successorMap[root] = rootSuccs;
                    }
                    rootSuccs.Add(neighborCard);
                }
            }
        }

        List<CardView> currentWave = new(firstWave);
        int step = 0;

        while (currentWave.Count > 0)
        {
            List<CardView> propagators = new();

            List<CardView> feedbackCards = new();

            foreach (CardView current in currentWave)
            {
                if (current == null || current.CurrentSlot == null) continue;

                bool wasInactive = !current.IsActivated;
                current.SetActivated(!current.IsActivated); // 상태 토글

                feedbackCards.Add(current); // 방향 무관하게 피드백 재생

                if (wasInactive)
                {
                    // 비활성 → 활성: 효과 발동
                    _activatedCards.Add(current);
                    activationOrder.Add(current);
                    current.ReduceDurability();
                    // 내구도 남아있을 때만 신호 전파
                    if (current.CanPropagate)
                        propagators.Add(current);
                }
                // 활성 → 비활성: 효과 없음, 전파 없음

                step++;
                if (step > _maxActivationSteps)
                {
                    Debug.LogWarning("[ChainExecutor] 안전 한도 초과로 체인 중단.");
                    yield break;
                }
            }

            foreach (CardView p in feedbackCards)
                StartCoroutine(p.PlayActivationFeedback(_cardFeedbackDuration));

            yield return new WaitForSeconds(_cardFeedbackDuration);

            HashSet<CardView> nextWaveSet = new();
            foreach (CardView p in propagators)
            {
                if (p.Data == null) continue;
                foreach (CardDirection dir in p.GetRuntimeDirections())
                {
                    GridSlot neighbor = GridManager.Instance.GetNeighbor(p.CurrentSlot, dir);
                    CardView neighborCard = neighbor?.OccupiedCard;
                    if (neighborCard == null) continue;

                    nextWaveSet.Add(neighborCard);

                    if (!successorMap.TryGetValue(p, out HashSet<CardView> succs))
                    {
                        succs = new HashSet<CardView>();
                        successorMap[p] = succs;
                    }
                    succs.Add(neighborCard);
                }
            }

            currentWave = new List<CardView>(nextWaveSet);
        }
    }

    // ── 버프 해소: 각 Buff 카드의 최종 Active 타겟 결정 ────────

    private Dictionary<CardView, BuffBonus> ResolveBuffBonuses(
        List<CardView> activationOrder,
        Dictionary<CardView, HashSet<CardView>> successorMap)
    {
        Dictionary<CardView, BuffBonus> result = new();

        foreach (CardView card in activationOrder)
        {
            if (card.Data?.cardType != CardType.Buff) continue;

            // Find terminal Active cards reachable via this buff card's arrows
            List<CardView> targets = FindActiveTargets(card, successorMap);
            foreach (CardView target in targets)
            {
                if (!result.ContainsKey(target))
                    result[target] = new BuffBonus();

                foreach (CardEffect effect in card.Data.effects)
                {
                    float val = EvaluateEffect(effect, card);
                    switch (effect.effectType)
                    {
                        case EffectType.Damage: result[target].damage += val; break;
                        case EffectType.Defense: result[target].defense += val; break;
                        case EffectType.Heal: result[target].heal += val; break;
                    }
                }
            }
        }

        return result;
    }

    // 버프 카드에서 도달 가능한 모든 Active 카드 반환 (Active 타입만)
    private List<CardView> FindActiveTargets(CardView buffCard,
        Dictionary<CardView, HashSet<CardView>> successorMap)
    {
        if (!successorMap.TryGetValue(buffCard, out HashSet<CardView> directSuccs))
            return new List<CardView>();

        HashSet<CardView> reachable = new();
        Queue<CardView> queue = new();
        foreach (CardView s in directSuccs) queue.Enqueue(s);

        while (queue.Count > 0)
        {
            CardView current = queue.Dequeue();
            if (!reachable.Add(current)) continue;
            if (successorMap.TryGetValue(current, out HashSet<CardView> nexts))
                foreach (CardView n in nexts) queue.Enqueue(n);
        }

        List<CardView> targets = new();
        foreach (CardView candidate in reachable)
            if (candidate.Data?.cardType == CardType.Active)
                targets.Add(candidate);

        return targets;
    }

    // ── 2단계: 효과 순차 발동 ─────────────────────────────────

    private IEnumerator ExecuteEffectsSequentially(
        List<CardView> activationOrder,
        Dictionary<CardView, BuffBonus> buffBonuses,
        ChainResult result)
    {
        HashSet<CardView> buffApplied = new();
        int stepIndex = 0;

        foreach (CardView card in activationOrder)
        {
            yield return new WaitForSeconds(_effectApplyDelay);

            if (card.Data == null)
            {
                OnEffectStepProcessed?.Invoke(stepIndex++);
                continue;
            }

            CardType type = card.Data.cardType;

            // ── Buff: 자체 효과 없음 (이미 Active 카드에 반영됨) ──
            if (type == CardType.Buff)
            {
                OnEffectStepProcessed?.Invoke(stepIndex++);
                continue;
            }

            // ── Draw: 덱에서 N장 드로우 ───────────────────────────
            if (type == CardType.Draw)
            {
                int drawCount = card.Data.effects.Count > 0
                    ? Mathf.Max(1, (int)card.Data.effects[0].value)
                    : 1;
                CardManager.Instance.DrawToHand(drawCount, ignoreHandLimit: true);
                OnEffectStepProcessed?.Invoke(stepIndex++);
                continue;
            }

            // ── Multiplier: 다음 Active 효과에 배율 적용 ──────────
            if (type == CardType.Multiplier)
            {
                float mult = card.Data.effects.Count > 0 ? card.Data.effects[0].value : 2f;
                _pendingMultiplier *= mult;
                OnEffectStepProcessed?.Invoke(stepIndex++);
                continue;
            }

            // ── Active: 자신의 효과 발동 (배율 소비) ─────────────
            float effectMult = _pendingMultiplier;
            _pendingMultiplier = 1f;

            foreach (CardEffect effect in card.Data.effects)
            {
                float value = EvaluateEffect(effect, card) * effectMult;
                if (value > 0f)
                {
                    switch (effect.effectType)
                    {
                        case EffectType.Damage:  result.damage  += value; break;
                        case EffectType.Defense: result.defense += value; break;
                        case EffectType.Heal:    result.heal    += value; break;
                    }
                }
            }

            if (!buffApplied.Contains(card) && buffBonuses.TryGetValue(card, out BuffBonus bonus))
            {
                result.damage  += bonus.damage  * effectMult;
                result.defense += bonus.defense * effectMult;
                result.heal    += bonus.heal    * effectMult;
                buffApplied.Add(card);
            }

            OnStatsUpdated?.Invoke(result);
            OnEffectStepProcessed?.Invoke(stepIndex++);
        }
    }

    // ── 효과 평가 ─────────────────────────────────────────────

    private float EvaluateEffect(CardEffect effect, CardView card)
    {
        if (effect.scope == CountScope.None)
            return effect.value;

        int count = CountByScope(effect.scope, card);
        return count >= effect.threshold ? effect.value : 0f;
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
                    if (slot.OccupiedCard != null && slot.OccupiedCard.IsActivated && slot.Position.y == pos.y)
                        rowCount++;
                return rowCount;

            case CountScope.Column:
                int colCount = 0;
                foreach (GridSlot slot in GridManager.Instance.Slots.Values)
                    if (slot.OccupiedCard != null && slot.OccupiedCard.IsActivated && slot.Position.x == pos.x)
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

            default: return 0;
        }
    }

    protected override void Dispose()
    {
        OnChainStarted = null;
        OnChainFinished = null;
        OnStatsUpdated = null;
        OnActivationOrderResolved = null;
        OnEffectStepProcessed = null;
        _pendingOrders.Clear();
        _pendingBonuses.Clear();
        base.Dispose();
    }
}

public class ChainResult
{
    public float damage;
    public float defense;
    public float heal;
}

public class BuffBonus
{
    public float damage;
    public float defense;
    public float heal;
}
