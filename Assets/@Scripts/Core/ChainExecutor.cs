using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ChainExecutor : SingletonBehaviour<ChainExecutor>
{
    [Header("Chain Settings")]
    [SerializeField] private int _maxActivationSteps = 2048;
    [SerializeField] private float _cardFeedbackDuration = 0.22f;
    [SerializeField] private int _maxLoopCount = 3;

    [Header("Action Block Flight Effect")]
    [SerializeField] private ActionBlockFlightEffectPlayer _actionBlockFlightEffectPlayer;

    [Header("Infinite Loop Finish")]
    [SerializeField] private TMP_Text _infiniteLoopText;
    [SerializeField] private BattleCinematicSlashDirector _loopSlashDirector;
    [SerializeField] private float _infiniteLoopTextDuration = 1.2f;
    [SerializeField] private int _infiniteLoopFinishDamage = 999;

    public event Action OnChainStarted;
    public event Action OnChainFinished;

    private readonly HashSet<CardView> _activatedCards = new();
    public IReadOnlyCollection<CardView> ActivatedCards => _activatedCards;

    // 카운터형: 이번 턴 그리드 전체 ON 횟수
    private int _turnToggleCount;
    public int TurnToggleCount => _turnToggleCount;
    private int _runningDetachedChainCount;

    public event Action OnToggleCountChanged;

    private sealed class ChainLoopContext
    {
        private readonly Dictionary<string, int> _stateRepeatCounts = new();

        public int StepCount { get; private set; }
        public bool InfiniteLoopDetected { get; private set; }
        public string DetectionReason { get; private set; }

        public bool TryAdvanceStep(int maxSteps, string source)
        {
            StepCount++;
            if (StepCount <= maxSteps) return true;

            MarkInfiniteLoop($"안전 한도 초과 ({source}, {StepCount}/{maxSteps})");
            return false;
        }

        public bool TryRecordState(string stateKey, int maxLoopCount, string source)
        {
            if (InfiniteLoopDetected) return false;

            if (!_stateRepeatCounts.TryGetValue(stateKey, out int repeatCount))
            {
                _stateRepeatCounts[stateKey] = 0;
                return true;
            }

            repeatCount++;
            _stateRepeatCounts[stateKey] = repeatCount;
            Debug.Log($"[ChainExecutor] 루프 후보 감지 — {source} {repeatCount}/{maxLoopCount}");

            if (repeatCount < maxLoopCount) return true;

            MarkInfiniteLoop($"반복 상태 감지 ({source})");
            return false;
        }

        public void MarkInfiniteLoop(string reason)
        {
            if (InfiniteLoopDetected) return;

            InfiniteLoopDetected = true;
            DetectionReason = reason;
        }
    }

    public void ResetTurnToggleCount()
    {
        _turnToggleCount = 0;
        OnToggleCountChanged?.Invoke();
    }

    public bool IsExecuting { get; private set; }

    public void Init()
    {
        Debug.Log("[ChainExecutor] Init");
    }

    public void ExecuteFrom(CardView rootCard)
    {
        if (rootCard == null || IsExecuting) return;

        IsExecuting = true;
        StartCoroutine(ExecuteChain(rootCard));
    }

    private IEnumerator ExecuteChain(CardView rootCard)
    {
        if (rootCard == null)
        {
            IsExecuting = false;
            yield break;
        }

        ChainLoopContext loopContext = new();
        _runningDetachedChainCount = 0;

        OnChainStarted?.Invoke();
        _activatedCards.Clear();

        SetAllCardsDraggable(false);

        yield return ActivateChainFrom(rootCard, _activatedCards, loopContext);

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null)
                slot.OccupiedCard.SetDraggable(false);

        while (_runningDetachedChainCount > 0)
            yield return null;

        if (!loopContext.InfiniteLoopDetected)
        {
            // 자동 트리거: 체인 종료 후 조건 충족 카드 자동 ON
            yield return CheckAutoTriggers(loopContext);
        }

        while (_runningDetachedChainCount > 0)
            yield return null;

        if (loopContext.InfiniteLoopDetected && !IsEnemyDead())
        {
            OnInfiniteLoopDetected(loopContext.DetectionReason);
            ClearPlayerMotionQueues();
            yield return ExecuteInfiniteLoopFinishRoutine();
        }

        IsExecuting = false;
        OnChainFinished?.Invoke();
    }

    // ── 효과 처리 ──────────────────────────────────────────

    private HashSet<EffectType> ApplyEffects(CardView card, EffectTrigger trigger = EffectTrigger.OnActivated, ChainLoopContext loopContext = null)
    {
        HashSet<EffectType> appliedTypes = new();
        if (card?.Data?.effects == null) return appliedTypes;

        var atLeastBest = new Dictionary<EffectType, (int threshold, float value)>();

        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.trigger != trigger) continue;

            if (effect.thresholdType == ThresholdType.Full)
            {
                if (IsFullActivated(effect.scope, card))
                    ApplyEffectAndRecord(effect.effectType, effect.value, card, loopContext, appliedTypes);
                continue;
            }

            if (effect.scope == CountScope.None)
            {
                ApplyEffectAndRecord(effect.effectType, effect.value, card, loopContext, appliedTypes);
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
            ApplyEffectAndRecord(kv.Key, kv.Value.value, card, loopContext, appliedTypes);

        return appliedTypes;
    }

    private void ApplyEffectAndRecord(
        EffectType type,
        float value,
        CardView card,
        ChainLoopContext loopContext,
        HashSet<EffectType> appliedTypes)
    {
        ApplyEffect(type, value, card, loopContext);
        appliedTypes?.Add(type);
    }

    private void ApplyEffect(EffectType type, float value, CardView card, ChainLoopContext loopContext = null)
    {
        var runtime = card != null ? card.GetComponent<CardRuntimeState>() : null;
        switch (type)
        {
            case EffectType.Damage:
                int baseDamage = Mathf.Max(1, Mathf.RoundToInt(value));
                int damage = runtime != null ? runtime.GetModifiedDamage(baseDamage) : baseDamage;
                BattleManager.Instance.Player.AddPendingAttack(damage);
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
                        if (e.effectType != EffectType.Damage) continue;
                        int targetBaseDamage = Mathf.Max(1, Mathf.RoundToInt(e.value));
                        int finalDamage = targetRuntime != null
                            ? targetRuntime.GetModifiedDamage(targetBaseDamage)
                            : targetBaseDamage;
                        totalDamage += finalDamage;
                    }
                }
                BattleManager.Instance.Player.AddPendingAttack(totalDamage);
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
            case EffectType.GainDamage:
                if (runtime != null)
                    runtime.AddBonusDamage(Mathf.RoundToInt(value));
                break;
            case EffectType.DecayDamage:
                if (runtime != null)
                    runtime.DeductBonusDamage(Mathf.RoundToInt(value));
                break;
            case EffectType.DefenseOnOff:
                int reverseDamage = Mathf.Max(1, Mathf.RoundToInt(value));
                BattleManager.Instance.Player.AddPendingAttack(reverseDamage);
                break;
            case EffectType.CounterDamage:
                int counterDamage = Mathf.Max(1, Mathf.RoundToInt(value) + _turnToggleCount);
                BattleManager.Instance.Player.AddPendingAttack(counterDamage);
                break;
            case EffectType.Explode:
                ApplyExplodeEffect(card, loopContext);
                break;
            case EffectType.PopularityDamage:
                if (card.CurrentSlot != null)
                {
                    int neighborCount = 0;
                    foreach (CardDirection dir in System.Enum.GetValues(typeof(CardDirection)))
                    {
                        if (dir == CardDirection.None) continue;
                        GridSlot neighborSlot = GridManager.Instance.GetNeighbor(card.CurrentSlot, dir);
                        if (neighborSlot != null && !neighborSlot.IsEmpty)
                            neighborCount++;
                    }
                    int popularityDamage = Mathf.RoundToInt(value) * neighborCount;
                    if (popularityDamage > 0)
                        BattleManager.Instance.Player.AddPendingAttack(popularityDamage);
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
                    int finisherDamage = Mathf.RoundToInt(value) * onCount;
                    if (finisherDamage > 0)
                        BattleManager.Instance.Player.AddPendingAttack(finisherDamage);
                }
                break;
        }
    }

    private void ApplyDefenseOnOffEffects(CardView card)
    {
        foreach (CardEffect effect in card.Data.effects)
        {
            if (effect.effectType != EffectType.DefenseOnOff) continue;
            // OFF될 때 → Defense (secondaryValue)
            int defense = Mathf.Max(1, Mathf.RoundToInt(effect.secondaryValue));
            BattleManager.Instance.Player.AddDefense(defense);
        }
    }

    // ── 재발동형 ──────────────────────────────────────────

    private IEnumerator ApplyReplayEffect(CardView replayCard, ChainLoopContext loopContext)
    {
        CardView target = CardManager.Instance.FirstPlacedCard;

        // 자기 자신이거나 없으면 무시
        if (target == null || target == replayCard) yield break;
        if (target.CurrentSlot == null) yield break;
        if (!loopContext.TryRecordState(BuildRuntimeLoopStateKey("Replay", new List<CardView> { target }), _maxLoopCount, "Replay"))
            yield break;

        yield return ActivateChainFrom(target, _activatedCards, loopContext);
    }

    // ── 자동 트리거 ───────────────────────────────────────

    private IEnumerator CheckAutoTriggers(ChainLoopContext loopContext)
    {
        if (GridManager.Instance == null) yield break;
        if (loopContext.InfiniteLoopDetected) yield break;

        int onCount = 0;
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null && slot.OccupiedCard.IsActivated)
                onCount++;

        List<CardView> toTrigger = new();
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            if (card == null || card.IsEnemy) continue;
            if (card.Data == null || !card.Data.hasAutoTrigger) continue;
            if (onCount >= card.Data.autoTriggerThreshold)
                toTrigger.Add(card);
        }

        if (toTrigger.Count == 0) yield break;
        if (!loopContext.TryRecordState(BuildRuntimeLoopStateKey("AutoTrigger", toTrigger), _maxLoopCount, "AutoTrigger"))
            yield break;

        // 조건 충족 카드들을 동시에 하나의 웨이브로 토글
        foreach (CardView card in toTrigger)
        {
            if (card == null || card.CurrentSlot == null) continue;
            if (!loopContext.TryAdvanceStep(_maxActivationSteps, "AutoTrigger")) yield break;

            bool nextState = !card.IsActivated;
            card.SetActivated(nextState);

            if (nextState && !card.IsEnemy)
            {
                _turnToggleCount++;
                OnToggleCountChanged?.Invoke();
                card.Instance?.PersistentState.IncrementTurnOnCount();
                HashSet<EffectType> appliedTypes = ApplyEffects(card, EffectTrigger.OnActivated, loopContext);
                PlayActionBlockFlightEffect(card, appliedTypes);
                if (loopContext.InfiniteLoopDetected) yield break;
            }
            else if (!nextState && !card.IsEnemy)
            {
                ApplyDefenseOnOffEffects(card);
            }

            StartCoroutine(card.PlayActivationFeedback(_cardFeedbackDuration));
        }

        yield return new WaitForSeconds(_cardFeedbackDuration);

        // 새로 ON된 카드들로 체인 전파
        yield return ActivateChainFromWave(
            toTrigger.FindAll(c => c != null && c.IsActivated),
            _activatedCards,
            loopContext);
    }

    // ── 폭발형 ────────────────────────────────────────────

    private void ApplyExplodeEffect(CardView card, ChainLoopContext loopContext)
    {
        if (card?.Data == null || card.CurrentSlot == null) return;
        if (loopContext != null && loopContext.InfiniteLoopDetected) return;
        if (loopContext != null
            && !loopContext.TryRecordState(BuildRuntimeLoopStateKey("Explode", new List<CardView> { card }), _maxLoopCount, "Explode"))
            return;

        HashSet<CardView> nextWave = new();

        foreach (CardDirection dir in card.Data.GetAllDirections())
        {
            GridSlot neighborSlot = GridManager.Instance.GetNeighbor(card.CurrentSlot, dir);
            if (neighborSlot == null) continue;

            CardView neighbor = neighborSlot.OccupiedCard;
            if (neighbor == null || neighbor.IsEnemy) continue;
            if (loopContext != null && !loopContext.TryAdvanceStep(_maxActivationSteps, "Explode")) return;

            bool wasOff = !neighbor.IsActivated;

            if (wasOff)
                neighbor.SetActivated(true);

            // ON 여부와 관계없이 효과 발동 + 카운트 증가 + 피드백
            _turnToggleCount++;
            OnToggleCountChanged?.Invoke();
            neighbor.Instance?.PersistentState.IncrementTurnOnCount();
            HashSet<EffectType> appliedTypes = ApplyEffects(neighbor, EffectTrigger.OnActivated, loopContext);
            PlayActionBlockFlightEffect(neighbor, appliedTypes);
            if (loopContext != null && loopContext.InfiniteLoopDetected) return;
            StartCoroutine(neighbor.PlayActivationFeedback(_cardFeedbackDuration));

            // OFF→ON이 된 카드만 이웃으로 체인 전파
            if (wasOff)
                nextWave.Add(neighbor);
        }

        // 소멸 처리 (효과 발동 후)
        CardManager.Instance.ExileCard(card);

        // 새로 ON된 카드들의 이웃부터 체인 시작 (카드 자체는 이미 ON 상태)
        if (nextWave.Count > 0)
            StartCoroutine(ActivateChainFromWaveTracked(new List<CardView>(nextWave), _activatedCards, loopContext));
    }

    private IEnumerator ActivateChainFromWaveTracked(
        List<CardView> emitters,
        HashSet<CardView> activatedCards,
        ChainLoopContext loopContext)
    {
        _runningDetachedChainCount++;
        yield return ActivateChainFromWave(emitters, activatedCards, loopContext);
        _runningDetachedChainCount = Mathf.Max(0, _runningDetachedChainCount - 1);
    }

    private IEnumerator ActivateChainFromWave(
        List<CardView> emitters,
        HashSet<CardView> activatedCards,
        ChainLoopContext loopContext)
    {
        if (loopContext.InfiniteLoopDetected) yield break;
        if (!loopContext.TryRecordState(BuildRuntimeLoopStateKey("Wave", emitters), _maxLoopCount, "Wave"))
            yield break;

        // emitters는 이미 ON 상태 — 이웃으로만 전파
        HashSet<CardView> nextWaveSet = new();
        foreach (CardView emitter in emitters)
        {
            if (emitter?.Data == null || emitter.CurrentSlot == null) continue;
            activatedCards.Add(emitter);

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

        yield return new WaitForSeconds(_cardFeedbackDuration);

        if (nextWaveSet.Count > 0)
        {
            // 다음 웨이브 카드들을 ActivateChainFrom에 순차 처리
            // 웨이브 내 카드가 여러 개일 때 각각 독립 체인으로 시작
            foreach (CardView next in nextWaveSet)
            {
                if (loopContext.InfiniteLoopDetected) yield break;
                yield return ActivateChainFrom(next, activatedCards, loopContext);
            }
        }
    }

    // ── 배치 시 이펙트 ────────────────────────────────────

    public void ApplyOnPlacedEffects(CardView card)
    {
        if (card?.Data?.effects == null || card.IsEnemy) return;
        ApplyEffects(card, EffectTrigger.OnPlaced);
    }

    public void ApplyTurnEndEffects()
    {
        if (GridManager.Instance == null) return;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            if (card == null || card.IsEnemy) continue;
            if (!card.IsActivated) continue;
            ApplyEffects(card, EffectTrigger.OnTurnEnd);
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

            case CountScope.Self:
                return card.Instance?.PersistentState.TurnOnCount ?? 0;

            case CountScope.GridTotal:
                return _turnToggleCount;

            default:
                return 0;
        }
    }

    private IEnumerator ActivateChainFrom(CardView root, HashSet<CardView> activatedCards, ChainLoopContext loopContext)
    {
        List<CardView> currentWave = new() { root };

        while (currentWave.Count > 0)
        {
            if (loopContext.InfiniteLoopDetected) yield break;
            if (!loopContext.TryRecordState(BuildRuntimeLoopStateKey("Chain", currentWave), _maxLoopCount, "Chain"))
                yield break;

            List<CardView> emitters = new();

            foreach (CardView current in currentWave)
            {
                if (current == null || current.CurrentSlot == null) continue;

                bool nextState = !current.IsActivated;
                current.SetActivated(nextState);

                if (!loopContext.TryAdvanceStep(_maxActivationSteps, "Chain")) yield break;

                if (nextState)
                {
                    emitters.Add(current);
                    activatedCards.Add(current);

                    if (!current.IsEnemy)
                    {
                        // 임계 활성화: ON 횟수 누적
                        current.Instance?.PersistentState.IncrementTurnOnCount();
                        // 카운터형: 그리드 전체 ON 횟수 누적
                        _turnToggleCount++;
                        OnToggleCountChanged?.Invoke();

                        bool hasMotionRequest = TryCreateMotionRequest(current, out CharacterMotionRequest motionRequest);
                        HashSet<EffectType> appliedTypes = ApplyEffects(current, EffectTrigger.OnActivated, loopContext);
                        PlayActionBlockFlightEffect(current, appliedTypes);
                        if (loopContext.InfiniteLoopDetected) yield break;

                        if (hasMotionRequest)
                        {
                            CharacterMotionEvents.RequestCardMotion(motionRequest);
                        }

                        // Replay 이펙트: 체인 흐름 안에서 처리
                        if (current.Data?.effects != null)
                        {
                            foreach (CardEffect effect in current.Data.effects)
                            {
                                if (effect.trigger != EffectTrigger.OnActivated) continue;
                                if (effect.effectType != EffectType.Replay) continue;
                                yield return ApplyReplayEffect(current, loopContext);
                                if (loopContext.InfiniteLoopDetected) yield break;
                            }
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

                    // 반전형: OFF될 때 즉시 방어 발동
                    if (!current.IsEnemy && current.Data?.effects != null)
                        ApplyDefenseOnOffEffects(current);
                }
            }

            foreach (CardView current in currentWave)
                if (current != null)
                    StartCoroutine(current.PlayActivationFeedback(_cardFeedbackDuration));

            yield return new WaitForSeconds(_cardFeedbackDuration);

            HashSet<CardView> nextWaveSet = new();
            foreach (CardView emitter in emitters)
            {
                if (loopContext.InfiniteLoopDetected) yield break;
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

            currentWave = new List<CardView>(nextWaveSet);
        }
    }

    private bool TryCreateMotionRequest(CardView card, out CharacterMotionRequest request)
    {
        request = default;
        if (card?.Data?.effects == null || card.CurrentSlot == null) return false;

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

    private void PlayActionBlockFlightEffect(CardView card, HashSet<EffectType> appliedTypes)
    {
        if (_actionBlockFlightEffectPlayer == null || card == null || appliedTypes == null) return;
        if (!appliedTypes.Contains(EffectType.Damage) && !appliedTypes.Contains(EffectType.Defense)) return;

        StartCoroutine(_actionBlockFlightEffectPlayer.Play(card, appliedTypes));
    }

    private bool ContainsEffect(CardData data, EffectType effectType)
    {
        if (data?.effects == null) return false;

        foreach (CardEffect effect in data.effects)
            if (effect.effectType == effectType)
                return true;

        return false;
    }

    private IEnumerator ExecuteInfiniteLoopFinishRoutine()
    {
        yield return PlayInfiniteLoopTextRoutine();

        if (_loopSlashDirector != null)
            yield return _loopSlashDirector.PlayAndWait();
        else
            Debug.LogWarning("[ChainExecutor] 무한 루프 연출용 BattleCinematicSlashDirector가 연결되지 않았습니다.");

        if (BattleManager.Instance != null && BattleManager.Instance.Enemy != null && !BattleManager.Instance.Enemy.IsDead)
            BattleManager.Instance.DealDamageToEnemy(_infiniteLoopFinishDamage);
    }

    private IEnumerator PlayInfiniteLoopTextRoutine()
    {
        if (_infiniteLoopText == null)
        {
            Debug.LogWarning("[ChainExecutor] 무한 루프 TMP 텍스트가 연결되지 않았습니다.");
            yield break;
        }

        GameObject textObject = _infiniteLoopText.gameObject;
        Transform textTransform = _infiniteLoopText.transform;
        Color originalColor = _infiniteLoopText.color;
        Vector3 originalScale = textTransform.localScale;

        textObject.SetActive(true);

        Sequence colorSequence = DOTween.Sequence();
        Color[] rainbowColors =
        {
            Color.red,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            new(0.65f, 0f, 1f, 1f)
        };

        float colorStepDuration = Mathf.Max(0.05f, _infiniteLoopTextDuration / rainbowColors.Length);
        foreach (Color color in rainbowColors)
            colorSequence.Append(_infiniteLoopText.DOColor(color, colorStepDuration).SetEase(Ease.Linear));
        colorSequence.SetLoops(-1, LoopType.Restart);

        Tween scaleTween = textTransform
            .DOScale(originalScale * 1.15f, 0.24f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        yield return new WaitForSeconds(_infiniteLoopTextDuration);

        colorSequence.Kill();
        scaleTween.Kill();
        _infiniteLoopText.color = originalColor;
        textTransform.localScale = originalScale;
        textObject.SetActive(false);
    }

    private void SetAllCardsDraggable(bool draggable)
    {
        if (GridManager.Instance != null)
        {
            foreach (GridSlot slot in GridManager.Instance.Slots.Values)
                if (slot.OccupiedCard != null)
                    slot.OccupiedCard.SetDraggable(draggable);
        }

        if (CardManager.Instance != null)
        {
            foreach (CardView card in CardManager.Instance.Hand)
                if (card != null)
                    card.SetDraggable(draggable);
        }
    }

    private static void ClearPlayerMotionQueues()
    {
        CharacterMotionQueuePlayer[] queuePlayers = FindObjectsByType<CharacterMotionQueuePlayer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (CharacterMotionQueuePlayer queuePlayer in queuePlayers)
            if (queuePlayer != null)
                queuePlayer.CancelQueuedMotions();
    }

    private static bool IsEnemyDead()
    {
        return BattleManager.Instance != null
            && BattleManager.Instance.Enemy != null
            && BattleManager.Instance.Enemy.IsDead;
    }

    private List<CardView> GetPlacedCards()
    {
        List<CardView> result = new();
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null)
                result.Add(slot.OccupiedCard);
        return result;
    }

    private static void SortCardsByGridPosition(List<CardView> cards)
    {
        cards.Sort((a, b) =>
        {
            Vector2Int aPosition = a != null && a.CurrentSlot != null ? a.CurrentSlot.Position : Vector2Int.zero;
            Vector2Int bPosition = b != null && b.CurrentSlot != null ? b.CurrentSlot.Position : Vector2Int.zero;
            int yCompare = aPosition.y.CompareTo(bPosition.y);
            return yCompare != 0 ? yCompare : aPosition.x.CompareTo(bPosition.x);
        });
    }

    private string BuildRuntimeLoopStateKey(string source, List<CardView> currentWave)
    {
        StringBuilder builder = new();

        builder.Append(source);
        builder.Append("|wave:");

        List<CardView> waveCards = new(currentWave);
        SortCardsByGridPosition(waveCards);
        foreach (CardView card in waveCards)
            builder.Append(GetCardInstanceId(card)).Append(',');

        builder.Append("|first:");
        builder.Append(GetCardInstanceId(CardManager.Instance != null ? CardManager.Instance.FirstPlacedCard : null));

        builder.Append("|grid:");
        List<GridSlot> slots = GetSortedSlots();
        foreach (GridSlot slot in slots)
        {
            builder.Append(slot.Position.x).Append(',').Append(slot.Position.y).Append('=');

            CardView card = slot.OccupiedCard;
            if (card == null)
            {
                builder.Append("empty;");
                continue;
            }

            builder.Append(GetCardInstanceId(card)).Append(':');
            builder.Append(card.IsActivated ? '1' : '0').Append(':');
            builder.Append(card.IsEnemy ? '1' : '0').Append(':');
            builder.Append(card.Data != null ? card.Data.cardId : "").Append(':');
            builder.Append(card.Data != null ? card.Data.range : 0).Append(':');
            builder.Append(GetDirectionMask(card.Data)).Append(':');
            builder.Append(card.Data != null && card.Data.hasAutoTrigger ? '1' : '0').Append(':');
            builder.Append(card.Data != null ? card.Data.autoTriggerThreshold : 0).Append(':');
            builder.Append(BuildEffectProgressKey(card)).Append(';');
        }

        return builder.ToString();
    }

    private string BuildEffectProgressKey(CardView card)
    {
        if (card?.Data?.effects == null) return "";

        StringBuilder builder = new();
        for (int i = 0; i < card.Data.effects.Count; i++)
        {
            CardEffect effect = card.Data.effects[i];
            if (effect.trigger != EffectTrigger.OnActivated) continue;

            builder.Append(i).Append(':');
            builder.Append((int)effect.effectType).Append(':');
            builder.Append((int)effect.scope).Append(':');
            builder.Append((int)effect.thresholdType).Append(':');

            int threshold = Mathf.Max(0, effect.threshold);
            if (effect.scope == CountScope.Self || effect.scope == CountScope.GridTotal)
                builder.Append(Mathf.Min(CountByScope(effect.scope, card), threshold));
            else
                builder.Append(IsEffectConditionMet(effect, card) ? '1' : '0');

            builder.Append(',');
        }

        return builder.ToString();
    }

    private bool IsEffectConditionMet(CardEffect effect, CardView card)
    {
        if (effect.thresholdType == ThresholdType.Full)
            return IsFullActivated(effect.scope, card);
        if (effect.scope == CountScope.None)
            return true;
        return CountByScope(effect.scope, card) >= effect.threshold;
    }

    private List<GridSlot> GetSortedSlots()
    {
        List<GridSlot> slots = new();
        if (GridManager.Instance == null) return slots;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot != null)
                slots.Add(slot);

        slots.Sort((a, b) =>
        {
            int yCompare = a.Position.y.CompareTo(b.Position.y);
            return yCompare != 0 ? yCompare : a.Position.x.CompareTo(b.Position.x);
        });

        return slots;
    }

    private static int GetCardInstanceId(CardView card)
    {
        return card != null && card.Instance != null ? card.Instance.InstanceId : 0;
    }

    private static int GetDirectionMask(CardData data)
    {
        if (data == null) return 0;

        int mask = 0;
        foreach (CardDirection direction in data.GetAllDirections())
            mask |= 1 << (int)direction;
        return mask;
    }

    /// <summary>무한 루프가 감지됐을 때 원인을 로그로 남깁니다.</summary>
    private void OnInfiniteLoopDetected(string reason)
    {
        Debug.Log($"[ChainExecutor] 무한 루프 판정 — {reason}");
    }

    protected override void Dispose()
    {
        OnChainStarted = null;
        OnChainFinished = null;
        OnToggleCountChanged = null;
        base.Dispose();
    }
}
