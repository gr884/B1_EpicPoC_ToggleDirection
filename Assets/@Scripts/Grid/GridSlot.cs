using UnityEngine;
using UnityEngine.EventSystems;

public class GridSlot : MonoBehaviour, IDropHandler
{
    [Header("Highlight")]
    [SerializeField] private GameObject _highlightOverlay;
    [SerializeField] private GameObject _DmgTotemBorderOverlay;
    [SerializeField] private GameObject _DefTotemBorderOverlay;

    public Vector2Int Position { get; private set; }
    public CardView OccupiedCard { get; private set; }
    public bool IsEmpty => OccupiedCard == null;

    public void Setup(Vector2Int position)
    {
        Position = position;
        OccupiedCard = null;
        SetHighlight(false);
        SetTotemBorder(false, false);
    }

    public void AssignCard(CardView card)
    {
        if (GridManager.Instance != null && card != null)
            GridManager.Instance.PlaceCard(card, this);
        else
            SetOccupant(card);
    }

    public void ClearCard()
    {
        if (GridManager.Instance != null && OccupiedCard != null)
            GridManager.Instance.RemoveCard(OccupiedCard);
        else
            SetOccupant(null);
    }

    public void SetOccupant(CardView card)
    {
        OccupiedCard = card;
        SetHighlight(false);
        SetTotemBorder(false, false);
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
}
