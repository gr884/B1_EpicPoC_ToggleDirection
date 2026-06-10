using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public partial class ChainExecutor : SingletonBehaviour<ChainExecutor>
{
    [Header("Chain Settings")]
    [SerializeField] private int _maxActivationSteps = 2048;
    [SerializeField] private float _cardFeedbackDuration = 0.22f;
    [SerializeField] private int _maxLoopCount = 3;

    [Header("Card Effect Visual")]
    [SerializeField] private CardEffectPlaySystem _cardEffectPlaySystem;
    [SerializeField] private TriggerDirectionLineEffectPlayer _triggerDirectionLineEffectPlayer;

    [Header("Infinite Loop Finish")]
    [SerializeField] private TMP_Text _infiniteLoopText;
    [SerializeField] private BattleCinematicSlashDirector _loopSlashDirector;
    [SerializeField] private float _infiniteLoopGraceDuration = 2f;
    [SerializeField] private float _infiniteLoopTextDuration = 1.2f;
    [SerializeField] private int _infiniteLoopFinishDamage = 999;

    public event Action OnChainStarted;
    public event Action OnChainFinished;

    private readonly HashSet<CardView> _activatedCards = new();
    public IReadOnlyCollection<CardView> ActivatedCards => _activatedCards;

    // 카운터형: 이번 턴 그리드 전체 ON 횟수
    private int _turnToggleCount;
    public int TurnToggleCount => _turnToggleCount;

    public event Action OnToggleCountChanged;
    public event Action OnTotemAuraChanged;

    private TotemAuraSystem _totemSystem;
    private readonly InfiniteLoopDetector _loopDetector = new();

    // ON 상태에서 꺼지지 않아야 하는 카드 집합 (축전기 방전 중 등)
    private readonly HashSet<CardView> _onLockedCards = new();

    public int GetTotemDamageBonus(CardView card) => _totemSystem != null ? _totemSystem.GetDamageBonus(card) : 0;
    public int GetTotemDefenseBonus(CardView card) => _totemSystem != null ? _totemSystem.GetDefenseBonus(card) : 0;

    public void ResetTurnToggleCount()
    {
        _turnToggleCount = 0;
        _onLockedCards.Clear();
        OnToggleCountChanged?.Invoke();
    }

    public bool IsExecuting { get; private set; }

    public void Init()
    {
        Debug.Log("[ChainExecutor] Init");
        EnsureTotemSystem();
        RefreshTotemAuras();
    }

    private void EnsureTotemSystem()
    {
        if (_totemSystem != null) return;
        _totemSystem = GetComponent<TotemAuraSystem>();
        if (_totemSystem == null)
            _totemSystem = gameObject.AddComponent<TotemAuraSystem>();
    }

    public void ExecuteFrom(CardView rootCard)
    {
        StartCoroutine(ExecuteChain(rootCard));
    }

    public void ExecutePlacedCard(CardView card)
    {
        StartCoroutine(ExecutePlacedCardRoutine(card));
    }

    private IEnumerator ExecutePlacedCardRoutine(CardView card)
    {
        if (card == null) yield break;

        IsExecuting = true;
        yield return ApplyOnPlacedEffects(card);
        ExecuteFrom(card);
    }

    private IEnumerator ExecuteChain(CardView rootCard)
    {
        if (rootCard == null) yield break;

        bool willCreateInfiniteLoop = _loopDetector.WouldCreateInfiniteLoop(rootCard, _maxActivationSteps);

        IsExecuting = true;
        OnChainStarted?.Invoke();
        _activatedCards.Clear();
        RefreshTotemAuras();

        SetAllCardsDraggable(false);

        if (willCreateInfiniteLoop)
            yield return ExecutePredictedInfiniteLoopRoutine(rootCard, _activatedCards);
        else
            yield return ActivateChainFrom(rootCard, _activatedCards);

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null)
                slot.OccupiedCard.SetDraggable(false);

        // 자동 트리거: 체인 종료 후 조건 충족 카드 자동 ON
        yield return CheckAutoTriggers();
        RefreshTotemAuras();
        IsExecuting = false;
        OnChainFinished?.Invoke();
    }

    public float GetTotemAdjustedValue(CardView card, EffectType effectType, float baseValue)
    {
        EnsureTotemSystem();
        return _totemSystem.GetAdjustedValue(card, effectType, baseValue);
    }

    public void RefreshTotemAuras()
    {
        EnsureTotemSystem();
        _totemSystem.Refresh();
        OnTotemAuraChanged?.Invoke();
    }

    // ── 재발동형 ──────────────────────────────────────────

    private IEnumerator ApplyReplayEffect(CardView replayCard)
    {
        CardView target = CardManager.Instance.FirstPlacedCard;

        // 자기 자신이거나 없으면 무시
        if (target == null || target == replayCard) yield break;
        if (target.CurrentSlot == null) yield break;

        // 독립적인 activatedCards로 실행 (기존 체인과 충돌 방지)
        yield return ActivateChainFrom(target, new HashSet<CardView>());
    }

    // ── 자동 트리거 ───────────────────────────────────────

    private IEnumerator CheckAutoTriggers()
    {
        if (GridManager.Instance == null) yield break;

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

        // 조건 충족 카드들을 동시에 하나의 웨이브로 토글
        foreach (CardView card in toTrigger)
        {
            if (card == null || card.CurrentSlot == null) continue;
            bool nextState = !card.IsActivated;
            card.SetActivated(nextState);
            HandleCastingStateChange(card, nextState);
            RefreshTotemAuras();

            if (nextState && !card.IsEnemy)
            {
                PlayTriggerDirectionLine(card);
                yield return FireCardOnEffects(card);
            }
            else if (!nextState && !card.IsEnemy)
            {
                yield return ApplyDefenseOnOffEffects(card);
            }

            StartCoroutine(card.PlayActivationFeedback(_cardFeedbackDuration));
        }

        yield return new WaitForSeconds(_cardFeedbackDuration);

        // 새로 ON된 카드들로 체인 전파
        yield return ActivateChainFromWave(
            toTrigger.FindAll(c => c != null && c.IsActivated),
            _activatedCards);
    }

    // ── 폭발형 ────────────────────────────────────────────

    private IEnumerator ApplyExplodeEffect(CardView card)
    {
        if (card?.Data == null || card.CurrentSlot == null) yield break;

        HashSet<CardView> nextWave = new();

        foreach (CardDirection dir in card.Data.GetAllDirections())
        {
            GridSlot neighborSlot = GridManager.Instance.GetNeighbor(card.CurrentSlot, dir);
            if (neighborSlot == null) continue;

            CardView neighbor = neighborSlot.OccupiedCard;
            if (neighbor == null || neighbor.IsEnemy) continue;

            bool wasOff = !neighbor.IsActivated;

            if (wasOff)
            {
                neighbor.SetActivated(true);
                HandleCastingStateChange(neighbor, true);
                PlayTriggerDirectionLine(neighbor);
            }
            RefreshTotemAuras();

            // ON 여부와 관계없이 효과 발동 + 카운트 증가 + 피드백
            yield return FireCardOnEffects(neighbor);
            StartCoroutine(neighbor.PlayActivationFeedback(_cardFeedbackDuration));

            // OFF→ON이 된 카드만 이웃으로 체인 전파
            if (wasOff)
                nextWave.Add(neighbor);
        }

        // 소멸 처리 (효과 발동 후)
        CardManager.Instance.ExileCard(card);

        // 새로 ON된 카드들의 이웃부터 체인 시작 (카드 자체는 이미 ON 상태)
        if (nextWave.Count > 0)
            StartCoroutine(ActivateChainFromWave(new List<CardView>(nextWave), _activatedCards));
    }

    private IEnumerator ActivateChainFromWave(List<CardView> emitters, HashSet<CardView> activatedCards)
    {
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
                yield return ActivateChainFrom(next, activatedCards);
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

                // 축전기 카드 처리: 일반 토글 대신 충전/방전 로직으로 분기
                if (!current.IsEnemy && current.Data != null && current.Data.isCapacitorCard)
                {
                    var capRuntime = current.GetComponent<CardRuntimeState>();
                    if (current.IsActivated && _onLockedCards.Contains(current))
                    {
                        // 방전 중 — 토글 무시
                    }
                    else if (!current.IsActivated)
                    {
                        // OFF 상태 — 충전 카운트 증가
                        capRuntime?.IncrementCharge();
                        StartCoroutine(current.PlayActivationFeedback(_cardFeedbackDuration));

                        int chargeCount = capRuntime?.CurrentChargeCount ?? 0;
                        int required = current.Data.capacitorChargeRequired;
                        if (chargeCount >= required)
                        {
                            // 충전 완료 → ON 전환 후 방전 시작
                            current.SetActivated(true);
                            RefreshTotemAuras();
                            _onLockedCards.Add(current);
                            StartCoroutine(DischargeCapacitor(current, activatedCards));
                        }
                    }
                    continue;
                }

                bool nextState = !current.IsActivated;
                current.SetActivated(nextState);
                HandleCastingStateChange(current, nextState);
                RefreshTotemAuras();

                step++;
                if (step > _maxActivationSteps)
                {
                    Debug.LogWarning("[ChainExecutor] 안전 한도 초과로 체인 중단.");
                    yield break;
                }

                if (nextState)
                {
                    PlayTriggerDirectionLine(current);
                    emitters.Add(current);
                    activatedCards.Add(current);

                    if (!current.IsEnemy)
                    {
                        yield return FireCardOnEffects(current);

                        // Replay 이펙트: 체인 흐름 안에서 처리
                        if (current.Data?.effects != null)
                        {
                            foreach (CardEffect effect in current.Data.effects)
                            {
                                if (effect.trigger != EffectTrigger.OnActivated) continue;
                                if (effect.effectType != EffectType.Replay) continue;
                                yield return ApplyReplayEffect(current);
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

                    // 반전형: OFF될 때 방어 발동
                    if (!current.IsEnemy && current.Data?.effects != null)
                        yield return ApplyDefenseOnOffEffects(current);
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

    // ON된 아군 카드의 공통 부수효과 발동 (시각 라인·체인 제어는 호출부 담당)
    private IEnumerator FireCardOnEffects(CardView card)
    {
        // 임계 활성화: ON 횟수 누적
        card.Instance?.PersistentState.IncrementTurnOnCount();
        // 카운터형: 그리드 전체 ON 횟수 누적
        _turnToggleCount++;
        OnToggleCountChanged?.Invoke();

        ProcessCastingTriggers(card);

        yield return ApplyEffects(card);
    }

    // ── 축전기 방전 ───────────────────────────────────────

    private IEnumerator DischargeCapacitor(CardView card, HashSet<CardView> activatedCards)
    {
        if (card?.Data == null || card.CurrentSlot == null) yield break;

        int dischargeCount = card.Data.capacitorDischargeCount;
        for (int i = 0; i < dischargeCount; i++)
        {
            if (card.CurrentSlot == null) break;

            PlayTriggerDirectionLine(card);

            // 화살표 방향 이웃 카드들을 체인 발동
            foreach (CardDirection dir in card.Data.GetAllDirections())
            {
                GridSlot current = card.CurrentSlot;
                for (int r = 0; r < card.Data.range; r++)
                {
                    GridSlot neighbor = GridManager.Instance.GetNeighbor(current, dir);
                    if (neighbor == null) break;
                    if (neighbor.OccupiedCard != null)
                        yield return ActivateChainFrom(neighbor.OccupiedCard, activatedCards);
                    current = neighbor;
                }
            }
        }

        // 방전 완료 — OFF로 전환 및 상태 리셋
        _onLockedCards.Remove(card);
        card.SetActivated(false);
        RefreshTotemAuras();
        card.GetComponent<CardRuntimeState>()?.ResetCharge();
    }

    private void PlayTriggerDirectionLine(CardView card)
    {
        if (card == null || card.IsEnemy || card.Data == null) return;

        TriggerDirectionLineEffectPlayer player = ResolveTriggerDirectionLineEffectPlayer();
        player?.Play(card);
    }

    private TriggerDirectionLineEffectPlayer ResolveTriggerDirectionLineEffectPlayer()
    {
        if (_triggerDirectionLineEffectPlayer != null)
            return _triggerDirectionLineEffectPlayer;

        _triggerDirectionLineEffectPlayer = FindFirstObjectByType<TriggerDirectionLineEffectPlayer>(FindObjectsInactive.Include);
        if (_triggerDirectionLineEffectPlayer != null)
            return _triggerDirectionLineEffectPlayer;

        GameObject obj = new("TriggerDirectionLineEffectPlayer");
        obj.transform.SetParent(transform, false);
        _triggerDirectionLineEffectPlayer = obj.AddComponent<TriggerDirectionLineEffectPlayer>();
        return _triggerDirectionLineEffectPlayer;
    }

    private IEnumerator ExecutePredictedInfiniteLoopRoutine(CardView root, HashSet<CardView> activatedCards)
    {
        bool chainFinished = false;
        Coroutine chainRoutine = StartCoroutine(ActivateChainAndMarkFinished(root, activatedCards, () => chainFinished = true));

        float elapsed = 0f;
        float graceDuration = Mathf.Max(0f, _infiniteLoopGraceDuration);
        while (elapsed < graceDuration && !IsEnemyDead())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!chainFinished && chainRoutine != null)
            StopCoroutine(chainRoutine);

        if (IsEnemyDead())
            yield break;

        ClearEffectQueues();
        yield return ExecuteInfiniteLoopFinishRoutine();
    }

    private IEnumerator ActivateChainAndMarkFinished(
        CardView root,
        HashSet<CardView> activatedCards,
        Action onFinished)
    {
        yield return ActivateChainFrom(root, activatedCards);
        onFinished?.Invoke();
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

        if (CardManager.Instance != null && draggable)
            foreach (CardView card in CardManager.Instance.Hand)
                if (card != null)
                    card.SetDraggable(true);
    }

    private static void ClearEffectQueues()
    {
        CardEffectPlaySystem[] queuePlayers = FindObjectsByType<CardEffectPlaySystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (CardEffectPlaySystem queuePlayer in queuePlayers)
            if (queuePlayer != null)
                queuePlayer.CancelQueuedEffects();
    }

    private static bool IsEnemyDead()
    {
        return BattleManager.Instance != null
            && BattleManager.Instance.Enemy != null
            && BattleManager.Instance.Enemy.IsDead;
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
        OnToggleCountChanged = null;
        OnTotemAuraChanged = null;
        base.Dispose();
    }

    //* ON/OFF 시 캐스팅 카운트를 리셋
    private void HandleCastingStateChange(CardView card, bool isNowActivated)
    {
        if (card == null || card.IsEnemy || card.Data == null || !card.Data.isCastingCard) return;
        var runtime = card.GetComponent<CardRuntimeState>();
        if (runtime == null) return;

        if (isNowActivated) runtime.InitializeCasting(card.Data.castingRequiredCount);
        else runtime.ResetCasting();
    }

    //* 다른 카드가 켜질 때 맵을 싹 뒤져서 캐스팅 카운트를 깎음
    private void ProcessCastingTriggers(CardView triggerCard)
    {
        if (GridManager.Instance == null) return;
        List<CardView> cardsToExecute = new();

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            // 켜져 있는 아군 캐스팅 카드만 찾음 (자기 자신 제외)
            if (card == null || card.IsEnemy || !card.IsActivated) continue;
            if (card == triggerCard) continue;
            if (card.Data == null || !card.Data.isCastingCard) continue;

            var runtime = card.GetComponent<CardRuntimeState>();
            if (runtime != null && runtime.CurrentCastingCount > 0)
            {
                runtime.DecreaseCasting();
                if (runtime.CurrentCastingCount <= 0) // 카운트가 0이 되면 발동 대기열에 추가
                    cardsToExecute.Add(card);
            }
        }

        foreach (CardView c in cardsToExecute) ExecuteCasting(c);
    }

    //* 캐스팅 완료 시 효과를 발동하고 꺼뜨림
    private void ExecuteCasting(CardView card)
    {
        var runtime = card.GetComponent<CardRuntimeState>();
        if (card.Data?.effects != null)
        {
            foreach (var effect in card.Data.effects)
            {
                if (effect.effectType == EffectType.CastingDamage)
                {
                    float adjusted = GetTotemAdjustedValue(card, EffectType.CastingDamage, effect.value);
                    int baseDmg = Mathf.Max(1, Mathf.RoundToInt(adjusted));
                    int finalDmg = runtime != null ? runtime.GetModifiedDamage(baseDmg) : baseDmg;
                    DealDamageToEnemy(finalDmg);
                }
                else if (effect.effectType == EffectType.CastingDefense)
                {
                    float adjusted = GetTotemAdjustedValue(card, EffectType.Defense, effect.value);
                    int baseDef = Mathf.Max(1, Mathf.RoundToInt(adjusted));
                    BattleManager.Instance.Player.AddDefense(baseDef);
                }
            }
        }
        // 딜을 넣고 스스로 꺼짐
        card.SetActivated(false);
        HandleCastingStateChange(card, false);
        RefreshTotemAuras();
        _activatedCards.Remove(card); // 체인 대기열에서 안전하게 제거
    }
}