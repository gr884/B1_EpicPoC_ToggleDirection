using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Image _backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color _baseColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color _lockedColor = new Color(0f, 0f, 0f, 0f);        // 투명
    [SerializeField] private Color _hoverColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color _previewColor = new Color(1f, 1f, 0.2f, 0.4f);
    [SerializeField] private Color _occupiedColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    public Vector2Int Position { get; private set; }
    public ItemView OccupiedItem { get; private set; }
    public bool IsEmpty => OccupiedItem == null;
    public bool IsLocked { get; private set; }

    public void Setup(Vector2Int position)
    {
        Position = position;
        OccupiedItem = null;
        IsLocked = false;
        SetColor(_baseColor);
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        SetColor(locked ? _lockedColor : _baseColor);
    }

    public void AssignItem(ItemView item)
    {
        OccupiedItem = item;
        if (item != null)
            item.SetPlaced(this);
        SetColor(_occupiedColor);
    }

    public void ClearItem()
    {
        if (OccupiedItem != null)
            OccupiedItem.SetPlaced(null);
        OccupiedItem = null;
        SetColor(_baseColor);
    }

    public void SetPreviewHighlight(bool on)
    {
        if (OccupiedItem != null)
            OccupiedItem.SetHighlight(on);
        else
            SetColor(on ? _previewColor : _baseColor);
    }

    // ── UI 이벤트 ──────────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        if (IsLocked) return;

        ItemDragHandler drag = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        if (drag == null || drag.Item == null) return;

        bool placed = BackpackManager.Instance.TryPlaceItem(drag.Item, this);
        if (placed)
            drag.CommitDrop(transform);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsLocked) return;

        if (IsEmpty) SetColor(_hoverColor);
        ChainExecutor.Instance.ShowPreview(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsLocked) return;

        if (IsEmpty) SetColor(_baseColor);
        ChainExecutor.Instance.ClearPreview();
    }

    private void SetColor(Color color)
    {
        if (_backgroundImage != null)
            _backgroundImage.color = color;
    }
}