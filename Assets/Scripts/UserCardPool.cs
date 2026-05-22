using System.Collections.Generic;
using UnityEngine;

public class UserCardPool : MonoBehaviour
{
    [Header("Deck")]
    [SerializeField] private List<CardData> startingDeck = new();
    [SerializeField] private bool shuffleOnReset = true;
    [SerializeField] private int drawCount = 5;

    private readonly List<CardData> drawPile = new();
    private readonly List<CardData> discardPile = new();

    public int DrawCount => drawCount;
    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;

    public void ResetForBattle()
    {
        drawPile.Clear();
        discardPile.Clear();

        if (startingDeck != null)
        {
            drawPile.AddRange(startingDeck);
        }

        if (shuffleOnReset)
        {
            Shuffle(drawPile);
        }

        Debug.Log($"UserCardPool: Reset deck. drawPile={drawPile.Count}");
    }

    public List<CardData> DrawCards(int count)
    {
        List<CardData> drawn = new();
        int targetCount = Mathf.Max(0, count);

        for (int i = 0; i < targetCount; i++)
        {
            if (drawPile.Count == 0)
            {
                RefillDrawPileFromDiscard();
            }

            if (drawPile.Count == 0)
            {
                break;
            }

            int lastIndex = drawPile.Count - 1;
            CardData card = drawPile[lastIndex];
            drawPile.RemoveAt(lastIndex);
            drawn.Add(card);
        }

        Debug.Log($"UserCardPool: Draw {drawn.Count}. drawPile={drawPile.Count}, discardPile={discardPile.Count}");
        return drawn;
    }

    public void Discard(CardData card)
    {
        if (card == null)
        {
            return;
        }

        discardPile.Add(card);
    }

    public void DiscardMany(IEnumerable<CardData> cards)
    {
        if (cards == null)
        {
            return;
        }

        foreach (CardData card in cards)
        {
            Discard(card);
        }

        Debug.Log($"UserCardPool: Discard pile now {discardPile.Count}.");
    }

    private void RefillDrawPileFromDiscard()
    {
        if (discardPile.Count == 0)
        {
            return;
        }

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
        Debug.Log($"UserCardPool: Shuffled discard into draw pile. drawPile={drawPile.Count}");
    }

    private static void Shuffle(List<CardData> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
        }
    }
}
