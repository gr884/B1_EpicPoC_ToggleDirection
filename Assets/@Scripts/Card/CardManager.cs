using System;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : SingletonBehaviour<CardManager>
{
    [Header("Refs")]
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private RectTransform _handRoot;
    private UserCardPool _cardPool;

    [Header("Hand Layout")]
    [SerializeField] private Vector2 _cardSize = new Vector2(80f, 80f);
    [SerializeField] private float _cardSpacing = 10f;
    [SerializeField] private int _maxHandSize = 8;

    private readonly List<CardView> _hand = new();
    private int _cardsPlacedThisTurn = 0;
    public int CardsPlacedThisTurn => _cardsPlacedThisTurn;

    public IReadOnlyList<CardView> Hand => _hand;
    public int HandCount => _hand.Count;
    public bool CanPlaceThisTurn
    {
        get
        {
            if (BattleManager.Instance.CurrentPhase == BattleManager.BattlePhase.FreePlace)
                return true;
            return _cardsPlacedThisTurn < BattleManager.Instance.MaxCardsPerTurn;
        }
    }

    public event Action OnHandChanged;

    public void Init()
    {
        _cardPool = UserCardPool.Instance;
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        BattleManager.Instance.OnPhaseChanged += OnPhaseChanged;
        Debug.Log("[CardManager] Init");
    }

    public void ResetTurnPlaceCount()
    {
        _cardsPlacedThisTurn = 0;
    }

    private void OnPhaseChanged(BattleManager.BattlePhase phase)
    {
        _cardsPlacedThisTurn = 0;
    }

    private void OnChainFinished(ChainResult result)
    {
        foreach (CardView card in _hand)
            if (card != null) card.SetDraggable(true);
    }

    // ── 전투 시작 시 덱 리셋 + 첫 손패 드로우 ───────────────

    public void StartBattleDraw()
    {
        Debug.Log("[CardManager] StartBattleDraw 호출");
        _cardPool.ResetForBattle();
        DrawToHand(_cardPool.InitialDrawCount);
    }

    // ── 드로우 ─────────────────────────────────────────────

    public void DrawToHand(int count)
    {
        int drawable = Mathf.Min(count, _maxHandSize - _hand.Count);
        if (drawable <= 0)
        {
            Debug.Log("[CardManager] 손패가 가득 찼습니다.");
            return;
        }

        List<CardData> drawn = _cardPool.DrawCards(drawable);
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
        if (!CanPlaceThisTurn)
        {
            Debug.Log("[CardManager] 이번 턴 배치 한도 초과");
            return false;
        }
        if (BattleManager.Instance.CurrentPhase == BattleManager.BattlePhase.Turn
            && !_hand.Contains(card)) return false;

        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        _hand.Remove(card);
        _cardsPlacedThisTurn++;
        ArrangeHand();
        OnHandChanged?.Invoke();

        ChainExecutor.Instance.ExecuteFrom(card);
        return true;
    }

    // ── 사이클 리셋 시 손패 버리기 ────────────────────────────

    public void DiscardHand()
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
        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnChainFinished -= OnChainFinished;
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        OnHandChanged = null;
        base.Dispose();
    }
}