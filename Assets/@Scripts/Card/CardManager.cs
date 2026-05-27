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

    [Header("Hand Layout")]
    [SerializeField] private Vector2 _cardSize = new Vector2(80f, 80f);
    [SerializeField] private float _cardSpacing = 10f;

    // ── 덱 상태 ────────────────────────────────────────────
    private readonly List<CardData> _drawPile = new();
    public int DrawPileCount => _drawPile.Count;

    // ── 손패 상태 ──────────────────────────────────────────
    private readonly List<CardView> _hand = new();
    public IReadOnlyList<CardView> Hand => _hand;
    public int HandCount => _hand.Count;

    public event Action OnHandChanged;

    // ── 초기화 ─────────────────────────────────────────────

    public void Init()
    {
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        Debug.Log("[CardManager] Init");
    }

    private void OnChainFinished(ChainResult result)
    {
        foreach (CardView card in _hand)
            if (card != null) card.SetDraggable(true);
    }

    // ── 덱 관리 ────────────────────────────────────────────

    public void ResetDeck()
    {
        _drawPile.Clear();
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
        List<CardData> inUse = new();

        foreach (CardView card in _hand)
            if (card?.Data != null) inUse.Add(card.Data);

        foreach (var slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard?.Data != null && !slot.OccupiedCard.IsEnemy)
                inUse.Add(slot.OccupiedCard.Data);

        List<CardData> available = new(_startingDeck);
        foreach (CardData used in inUse)
            available.Remove(used);

        if (available.Count == 0)
        {
            Debug.Log("[CardManager] 재활용할 카드 없음");
            return;
        }

        Shuffle(available);
        _drawPile.AddRange(available);
        Debug.Log($"[CardManager] 드로우파일 재구성 — {_drawPile.Count}장");
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

        ArrangeHand();
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
            if (card != null)
                PoolManager.Instance.Return(card.gameObject);

        _hand.Clear();
        OnHandChanged?.Invoke();
    }

    // ── 카드 배치 ──────────────────────────────────────────

    public bool TryPlaceCard(CardView card, GridSlot targetSlot)
    {
        if (card == null || targetSlot == null || !targetSlot.IsEmpty) return false;
        if (BattleManager.Instance.IsProcessing) return false;
        if (BattleManager.Instance.CurrentPhase != BattleManager.BattlePhase.PlayerTurn) return false;
        if (!_hand.Contains(card)) return false;

        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        _hand.Remove(card);
        ArrangeHand();
        OnHandChanged?.Invoke();

        ChainExecutor.Instance.ExecuteFrom(card);
        return true;
    }

    // ── 내부 ───────────────────────────────────────────────

    private void SpawnToHand(CardData data)
    {
        GameObject obj = PoolManager.Instance.Get(_cardPrefab, Vector3.zero, _handRoot);
        CardView card = obj.GetComponent<CardView>();
        card.Initialize(data);
        card.SetDraggable(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = _cardSize;
        }

        _hand.Add(card);
    }

    private void ArrangeHand()
    {
        int count = _hand.Count;
        if (count == 0) return;

        float step = _cardSize.x + _cardSpacing;
        float start = -((count - 1) * 0.5f) * step;

        for (int i = 0; i < count; i++)
        {
            if (_hand[i] == null) continue;
            RectTransform rect = _hand[i].GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = new Vector2(start + i * step, 0f);
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
        base.Dispose();
    }
}