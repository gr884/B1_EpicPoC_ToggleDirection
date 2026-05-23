using System.Collections.Generic;
using UnityEngine;

public class DeckManager : SingletonBehaviour<DeckManager>
{
    [Header("Refs")]
    [SerializeField] private Card _cardPrefab;

    [Header("Deck")]
    [SerializeField] private List<CardData> _cardDataList = new();

    [Header("Hand Layout")]
    [SerializeField] private Vector2 _handCenter = new Vector2(0f, -4f);
    [SerializeField] private float _handSpacing = 1.4f;

    private readonly Dictionary<string, Card> _handSlots = new();

    public void Init()
    {
        Debug.Log("[DeckManager] Init");
    }

    public void BuildInfiniteDeck()
    {
        ClearHand();

        foreach (CardData data in _cardDataList)
        {
            if (data == null) continue;
            Card card = SpawnHandCard(data);
            _handSlots[data.cardId] = card;
        }

        ArrangeHand();
        Debug.Log($"[DeckManager] 패 생성 완료 {_handSlots.Count}장");
    }

    public Card SpawnBoardCard(CardData data, Vector3 worldPos, bool startsActivated)
    {
        if (_cardPrefab == null || data == null) return null;

        Card instance = Instantiate(_cardPrefab, worldPos, Quaternion.identity);
        instance.Initialize(data, startsActivated);
        instance.SetDraggable(false);
        return instance;
    }

    public void NotifyCardPlaced(Card card)
    {
        if (card?.Data == null) return;

        string id = card.Data.cardId;
        if (_handSlots.TryGetValue(id, out Card old) && old == card)
            _handSlots.Remove(id);

        Card newCard = SpawnHandCard(card.Data);
        _handSlots[id] = newCard;
        ArrangeHand();
    }

    public void ReturnCardToHand(Card card)
    {
        if (card == null) return;

        CardData data = card.Data;
        Destroy(card.gameObject);

        if (data == null) return;
        if (_handSlots.ContainsKey(data.cardId)) return;

        Card newCard = SpawnHandCard(data);
        _handSlots[data.cardId] = newCard;
        ArrangeHand();
    }

    private Card SpawnHandCard(CardData data)
    {
        if (_cardPrefab == null || data == null) return null;

        Card instance = Instantiate(_cardPrefab, _handCenter, Quaternion.identity, transform);
        instance.Initialize(data, false);
        instance.SetDraggable(true);
        return instance;
    }

    private void ArrangeHand()
    {
        List<Card> cards = new(_handSlots.Values);
        int count = cards.Count;
        if (count == 0) return;

        float start = -((count - 1) * 0.5f) * _handSpacing;

        for (int i = 0; i < count; i++)
        {
            if (cards[i] == null) continue;
            cards[i].transform.position = new Vector3(
                _handCenter.x + start + i * _handSpacing,
                _handCenter.y,
                0f
            );
        }
    }

    private void ClearHand()
    {
        _handSlots.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    protected override void Dispose()
    {
        base.Dispose();
    }
}