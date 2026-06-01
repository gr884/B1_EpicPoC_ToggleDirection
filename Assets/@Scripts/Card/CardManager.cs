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
    private readonly List<CardData> _drawPile = new();
    private readonly List<CardData> _discardPile = new();
    public int DrawPileCount => _drawPile.Count;
    public int DiscardPileCount => _discardPile.Count;
    public IReadOnlyList<CardData> DrawPile => _drawPile;
    public IReadOnlyList<CardData> DiscardPile => _discardPile;

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
                card.SetAffordable(_player.CanSpend(card.Data.cost));
    }

    // ── 덱 관리 ────────────────────────────────────────────

    public void ResetDeck()
    {
        _drawPile.Clear();
        _discardPile.Clear();
        if (_startingDeck != null)
            _drawPile.AddRange(_startingDeck);
        if (_shuffleOnReset)
            Shuffle(_drawPile);
        Debug.Log($"[CardManager] 덱 리셋 — {_drawPile.Count}장");
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
        _drawPile.Insert(index, curseCard);
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

    private List<CardData> DrawCards(int count)
    {
        List<CardData> drawn = new();
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

        List<CardData> drawn = DrawCards(drawCount);
        foreach (CardData data in drawn)
            SpawnToHand(data);

        _flightEffectPlayer?.PlayDrawToHand(drawn.Count);

        // 손패 상한 초과분은 무덤으로
        if (overflow > 0)
        {
            List<CardData> discarded = DrawCards(overflow);
            foreach (CardData data in discarded)
                _discardPile.Add(data);
            Debug.Log($"[CardManager] 손패 상한 초과 — {overflow}장 무덤으로");
        }

        OnHandChanged?.Invoke();
    }

    public void DiscardAndDraw()
    {
        DiscardHand();
        DrawToHand(_player.HandSize);
        TutorialManager.Instance?.OnHandDrawn();
    }

    public void DiscardHand()
    {
        foreach (CardView card in _hand)
        {
            if (card == null) continue;
            _discardPile.Add(card.Data);
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

        switch (card.Data.recallDestination)
        {
            case RecallDestination.Hand:
                CardData recallData = card.Data;
                _flightEffectPlayer?.PlayRecallToHandFrom(card.transform);
                PoolManager.Instance.Return(card.gameObject);
                SpawnToHand(recallData);
                OnHandChanged?.Invoke();
                break;

            case RecallDestination.DrawPileTop:
                _drawPile.Add(card.Data);
                PoolManager.Instance.Return(card.gameObject);
                break;

            case RecallDestination.DrawPile:
                int index = UnityEngine.Random.Range(0, _drawPile.Count + 1);
                _drawPile.Insert(index, card.Data);
                PoolManager.Instance.Return(card.gameObject);
                break;
        }

        Debug.Log($"[CardManager] 카드 회수 — {card.Data.displayName} → {card.Data.recallDestination}");
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
                Debug.Log($"[CardManager] 보존 — {card.Data.displayName} 그리드 유지 (남은 스택: {card.PreserveStack})");
                continue;
            }

            _discardPile.Add(card.Data);
            _flightEffectPlayer?.PlayDiscardFrom(card.transform);
            slot.ClearCard();
            PoolManager.Instance.Return(card.gameObject);
        }

        Debug.Log("[CardManager] 그리드 플레이어 카드 → 버린 파일");
    }

    // ── 카드 배치 ──────────────────────────────────────────

    public bool TryPlaceCard(CardView card, GridSlot targetSlot)
    {
        if (card == null || targetSlot == null || !targetSlot.IsEmpty) return false;
        if (BattleManager.Instance.IsProcessing) return false;
        if (BattleManager.Instance.CurrentPhase != BattleManager.BattlePhase.PlayerTurn) return false;
        if (!_hand.Contains(card)) return false;
        if (card.Data != null && card.Data.isUnplayable) return false;
        if (GameManager.Instance.CurrentState == GameManager.GameState.Tutorial
            && !TutorialManager.Instance.CanPlaceCard(card, targetSlot)) return false;
        if (!_player.SpendCost(card.Data.cost)) return false;

        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        _hand.Remove(card);
        OnHandChanged?.Invoke();

        TutorialManager.Instance?.OnCardPlaced(card);
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
            _drawPile.Add(cards[i]);
    }

    // ── 유저 회수 액션 ─────────────────────────────────────

    public bool TryRecallCard(CardView card)
    {
        if (card == null || card.IsEnemy) return false;
        if (card.CurrentSlot == null) return false;
        if (BattleManager.Instance.IsProcessing) return false;
        if (BattleManager.Instance.CurrentPhase != BattleManager.BattlePhase.PlayerTurn) return false;
        if (!_player.SpendCost(1)) return false;

        GridSlot slot = card.CurrentSlot;
        slot.ClearCard();

        CardData recallData = card.Data;
        _flightEffectPlayer?.PlayRecallToHandFrom(card.transform);
        PoolManager.Instance.Return(card.gameObject);
        SpawnToHand(recallData);
        OnHandChanged?.Invoke();

        Debug.Log($"[CardManager] 유저 회수 — {recallData.displayName} → 손패");
        return true;
    }

    // ── 내부 ───────────────────────────────────────────────

    private void SpawnToHand(CardData data)
    {
        GameObject obj = PoolManager.Instance.Get(_cardPrefab, _handRoot);
        CardView card = obj.GetComponent<CardView>();
        card.Initialize(data);
        card.SetDraggable(true);
        card.SetAffordable(_player.CanSpend(data.cost));

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        _hand.Add(card);
    }

    private static void Shuffle(List<CardData> list)
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