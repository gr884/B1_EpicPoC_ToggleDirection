using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : SingletonBehaviour<CardManager>
{
    [Header("Refs")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private RectTransform _handRoot;

    [Header("Hand Layout")]
    [SerializeField] private Vector2 _cardSize = new Vector2(80f, 80f);
    [SerializeField] private float _cardSpacing = 10f;

    private readonly List<CardView> _hand = new();
    private readonly List<CardData> _deck = new();

    public IReadOnlyList<CardView> Hand => _hand;
    public int HandCount => _hand.Count;

    public event Action OnHandChanged;

    public void Init()
    {
        Debug.Log("[CardManager] Init");
    }

    // ── 덱 세팅 ────────────────────────────────────────────

    public void SetupDeck(List<CardData> cards)
    {
        _deck.Clear();
        _deck.AddRange(cards);
        Debug.Log($"[CardManager] 덱 세팅 완료 ({_deck.Count}장)");
    }

    // ── 드로우 ─────────────────────────────────────────────

    public void DrawHand(int count)
    {
        ClearHand();

        for (int i = 0; i < count && _deck.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, _deck.Count);
            SpawnToHand(_deck[index]);
            // TODO: 덱빌딩 구조 확정 후 제거 방식 결정
        }

        ArrangeHand();
        OnHandChanged?.Invoke();
    }

    public void DrawCards(int count)
    {
        for (int i = 0; i < count && _deck.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, _deck.Count);
            SpawnToHand(_deck[index]);
        }

        ArrangeHand();
        OnHandChanged?.Invoke();
    }

    // ── 카드 배치 ──────────────────────────────────────────

    public bool TryPlaceCard(CardView card, GridSlot targetSlot)
    {
        if (card == null || targetSlot == null || !targetSlot.IsEmpty) return false;
        if (BattleManager.Instance.IsChainRunning) return false;

        // Phase2에서는 손패에서만 배치 가능
        if (BattleManager.Instance.CurrentPhase == BattleManager.Phase.Phase2
            && !_hand.Contains(card)) return false;

        targetSlot.AssignCard(card);
        _hand.Remove(card);
        ArrangeHand();
        OnHandChanged?.Invoke();

        ChainExecutor.Instance.ExecuteFrom(card);
        return true;
    }

    // ── 손패 정리 ──────────────────────────────────────────

    public void ClearHand()
    {
        foreach (CardView card in _hand)
            if (card != null)
                PoolManager.Instance.Return(card.gameObject);

        _hand.Clear();
        OnHandChanged?.Invoke();
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

    protected override void Dispose()
    {
        OnHandChanged = null;
        base.Dispose();
    }
}