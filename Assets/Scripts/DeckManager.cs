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

    [Header("Sizing")]
    [SerializeField] private bool fitHandCardsToPanelHeight = true;
    [SerializeField] private float panelVerticalPadding;

    private readonly List<Card> handCards = new();
    private Vector2 currentHandCardSize;

    public int HandCount => handCards.Count;

    public IReadOnlyList<Card> HandCards => handCards;

    public void BuildHand(IEnumerable<CardData> handData)
    {
        EnsureRefs();
        ConfigureHandLayout();
        ClearHand();
        currentHandCardSize = CalculateHandCardSizeForPanelHeight();

        if (handData == null)
        {
            return;
        }

        foreach (CardData cardData in handData)
        {
            Card card = CreateCard(cardData, handRoot, CardTeam.Ally, false, false);
            if (card != null)
            {
                handCards.Add(card);
            }
        }

        ArrangeHandCardsCentered();
    }

    public Card SpawnBoardCard(CardData data, Transform parent, CardTeam team, bool startsActivated)
    {
        EnsureRefs();
        return CreateCard(data, parent, team, startsActivated, true);
    }

    public void RemoveFromHand(Card card)
    {
        handCards.Remove(card);
        ArrangeHandCardsCentered();
    }

    public void ClearCurrentHand()
    {
        ClearHand();
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
            if (found == null)
            {
                found = GameObject.Find("LowHandRoot");
            }

            if (found != null)
            {
                handRoot = found.GetComponent<RectTransform>();
            }
        }
    }

    private Card CreateCard(CardData data, Transform parent, CardTeam team, bool startsActivated, bool fillParent)
    {
        if (cardPrefab == null || parent == null || data == null)
        {
            Debug.LogWarning("DeckManager: Cannot spawn card. Check prefab/parent/data.");
            return null;
        }

        Card instance = Instantiate(cardPrefab, parent);
        instance.Initialize(data, team, startsActivated);
        instance.SetDraggable(!fillParent && team == CardTeam.Ally);

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
                rect.sizeDelta = currentHandCardSize;
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
            // Use explicit centered positioning to avoid layout-driven drift/overlap.
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
            Transform child = handRoot.GetChild(i);
            if (child.GetComponent<Card>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void ArrangeHandCardsCentered()
    {
        int count = handCards.Count;
        if (count == 0)
        {
            return;
        }

        if (currentHandCardSize == Vector2.zero)
        {
            currentHandCardSize = CalculateHandCardSizeForPanelHeight();
        }

        float step = currentHandCardSize.x + handSpacing;
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
            rect.sizeDelta = currentHandCardSize;
            rect.anchoredPosition = new Vector2(start + (i * step), 0f);
        }
    }

    private Vector2 CalculateHandCardSizeForPanelHeight()
    {
        if (!fitHandCardsToPanelHeight || handRoot == null || handCardSize.y <= 0f)
        {
            return handCardSize;
        }

        RectTransform panel = FindPanelRect(handRoot);
        if (panel == null)
        {
            return handCardSize;
        }

        float panelHeight = GetRectHeight(panel);
        if (panelHeight <= 0f)
        {
            return handCardSize;
        }

        float targetHeight = Mathf.Max(1f, panelHeight - Mathf.Max(0f, panelVerticalPadding));
        float aspect = handCardSize.x / handCardSize.y;
        return new Vector2(targetHeight * aspect, targetHeight);
    }

    private static RectTransform FindPanelRect(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == "Panel")
            {
                return child.GetComponent<RectTransform>();
            }
        }

        return root;
    }

    private static float GetRectHeight(RectTransform rectTransform)
    {
        float height = rectTransform.rect.height;
        if (height > 0f)
        {
            return height;
        }

        return Mathf.Abs(rectTransform.sizeDelta.y);
    }
}
