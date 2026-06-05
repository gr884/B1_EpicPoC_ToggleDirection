using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : SingletonBehaviour<CardManager>
{
    [Header("Refs")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private RectTransform _handRoot;
    [SerializeField] private Player _player;
    [SerializeField] private DeckHandFlightEffectPlayer _flightEffectPlayer;

    [Header("Deck")]
    [SerializeField] private List<CardData> _startingDeck = new();
    [SerializeField] private bool _shuffleOnReset = true;

    // ── 덱 상태 ────────────────────────────────────────────
    private readonly List<CardInstance> _drawPile = new();
    private readonly List<CardInstance> _discardPile = new();
    private readonly List<CardInstance> _exiledPile = new(); // 폭발형: 이번 전투 소멸 카드
    private readonly List<CardData> _drawPileView = new();
    private readonly List<CardData> _discardPileView = new();
    public int DrawPileCount => _drawPile.Count;
    public int DiscardPileCount => _discardPile.Count;
    public IReadOnlyList<CardData> DrawPile
    {
        get
        {
            RebuildPileViews();
            return _drawPileView;
        }
    }
    public IReadOnlyList<CardData> DiscardPile
    {
        get
        {
            RebuildPileViews();
            return _discardPileView;
        }
    }

    // ── 손패 상태 ──────────────────────────────────────────
    private readonly List<CardView> _hand = new();
    public IReadOnlyList<CardView> Hand => _hand;
    public int HandCount => _hand.Count;

    public event Action OnHandChanged;

    // ── 초기화 ─────────────────────────────────────────────

    public void Init()
    {
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        _player.OnCostChanged += RefreshHandAffordability;
        Debug.Log("[CardManager] Init");
    }

    private void OnChainFinished()
    {
        foreach (CardView card in _hand)
            if (card != null) card.SetDraggable(true);
        RefreshHandAffordability();
    }

    public void RefreshHandAffordability()
    {
        foreach (CardView card in _hand)
            if (card != null)
                card.SetAffordable(card.Data != null && _player.CanSpend(card.Data.cost));
    }

    // ── 덱 관리 ────────────────────────────────────────────

    public void ResetDeck()
    {
        // 소멸 카드 복귀 후 초기화
        foreach (CardInstance exiled in _exiledPile)
            exiled.ResetExile();
        _exiledPile.Clear();

        _drawPile.Clear();
        _discardPile.Clear();
        if (_startingDeck != null)
        {
            foreach (CardData data in _startingDeck)
                if (data != null)
                    _drawPile.Add(new CardInstance(data));
        }
        if (_shuffleOnReset)
            Shuffle(_drawPile);
        Debug.Log($"[CardManager] 덱 리셋 — {_drawPile.Count}장");
    }

    /// <summary>카드를 이번 전투에서 소멸시킵니다. 다음 전투 시작 시 복귀합니다.</summary>
    public void ExileCard(CardView card)
    {
        if (card == null || card.Instance == null) return;

        card.Instance.Exile();
        _exiledPile.Add(card.Instance);

        GridSlot slot = card.CurrentSlot;
        slot?.ClearCard();
        PoolManager.Instance.Return(card.gameObject);

        Debug.Log($"[CardManager] 카드 소멸 — {card.Data?.displayName}");
    }

    /// <summary>덱에 카드를 영구 추가합니다.</summary>
    public void AddCard(CardData data)
    {
        if (data == null) return;
        _startingDeck.Add(data);
        Debug.Log($"[CardManager] 덱에 카드 추가 — {data.displayName} (총 {_startingDeck.Count}장)");
    }

    /// <summary>현재 드로우 파일의 랜덤 위치에 저주 카드를 삽입합니다.</summary>
    public void InsertCurseCard(CardData curseCard)
    {
        if (curseCard == null) return;
        int index = UnityEngine.Random.Range(0, _drawPile.Count + 1);
        _drawPile.Insert(index, new CardInstance(curseCard));
        Debug.Log($"[CardManager] 저주 카드 삽입 — {curseCard.displayName} (드로우 파일 {index}번째)");
    }

    /// <summary>손패의 저주 카드 데미지 합산을 반환합니다.</summary>
    public int GetCurseHandDamage()
    {
        int total = 0;
        foreach (CardView card in _hand)
            if (card != null && card.Data != null && card.Data.isCurseCard)
                total += card.Data.curseDamage;
        return total;
    }

    /// <summary>덱에서 카드를 영구 제거합니다. 없으면 false 반환.</summary>
    public bool RemoveCard(CardData data)
    {
        if (data == null) return false;
        bool removed = _startingDeck.Remove(data);
        if (removed)
            Debug.Log($"[CardManager] 덱에서 카드 제거 — {data.displayName} (총 {_startingDeck.Count}장)");
        return removed;
    }

    private List<CardInstance> DrawCards(int count)
    {
        List<CardInstance> drawn = new();
        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            if (_drawPile.Count == 0)
                RefillDrawPile();
            if (_drawPile.Count == 0) break;

            int last = _drawPile.Count - 1;
            drawn.Add(_drawPile[last]);
            _drawPile.RemoveAt(last);
        }
        Debug.Log($"[CardManager] {drawn.Count}장 드로우 — 남은 드로우파일: {_drawPile.Count}");
        return drawn;
    }

    private void RefillDrawPile()
    {
        if (_discardPile.Count == 0)
        {
            Debug.Log("[CardManager] 버린 파일 없음 — 드로우 불가");
            return;
        }

        _flightEffectPlayer?.PlayRefillDiscardToDeck(_discardPile.Count);
        Shuffle(_discardPile);
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Debug.Log($"[CardManager] 버린 파일 → 드로우파일 재구성 — {_drawPile.Count}장");
    }

    // ── 손패 관리 ──────────────────────────────────────────

    public void StartBattleDraw()
    {
        ResetDeck();
        DrawToHand(_player.HandSize);
    }

    public void DrawToHand(int count)
    {
        int available = _player.MaxHandSize - _hand.Count;
        int drawCount = Mathf.Min(count, available);
        int overflow = count - drawCount;

        List<CardInstance> drawn = DrawCards(drawCount);
        foreach (CardInstance instance in drawn)
            SpawnToHand(instance);

        _flightEffectPlayer?.PlayDrawToHand(drawn.Count);

        // 손패 상한 초과분은 무덤으로
        if (overflow > 0)
        {
            List<CardInstance> discarded = DrawCards(overflow);
            foreach (CardInstance instance in discarded)
                _discardPile.Add(instance);
            Debug.Log($"[CardManager] 손패 상한 초과 — {overflow}장 무덤으로");
        }

        OnHandChanged?.Invoke();
    }

    public void DiscardAndDraw()
    {
        ResetTurnOnCounts();
        DiscardHand();
        DrawToHand(_player.HandSize);
        TutorialManager.Instance?.OnHandDrawn();
    }

    public void DiscardHand()
    {
        foreach (CardView card in _hand)
        {
            if (card == null) continue;
            if (card.Instance != null)
                _discardPile.Add(card.Instance);
            _flightEffectPlayer?.PlayDiscardFrom(card.transform);
            PoolManager.Instance.Return(card.gameObject);
        }

        _hand.Clear();
        OnHandChanged?.Invoke();
    }

    // ── 회수 ───────────────────────────────────────────────

    public void RecallCard(CardView card)
    {
        if (card == null || card.IsEnemy) return;

        GridSlot slot = card.CurrentSlot;
        if (slot == null) return;

        slot.ClearCard();

        CardInstance instance = card.Instance;
        CardData sourceData = card.Data;
        if (instance == null || sourceData == null) return;

        switch (sourceData.recallDestination)
        {
            case RecallDestination.Hand:
                _flightEffectPlayer?.PlayRecallToHandFrom(card.transform);
                PoolManager.Instance.Return(card.gameObject);
                SpawnToHand(instance);
                OnHandChanged?.Invoke();
                break;

            case RecallDestination.DrawPileTop:
                _drawPile.Add(instance);
                PoolManager.Instance.Return(card.gameObject);
                break;

            case RecallDestination.DrawPile:
                int index = UnityEngine.Random.Range(0, _drawPile.Count + 1);
                _drawPile.Insert(index, instance);
                PoolManager.Instance.Return(card.gameObject);
                break;
        }

        Debug.Log($"[CardManager] 카드 회수 — {sourceData.displayName} → {sourceData.recallDestination}");
    }

    // ── 그리드 초기화 ─────────────────────────────────────

    public void DiscardGrid()
    {
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.IsEmpty) continue;
            CardView card = slot.OccupiedCard;
            if (card.IsEnemy) continue;

            // 보존 스택이 있으면 1 차감 후 유지
            if (card.ConsumePreserve())
            {
                string displayName = card.Data != null ? card.Data.displayName : "(Unknown)";
                Debug.Log($"[CardManager] 보존 — {displayName} 그리드 유지 (남은 스택: {card.PreserveStack})");
                continue;
            }

            if (card.Instance != null)
                _discardPile.Add(card.Instance);
            _flightEffectPlayer?.PlayDiscardFrom(card.transform);
            slot.ClearCard();
            PoolManager.Instance.Return(card.gameObject);
        }

        Debug.Log("[CardManager] 그리드 플레이어 카드 → 버린 파일");
    }

    // ── 카드 배치 ──────────────────────────────────────────

    public bool TryPlaceCard(CardView card, GridSlot targetSlot)
    {
        if (card == null || targetSlot == null) return false;
        if (BattleManager.Instance.IsProcessing) return false;
        if (ChainExecutor.Instance.IsExecuting) return false;
        if (BattleManager.Instance.CurrentPhase != BattleManager.BattlePhase.PlayerTurn) return false;
        if (!_hand.Contains(card)) return false;
        if (card.Instance == null || card.Data == null) return false;
        if (card.Data.isUnplayable) return false;

        bool isRecaller = card.Data.isRecaller;

        // 일반 카드는 빈 슬롯만, 조작형은 점유 슬롯만 허용
        if (!isRecaller && !targetSlot.IsEmpty) return false;
        if (isRecaller && targetSlot.IsEmpty) return false;
        // 조작형은 적 카드 회수 불가
        if (isRecaller && targetSlot.OccupiedCard != null && targetSlot.OccupiedCard.IsEnemy) return false;
        if (GameManager.Instance.CurrentState == GameManager.GameState.Tutorial
            && !TutorialManager.Instance.CanPlaceCard(card, targetSlot)) return false;
        if (!_player.SpendCost(card.Data.cost)) return false;

        // 조작형: 대상 카드 손패로 회수 후 자신 소멸
        if (isRecaller)
        {
            CardView target = targetSlot.OccupiedCard;
            if (target != null)
            {
                targetSlot.ClearCard();
                SpawnToHand(target.Instance);
                PoolManager.Instance.Return(target.gameObject);
                OnHandChanged?.Invoke();
            }
            // 자신도 소멸 (그리드에 배치하지 않고 바로 exile)
            _hand.Remove(card);
            OnHandChanged?.Invoke();
            card.Instance.Exile();
            _exiledPile.Add(card.Instance);
            PoolManager.Instance.Return(card.gameObject);
            return true;
        }

        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        _hand.Remove(card);
        OnHandChanged?.Invoke();

        TutorialManager.Instance?.OnCardPlaced(card);
        ChainExecutor.Instance.ApplyOnPlacedEffects(card);
        ChainExecutor.Instance.ExecuteFrom(card);
        return true;
    }

    /// <summary>튜토리얼 전용 고정 덱을 세팅합니다.</summary>
    public void SetTutorialDeck(System.Collections.Generic.List<CardData> cards)
    {
        _drawPile.Clear();
        _discardPile.Clear();
        // DrawCards는 마지막 인덱스부터 뽑으므로 역순으로 추가
        for (int i = cards.Count - 1; i >= 0; i--)
            if (cards[i] != null)
                _drawPile.Add(new CardInstance(cards[i]));
    }

    // ── 유저 회수 액션 ─────────────────────────────────────

    public bool TryRecallCard(CardView card)
    {
        if (card == null || card.IsEnemy) return false;
        if (card.CurrentSlot == null) return false;
        if (BattleManager.Instance.IsProcessing) return false;
        if (BattleManager.Instance.CurrentPhase != BattleManager.BattlePhase.PlayerTurn) return false;
        CardInstance recallInstance = card.Instance;
        CardData recallData = card.Data;
        if (recallInstance == null || recallData == null) return false;
        if (!_player.SpendCost(1)) return false;

        GridSlot slot = card.CurrentSlot;
        slot.ClearCard();

        _flightEffectPlayer?.PlayRecallToHandFrom(card.transform);
        PoolManager.Instance.Return(card.gameObject);
        SpawnToHand(recallInstance);
        OnHandChanged?.Invoke();

        Debug.Log($"[CardManager] 유저 회수 — {recallData.displayName} → 손패");
        return true;
    }

    // ── 내부 ───────────────────────────────────────────────

    /// <summary>손패 카드를 버린파일/풀 없이 바로 제거. 덱 상태를 오염시키지 않을 때 사용.</summary>
    public void DestroyHand()
    {
        foreach (CardView card in _hand)
            if (card != null) UnityEngine.Object.Destroy(card.gameObject);
        _hand.Clear();
        OnHandChanged?.Invoke();
    }

    /// <summary>풀링 없이 카드를 새로 생성해서 손패에 추가. anchor 오염을 피해야 할 때 사용.</summary>
    public void DrawToHandFresh(List<CardData> cards)
    {
        foreach (CardData data in cards)
            if (data != null)
                SpawnToHandFresh(new CardInstance(data));
        OnHandChanged?.Invoke();
    }

    /// <summary>턴 시작 시 모든 카드의 TurnOnCount를 초기화합니다.</summary>
    private void ResetTurnOnCounts()
    {
        ChainExecutor.Instance.ResetTurnToggleCount();

        foreach (CardInstance instance in _drawPile)
            instance?.PersistentState.ResetTurnOnCount();
        foreach (CardInstance instance in _discardPile)
            instance?.PersistentState.ResetTurnOnCount();
        foreach (CardView card in _hand)
            card?.Instance?.PersistentState.ResetTurnOnCount();

        if (GridManager.Instance != null)
        {
            foreach (GridSlot slot in GridManager.Instance.Slots.Values)
                slot.OccupiedCard?.Instance?.PersistentState.ResetTurnOnCount();
        }
    }

    public void ClearCombatPersistentStates()
    {
        ChainExecutor.Instance.ResetTurnToggleCount();
        foreach (CardInstance instance in _drawPile)
            instance?.PersistentState.ClearCombatState();
        foreach (CardInstance instance in _discardPile)
            instance?.PersistentState.ClearCombatState();
        foreach (CardView card in _hand)
        {
            if (card == null || card.Instance == null) continue;
            card.Instance.PersistentState.ClearCombatState();
            card.GetComponent<CardRuntimeState>()?.Refresh();
        }

        if (GridManager.Instance != null)
        {
            foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            {
                CardView card = slot.OccupiedCard;
                if (card == null || card.Instance == null) continue;
                card.Instance.PersistentState.ClearCombatState();
                card.GetComponent<CardRuntimeState>()?.Refresh();
            }
        }
    }

    private void SpawnToHandFresh(CardInstance instance)
    {
        GameObject obj = UnityEngine.Object.Instantiate(_cardPrefab, _handRoot);
        CardView card = obj.GetComponent<CardView>();
        card.Initialize(instance);
        card.ApplyHandLayout();
        card.SetDraggable(true);
        card.SetAffordable(card.Data != null && _player.CanSpend(card.Data.cost));
        _hand.Add(card);
    }

    private void SpawnToHand(CardInstance instance)
    {
        GameObject obj = PoolManager.Instance.Get(_cardPrefab, _handRoot);
        CardView card = obj.GetComponent<CardView>();
        card.Initialize(instance);
        card.ApplyHandLayout();
        card.SetDraggable(true);
        card.SetAffordable(card.Data != null && _player.CanSpend(card.Data.cost));

        _hand.Add(card);
    }

    private void RebuildPileViews()
    {
        _drawPileView.Clear();
        foreach (CardInstance instance in _drawPile)
            if (instance?.SourceData != null)
                _drawPileView.Add(instance.SourceData);

        _discardPileView.Clear();
        foreach (CardInstance instance in _discardPile)
            if (instance?.SourceData != null)
                _discardPileView.Add(instance.SourceData);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    protected override void Dispose()
    {
        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnChainFinished -= OnChainFinished;
        if (_player != null)
            _player.OnCostChanged -= RefreshHandAffordability;
        OnHandChanged = null;
        base.Dispose();
    }
}