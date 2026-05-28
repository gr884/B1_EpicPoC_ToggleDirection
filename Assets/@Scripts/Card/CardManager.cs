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

    public IReadOnlyList<CardView> Hand => _hand;
    public int HandCount => _hand.Count;

    public bool CanPlaceCard =>
        BattleManager.Instance.CurrentTurn == BattleManager.TurnState.PlayerTurn &&
        !BattleManager.Instance.IsChainRunning;

    public event Action OnHandChanged;

    public void Init()
    {
        _cardPool = UserCardPool.Instance;
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        Debug.Log("[CardManager] Init");
    }

    private void OnChainFinished(ChainResult result)
    {
        // 체인 종료 후 손패 카드 드래그 재활성화
        foreach (CardView card in _hand)
            if (card != null) card.SetDraggable(true);
    }

    // ── 전투 시작 시 덱 리셋 + 첫 손패 드로우 ───────────────

    public void StartBattleDraw()
    {
        _cardPool.ResetForBattle();
        DrawToHand(_cardPool.InitialDrawCount);
    }

    // ── 드로우 ─────────────────────────────────────────────

    public void DrawToHand(int count, bool ignoreHandLimit = false)
    {
        int drawable = ignoreHandLimit ? count : Mathf.Min(count, _maxHandSize - _hand.Count);
        if (drawable <= 0)
        {
            if (!ignoreHandLimit) Debug.Log("[CardManager] 손패가 가득 찼습니다.");
            return;
        }

        List<CardInstance> drawn = _cardPool.DrawCards(drawable);
        foreach (CardInstance instance in drawn)
            SpawnToHand(instance);

        ArrangeHand();
        OnHandChanged?.Invoke();
    }

    // ── 카드 배치 — 활성 상태로 배치 후 즉시 신호 전달 ──────

    public bool TryPlaceCard(CardView card, GridSlot targetSlot)
    {
        if (card == null || targetSlot == null || !targetSlot.IsEmpty) return false;
        if (!CanPlaceCard) return false;
        if (!_hand.Contains(card)) return false;

        targetSlot.AssignCard(card);
        card.SetActivated(true);
        card.SetDraggable(false);
        _hand.Remove(card);
        ArrangeHand();
        OnHandChanged?.Invoke();

        BattleManager.Instance.TriggerChainFromCard(card);

        return true;
    }

    // ── 손패 버리기 ────────────────────────────────────────

    public void DiscardHand()
    {
        foreach (CardView card in _hand)
            if (card != null)
                PoolManager.Instance.Return(card.gameObject);

        _hand.Clear();
        OnHandChanged?.Invoke();
    }

    // ── 내부 ───────────────────────────────────────────────

    private void SpawnToHand(CardInstance instance)
    {
        GameObject obj = PoolManager.Instance.Get(_cardPrefab, Vector3.zero, _handRoot);
        CardView card = obj.GetComponent<CardView>();
        card.Initialize(instance.Data);
        card.SetRuntimeDirections(instance.Directions);
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
        OnHandChanged = null;
        base.Dispose();
    }
}
