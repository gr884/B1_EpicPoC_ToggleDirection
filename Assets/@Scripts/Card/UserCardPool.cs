using System.Collections.Generic;
using UnityEngine;

public class UserCardPool : SingletonBehaviour<UserCardPool>
{
    [Header("Deck")]
    [SerializeField] private List<CardData> _startingDeck = new();
    [SerializeField] private bool _shuffleOnReset = true;
    [SerializeField] private int _initialDrawCount = 5;
    [SerializeField] private int _turnDrawCount = 1;

    private readonly List<CardData> _drawPile = new();

    public int InitialDrawCount => _initialDrawCount;
    public int TurnDrawCount => _turnDrawCount;
    public int DrawPileCount => _drawPile.Count;

    public void Init()
    {
        Debug.Log("[UserCardPool] Init");
    }

    public void ResetForBattle()
    {
        _drawPile.Clear();

        if (_startingDeck != null)
            _drawPile.AddRange(_startingDeck);

        if (_shuffleOnReset)
            Shuffle(_drawPile);

        Debug.Log($"[UserCardPool] 덱 리셋 — {_drawPile.Count}장");
    }

    public List<CardData> DrawCards(int count)
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

        Debug.Log($"[UserCardPool] {drawn.Count}장 드로우 — 남은 드로우파일: {_drawPile.Count}");
        return drawn;
    }

    // ── 내부 ───────────────────────────────────────────────

    private void RefillDrawPile()
    {
        // 전체 덱에서 손패 + 그리드에 있는 카드를 제외한 나머지로 재구성
        List<CardData> inUse = new();

        // 손패에 있는 카드
        foreach (CardView card in CardManager.Instance.Hand)
            if (card?.Data != null) inUse.Add(card.Data);

        // 그리드에 있는 카드
        foreach (var slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard?.Data != null && !slot.OccupiedCard.IsEnemy)
                inUse.Add(slot.OccupiedCard.Data);

        // 전체 덱에서 사용 중인 카드 제외
        List<CardData> available = new(_startingDeck);
        foreach (CardData used in inUse)
            available.Remove(used);

        if (available.Count == 0)
        {
            Debug.Log("[UserCardPool] 재활용할 카드 없음");
            return;
        }

        Shuffle(available);
        _drawPile.AddRange(available);
        Debug.Log($"[UserCardPool] 드로우파일 재구성 — {_drawPile.Count}장");
    }

    private static void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}