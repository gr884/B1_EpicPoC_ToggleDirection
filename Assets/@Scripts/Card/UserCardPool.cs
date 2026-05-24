using System.Collections.Generic;
using UnityEngine;

public class UserCardPool : MonoBehaviour
{
    [Header("Deck")]
    [SerializeField] private List<CardData> _startingDeck = new();
    [SerializeField] private bool _shuffleOnReset = true;
    [SerializeField] private int _drawCount = 5;

    private readonly List<CardData> _drawPile = new();
    private readonly List<CardData> _discardPile = new();

    public int DrawCount => _drawCount;
    public int DrawPileCount => _drawPile.Count;
    public int DiscardPileCount => _discardPile.Count;

    public void ResetForBattle()
    {
        _drawPile.Clear();
        _discardPile.Clear();

        if (_startingDeck != null)
            _drawPile.AddRange(_startingDeck);

        if (_shuffleOnReset)
            Shuffle(_drawPile);

        Debug.Log($"[UserCardPool] 덱 리셋 — 드로우파일: {_drawPile.Count}장");
    }

    public List<CardData> DrawCards(int count)
    {
        List<CardData> drawn = new();

        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            if (_drawPile.Count == 0)
                RefillFromDiscard();

            if (_drawPile.Count == 0) break;

            int last = _drawPile.Count - 1;
            drawn.Add(_drawPile[last]);
            _drawPile.RemoveAt(last);
        }

        Debug.Log($"[UserCardPool] {drawn.Count}장 드로우 — 남은 드로우파일: {_drawPile.Count}");
        return drawn;
    }

    public void DiscardMany(IEnumerable<CardData> cards)
    {
        if (cards == null) return;
        foreach (CardData card in cards)
            if (card != null) _discardPile.Add(card);
    }

    // ── 내부 ───────────────────────────────────────────────

    private void RefillFromDiscard()
    {
        if (_discardPile.Count == 0) return;

        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);

        Debug.Log($"[UserCardPool] 버리기파일 → 드로우파일 {_drawPile.Count}장");
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