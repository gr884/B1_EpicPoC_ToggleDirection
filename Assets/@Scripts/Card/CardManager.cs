using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : SingletonBehaviour<CardManager>
{
    [Header("Refs")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private RectTransform _handRoot;
    [SerializeField] private Player _player;

    [Header("Deck")]
    [SerializeField] private List<CardData> _startingDeck = new();
    [SerializeField] private bool _shuffleOnReset = true;

    [Header("Cost")]
    [SerializeField, Min(0)] private int _maxCostPerTurn = 3;

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
    public int MaxCostPerTurn => _maxCostPerTurn;
    public int CurrentCost { get; private set; }

    public event Action OnHandChanged;
    public event Action<int, int> OnCostChanged;

    // ── 초기화 ─────────────────────────────────────────────

    public void Init()
    {
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        CurrentCost = _maxCostPerTurn;
        Debug.Log("[CardManager] Init");
    }

    private void OnChainFinished(ChainResult result)
    {
        UpdateHandDraggableState();
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
        List<CardData> drawn = DrawCards(count);
        foreach (CardData data in drawn)
            SpawnToHand(data);

        UpdateHandDraggableState();
        OnHandChanged?.Invoke();
    }

    public void DiscardAndDraw()
    {
        DiscardHand();
        DrawToHand(_player.HandSize);
    }

    public void DiscardHand()
    {
        foreach (CardView card in _hand)
        {
            if (card == null) continue;
            _discardPile.Add(card.Data);
            PoolManager.Instance.Return(card.gameObject);
        }

        _hand.Clear();
        UpdateHandDraggableState();
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
                PoolManager.Instance.Return(card.gameObject);
                SpawnToHand(recallData);
                UpdateHandDraggableState();
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

            _discardPile.Add(card.Data);
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
        if (!CanAfford(card.Data)) return false;

        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        _hand.Remove(card);
        SpendCost(card.Data.playCost);
        UpdateHandDraggableState();
        OnHandChanged?.Invoke();

        ChainExecutor.Instance.ExecuteFrom(card);
        return true;
    }

    // ── 내부 ───────────────────────────────────────────────

    private void SpawnToHand(CardData data)
    {
        GameObject obj = PoolManager.Instance.Get(_cardPrefab, _handRoot);
        CardView card = obj.GetComponent<CardView>();
        card.Initialize(data);
        card.SetDraggable(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        _hand.Add(card);
    }

    public void ResetTurnCost()
    {
        CurrentCost = _maxCostPerTurn;
        OnCostChanged?.Invoke(CurrentCost, _maxCostPerTurn);
        UpdateHandDraggableState();
    }

    private bool CanAfford(CardData data)
    {
        if (data == null) return false;
        return CurrentCost >= Mathf.Max(0, data.playCost);
    }

    private void SpendCost(int amount)
    {
        CurrentCost = Mathf.Max(0, CurrentCost - Mathf.Max(0, amount));
        OnCostChanged?.Invoke(CurrentCost, _maxCostPerTurn);
    }

    private void UpdateHandDraggableState()
    {
        bool canPlayCards = BattleManager.Instance != null
            && !BattleManager.Instance.IsProcessing
            && BattleManager.Instance.CurrentPhase == BattleManager.BattlePhase.PlayerTurn;

        foreach (CardView card in _hand)
        {
            if (card == null) continue;
            card.SetDraggable(canPlayCards && CanAfford(card.Data));
        }
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
        OnHandChanged = null;
        OnCostChanged = null;
        base.Dispose();
    }
}
