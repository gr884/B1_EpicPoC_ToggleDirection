using UnityEngine;

[RequireComponent(typeof(Card))]
[RequireComponent(typeof(Collider2D))]
public class CardDragHandler : MonoBehaviour
{
    private Card _card;
    private Camera _mainCamera;
    private Vector3 _dragOffset;
    private Vector3 _startPosition;
    private Transform _startParent;
    private bool _isDragging;

    private void Awake()
    {
        _card = GetComponent<Card>();
        _mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        if (!enabled) return;

        // 보드에 놓인 카드 클릭 → 패로 돌려보내기
        if (_card.IsPlacedOnBoard)
        {
            GameFlowManager.Instance?.TryRemoveCard(_card.CurrentSlot);
            return;
        }

        // 드래그 시작
        _isDragging = true;
        _startPosition = transform.position;
        _startParent = transform.parent;

        _dragOffset = transform.position - GetMouseWorldPos();

        transform.SetParent(null);
        SetSortingOrder(10);
    }

    private void OnMouseDrag()
    {
        if (!enabled || !_isDragging) return;
        transform.position = GetMouseWorldPos() + _dragOffset;
    }

    private void OnMouseUp()
    {
        if (!enabled || !_isDragging) return;
        _isDragging = false;

        BoardSlot slot = GetSlotUnderMouse();
        if (slot != null && slot.IsEmpty())
        {
            bool placed = GameFlowManager.Instance.TryPlaceCard(_card, slot);
            if (placed)
            {
                SetSortingOrder(1);
                return;
            }
        }

        // 드롭 실패 → 원위치
        transform.SetParent(_startParent);
        transform.position = _startPosition;
        SetSortingOrder(1);
    }

    private BoardSlot GetSlotUnderMouse()
    {
        Vector2 worldPos = GetMouseWorldPos();
        int slotLayer = LayerMask.GetMask("Slot");
        Collider2D hit = Physics2D.OverlapPoint(worldPos, slotLayer);
        if (hit != null)
            return hit.GetComponent<BoardSlot>();
        return null;
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(_mainCamera.transform.position.z);
        return _mainCamera.ScreenToWorldPoint(mousePos);
    }

    private void SetSortingOrder(int order)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = order;

        foreach (TMPro.TextMeshPro tmp in GetComponentsInChildren<TMPro.TextMeshPro>())
            tmp.sortingOrder = order;
    }
}