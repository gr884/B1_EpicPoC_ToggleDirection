using UnityEngine;
using UnityEngine.EventSystems;

public class GridSlot : MonoBehaviour, IDropHandler
{
    public Vector2Int Position { get; private set; }
    public CardView OccupiedCard { get; private set; }
    public bool IsEmpty => OccupiedCard == null;

    public void Setup(Vector2Int position)
    {
        Position = position;
        OccupiedCard = null;
    }

    public void AssignCard(CardView card)
    {
        OccupiedCard = card;
        if (card != null)
            card.SetPlaced(this);
    }

    public void ClearCard()
    {
        if (OccupiedCard != null)
            OccupiedCard.SetPlaced(null);
        OccupiedCard = null;
    }

    // ── UI 이벤트 ──────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        CardDragHandler drag = eventData.pointerDrag?.GetComponent<CardDragHandler>();
        if (drag == null || drag.Card == null) return;

        bool placed = CardManager.Instance.TryPlaceCard(drag.Card, this);
        if (placed)
            drag.CommitDrop(transform);
    }
}