using UnityEngine;
using UnityEngine.EventSystems;

public class GridSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Highlight")]
    [SerializeField] private GameObject _highlightOverlay;
    [SerializeField] private GameObject _DmgTotemBorderOverlay;
    [SerializeField] private GameObject _DefTotemBorderOverlay;

    public Vector2Int Position { get; private set; }
    public CardView OccupiedCard { get; private set; }
    public RelicView OccupiedRelic { get; private set; }
    public bool IsEmpty => OccupiedCard == null;
    public bool HasAnyOccupant => OccupiedCard != null || OccupiedRelic != null;
    public bool CanPlaceCardAt() => OccupiedCard == null && OccupiedRelic == null;
    public bool CanPlaceRelicAt() => OccupiedCard == null && OccupiedRelic == null;
    public IGridChainNode GetChainNodeAt() => OccupiedCard != null ? OccupiedCard : OccupiedRelic;

    public void Setup(Vector2Int position)
    {
        Position = position;
        OccupiedCard = null;
        OccupiedRelic = null;
        SetHighlight(false);
        SetTotemBorder(false, false);
    }

    public void AssignCard(CardView card)
    {
        OccupiedCard = card;
        if (card != null)
            card.SetPlaced(this);
        SetHighlight(false);
        SetTotemBorder(false, false);
    }

    public void ClearCard()
    {
        if (OccupiedCard != null)
            OccupiedCard.SetPlaced(null);
        OccupiedCard = null;
    }

    public void AssignRelic(RelicView relic)
    {
        OccupiedRelic = relic;
        SetHighlight(false);
    }

    public void ClearRelic()
    {
        OccupiedRelic = null;
    }

    public void SetHighlight(bool highlight)
    {
        if (_highlightOverlay != null)
            _highlightOverlay.SetActive(highlight);
    }

    //* 토템으로 인한 영역을 show에 따라 켜고 끄기
    public void SetTotemBorder(bool showDmg, bool showDef)
    {
        if (_DmgTotemBorderOverlay != null)
        {
            _DmgTotemBorderOverlay.SetActive(showDmg);
        }

        if (_DefTotemBorderOverlay != null)
        {
            _DefTotemBorderOverlay.SetActive(showDef);
        }
    }

    // ── UI 이벤트 ──────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        CardDragHandler drag = eventData.pointerDrag?.GetComponent<CardDragHandler>();
        if (drag == null || drag.Card == null) return;
        if (CardManager.Instance == null) return;

        bool isChainExecuting = ChainExecutor.Instance != null && ChainExecutor.Instance.IsExecuting;
        bool placed = CardManager.Instance.TryPlaceCard(drag.Card, this);
        if (placed)
        {
            if (isChainExecuting)
                drag.CommitPendingDrop(transform);
            else
                drag.CommitDrop(transform);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        RelicPlacementController.Instance?.HandleSlotHovered(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        RelicPlacementController.Instance?.HandleSlotClicked(this);
    }
}
