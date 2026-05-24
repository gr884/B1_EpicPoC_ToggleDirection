using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : SingletonBehaviour<CardManager>
{
    [Header("Refs")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private RectTransform _handRoot;
    [SerializeField] private UserCardPool _cardPool;

    [Header("Hand Layout")]
    [SerializeField] private Vector2 _cardSize = new Vector2(80f, 80f);
    [SerializeField] private float _cardSpacing = 10f;

    private readonly List<CardView> _hand = new();

    public IReadOnlyList<CardView> Hand => _hand;
    public int HandCount => _hand.Count;

    public event Action OnHandChanged;

    public void Init()
    {
        if (_cardPool == null)
            _cardPool = GetComponentInChildren<UserCardPool>();

        Debug.Log("[CardManager] Init");
    }

    // ── 전투 시작 시 덱 리셋 + 첫 손패 드로우 ───────────────

    public void StartBattleDraw()
    {
        _cardPool.ResetForBattle();
        DrawToHand(_cardPool.DrawCount);
    }

    // ── 드로우 ─────────────────────────────────────────────

    public void DrawToHand(int count)
    {
        List<CardData> drawn = _cardPool.DrawCards(count);
        foreach (CardData data in drawn)
            SpawnToHand(data);

        ArrangeHand();
        OnHandChanged?.Invoke();
    }

    // ── 카드 배치 ──────────────────────────────────────────

    public bool TryPlaceCard(CardView card, GridSlot targetSlot)
    {
        if (card == null || targetSlot == null || !targetSlot.IsEmpty) return false;
        if (BattleManager.Instance.IsChainRunning) return false;

        if (BattleManager.Instance.CurrentPhase == BattleManager.Phase.Phase2
            && !_hand.Contains(card)) return false;

        targetSlot.AssignCard(card);
        _hand.Remove(card);
        ArrangeHand();
        OnHandChanged?.Invoke();

        ChainExecutor.Instance.ExecuteFrom(card);
        return true;
    }

    // ── 사이클 리셋 시 손패 버리기 ────────────────────────────

    public void DiscardHand()
    {
        List<CardData> discarded = new();
        foreach (CardView card in _hand)
        {
            if (card?.Data != null)
                discarded.Add(card.Data);
            if (card != null)
                PoolManager.Instance.Return(card.gameObject);
        }

        _hand.Clear();
        _cardPool.DiscardMany(discarded);
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