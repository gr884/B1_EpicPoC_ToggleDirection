using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이그라운드 전용 DeckManager.
/// 인스펙터에 꽂은 CardData 리스트를 기반으로 패를 무제한으로 제공한다.
/// </summary>
public class Ryeol_DeckManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform handRoot;
    [SerializeField] private Card cardPrefab;

    [Header("Deck")]
    [SerializeField] private List<CardData> cardDataList = new();

    [Header("Hand Layout")]
    [SerializeField] private Vector2 handCardSize = new Vector2(100f, 150f);
    [SerializeField] private float handSpacing = 12f;

    // cardId 기준으로 패에 하나씩 유지
    private readonly Dictionary<string, Card> handSlots = new();

    // ── 외부 호출 ──────────────────────────────────────────

    /// <summary>cardDataList 기반으로 패를 채운다.</summary>
    public void BuildInfiniteDeck()
    {
        EnsureRefs();
        ClearHand();

        foreach (CardData data in cardDataList)
        {
            if (data == null) continue;
            Card card = SpawnHandCard(data);
            handSlots[data.cardId] = card;
        }

        ArrangeHand();
    }

    /// <summary>보드 위에 카드를 스폰한다 (스냅샷 복원용).</summary>
    public Card SpawnBoardCard(CardData data, Transform parent, bool startsActivated)
    {
        EnsureRefs();
        return CreateCard(data, parent, startsActivated, fillParent: true);
    }

    /// <summary>패에서 카드가 보드로 나갔음을 알린다. 같은 CardData로 새 카드를 보충한다.</summary>
    public void NotifyCardPlaced(Card card)
    {
        if (card?.Data == null) return;

        string id = card.Data.cardId;
        if (handSlots.TryGetValue(id, out Card old) && old == card)
        {
            handSlots.Remove(id);
        }

        Card newCard = SpawnHandCard(card.Data);
        handSlots[id] = newCard;
        ArrangeHand();
    }

    /// <summary>보드에서 빠진 카드를 패로 돌려보낸다.</summary>
    public void ReturnCardToHand(Card card)
    {
        if (card == null) return;

        CardData data = card.Data;
        Destroy(card.gameObject);

        if (data == null) return;

        // 이미 패에 있으면 중복 생성 안 함
        if (handSlots.ContainsKey(data.cardId)) return;

        Card newCard = SpawnHandCard(data);
        handSlots[data.cardId] = newCard;
        ArrangeHand();
    }

    // ── 내부 ───────────────────────────────────────────────

    private Card SpawnHandCard(CardData data)
    {
        return CreateCard(data, handRoot, startsActivated: false, fillParent: false);
    }

    private Card CreateCard(CardData data, Transform parent, bool startsActivated, bool fillParent)
    {
        if (cardPrefab == null || parent == null || data == null)
        {
            Debug.LogWarning("Ryeol_DeckManager: 카드 생성 불가 - 참조 누락.");
            return null;
        }

        Card instance = Instantiate(cardPrefab, parent);
        instance.Initialize(data, startsActivated);
        instance.SetDraggable(!fillParent);

        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            if (fillParent)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = handCardSize;
            }
        }

        return instance;
    }

    private void ArrangeHand()
    {
        List<Card> cards = new(handSlots.Values);
        int count = cards.Count;
        if (count == 0) return;

        float step = handCardSize.x + handSpacing;
        float start = -((count - 1) * 0.5f) * step;

        for (int i = 0; i < count; i++)
        {
            Card card = cards[i];
            if (card == null) continue;

            RectTransform rect = card.GetComponent<RectTransform>();
            if (rect == null) continue;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = handCardSize;
            rect.anchoredPosition = new Vector2(start + i * step, 0f);
        }
    }

    private void ClearHand()
    {
        handSlots.Clear();

        if (handRoot == null) return;
        for (int i = handRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(handRoot.GetChild(i).gameObject);
        }
    }

    private void EnsureRefs()
    {
        if (handRoot == null)
        {
            GameObject found = GameObject.Find("HandRoot");
            if (found != null) handRoot = found.GetComponent<RectTransform>();
        }
    }
}