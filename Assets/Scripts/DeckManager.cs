using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform handRoot;
    [SerializeField] private Card cardPrefab;
    [SerializeField] private Vector2 handCardSize = new Vector2(120f, 180f);
    [SerializeField] private float handSpacing = 16f;

    private readonly List<Card> handCards = new();

    public int HandCount => handCards.Count;

    public void BuildHand(IEnumerable<CardData> handData)
    {
        EnsureRefs();
        SyncHandCardSizeToBoard();
        ConfigureHandLayout();
        ClearHand();

        if (handData == null)
        {
            return;
        }

        foreach (CardData cardData in handData)
        {
            Card card = CreateCard(cardData, handRoot, false, false);
            if (card != null)
            {
                handCards.Add(card);
            }
        }

        ArrangeHandCardsCentered();
    }

    public Card SpawnBoardCard(CardData data, Transform parent, bool startsActivated)
    {
        EnsureRefs();
        return CreateCard(data, parent, startsActivated, true);
    }

    public void RemoveFromHand(Card card)
    {
        handCards.Remove(card);
    }

    public List<CardData> CaptureHandState()
    {
        List<CardData> snapshot = new();
        for (int i = 0; i < handCards.Count; i++)
        {
            Card card = handCards[i];
            if (card == null || card.Data == null)
            {
                continue;
            }

            snapshot.Add(card.Data);
        }

        return snapshot;
    }

    private void EnsureRefs()
    {
        if (handRoot == null)
        {
            GameObject found = GameObject.Find("HandRoot");
            if (found != null)
            {
                handRoot = found.GetComponent<RectTransform>();
            }
        }
    }

    private Card CreateCard(CardData data, Transform parent, bool startsActivated, bool fillParent)
    {
        if (cardPrefab == null || parent == null || data == null)
        {
            Debug.LogWarning("DeckManager: Cannot spawn card. Check prefab/parent/data.");
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
                rect.localScale = Vector3.one;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = handCardSize;
                rect.localScale = Vector3.one;
            }
        }

        return instance;
    }

    private void ConfigureHandLayout()
    {
        if (handRoot == null)
        {
            return;
        }

        HorizontalLayoutGroup layout = handRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.enabled = false;
        }
    }

    private void ClearHand()
    {
        handCards.Clear();

        if (handRoot == null)
        {
            return;
        }

        for (int i = handRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(handRoot.GetChild(i).gameObject);
        }
    }

    private void ArrangeHandCardsCentered()
    {
        int count = handCards.Count;
        if (count == 0)
        {
            return;
        }

        float step = handCardSize.x + handSpacing;
        float start = -((count - 1) * 0.5f) * step;

        for (int i = 0; i < count; i++)
        {
            Card card = handCards[i];
            if (card == null)
            {
                continue;
            }

            RectTransform rect = card.GetComponent<RectTransform>();
            if (rect == null)
            {
                continue;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = handCardSize;
            rect.anchoredPosition = new Vector2(start + (i * step), 0f);
            rect.localScale = Vector3.one;
        }
    }

    private void SyncHandCardSizeToBoard()
    {
        BoardSlot sampleSlot = FindFirstObjectByType<BoardSlot>();
        if (sampleSlot == null)
        {
            return;
        }

        RectTransform slotRect = sampleSlot.GetComponent<RectTransform>();
        if (slotRect == null)
        {
            return;
        }

        Vector2 size = slotRect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = slotRect.sizeDelta;
        }

        if (size.x > 0f && size.y > 0f)
        {
            handCardSize = size;
        }
    }
}
