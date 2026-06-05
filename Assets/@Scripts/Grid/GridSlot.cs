using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridSlot : MonoBehaviour, IDropHandler
{
    [Header("Highlight")]
    [SerializeField] private GameObject _highlightOverlay;

    public Vector2Int Position { get; private set; }
    public CardView OccupiedCard { get; private set; }
    public bool IsEmpty => OccupiedCard == null;

    public void Setup(Vector2Int position)
    {
        Position = position;
        OccupiedCard = null;
        SetHighlight(false);
    }

    public void AssignCard(CardView card)
    {
        OccupiedCard = card;
        if (card != null)
            card.SetPlaced(this);
        SetHighlight(false);
    }

    public void ClearCard()
    {
        if (OccupiedCard != null)
            OccupiedCard.SetPlaced(null);
        OccupiedCard = null;
    }

    public void SetHighlight(bool highlight)
    {
        if (_highlightOverlay != null)
            _highlightOverlay.SetActive(highlight);
    }

    // ── UI 이벤트 ──────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        CardDragHandler drag = eventData.pointerDrag?.GetComponent<CardDragHandler>();
        if (drag == null || drag.Card == null) return;
        if (CardManager.Instance == null || !CardManager.Instance.CanAcceptPlayerCardInput) return;

        bool placed = CardManager.Instance.TryPlaceCard(drag.Card, this);
        if (placed)
            drag.CommitDrop(transform);
    }
}
