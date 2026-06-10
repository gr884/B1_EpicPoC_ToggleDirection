using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuGridSlot : MonoBehaviour, IDropHandler
{
    [Header("Highlight")]
    [SerializeField] private GameObject _centerMarker;

    private MainMenuGridController _grid;

    public Vector2Int Position { get; private set; }
    public bool IsCenterSlot { get; private set; }
    public MainMenuCardView OccupiedCard { get; private set; }
    public bool IsEmpty => OccupiedCard == null;

    public void Setup(Vector2Int position, bool isCenterSlot, MainMenuGridController grid)
    {
        Position = position;
        IsCenterSlot = isCenterSlot;
        _grid = grid;
        OccupiedCard = null;

        if (_centerMarker != null)
            _centerMarker.SetActive(isCenterSlot);
    }

    public void AssignCard(MainMenuCardView card)
    {
        OccupiedCard = card;
        if (card == null) return;

        card.SetCurrentSlot(this);
        card.transform.SetParent(transform, false);
        card.ApplySlotLayout();
    }

    public void ClearCard()
    {
        if (OccupiedCard != null)
            OccupiedCard.SetCurrentSlot(null);
        OccupiedCard = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        MainMenuCardView card = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<MainMenuCardView>()
            : null;
        if (card == null) return;

        _grid?.TryPlaceHandCard(card, this);
    }
}
