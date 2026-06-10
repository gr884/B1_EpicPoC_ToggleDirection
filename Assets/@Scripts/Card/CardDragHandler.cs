using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private Transform _startParent;
    private Vector2 _startAnchoredPosition;
    private int _startSiblingIndex;
    private bool _dropAccepted;
    private bool _pendingDropAccepted;
    private bool _dragBlocked;
    private bool _isDraggable = true;
    private GridSlot _previewSlot;
    private readonly List<RaycastResult> _raycastResults = new();

    public void SetDraggable(bool draggable)
    {
        _isDraggable = draggable;
    }

    public CardView Card { get; private set; }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        Card = GetComponent<CardView>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragBlocked = true; // 기본값 차단, 정상 진행 시에만 false로
        if (!enabled || Card == null) return;
        if (!_isDraggable) return;
        if (Card.CurrentSlot != null) return;
        if (!CanDragHandCardInput()) return;

        // 튜토리얼에서 막힌 카드면 드래그 차단
        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanDragCard(Card))
            return;

        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas != null && _rootCanvas.rootCanvas != null)
            _rootCanvas = _rootCanvas.rootCanvas;
        if (_rootCanvas == null) return;

        _dragBlocked = false; // 여기까지 왔으면 정상 드래그
        _dropAccepted = false;
        _pendingDropAccepted = false;
        _previewSlot = null;
        _startAnchoredPosition = _rectTransform.anchoredPosition;
        _startParent = transform.parent;
        _startSiblingIndex = transform.GetSiblingIndex();

        _rectTransform.SetParent(_rootCanvas.transform, true);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.75f;

        TutorialManager.Instance?.OnCardDragBegin(Card);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enabled || _dragBlocked || _rootCanvas == null || Card.CurrentSlot != null) return;
        if (!CanDragHandCardInput())
        {
            ClearPendingDirectionalImpact();
            CancelDrag();
            return;
        }

        _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
        RefreshPendingDirectionalImpact(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enabled || _dragBlocked) return;

        _canvasGroup.blocksRaycasts = !_pendingDropAccepted;
        _canvasGroup.alpha = 1f;
        ClearPendingDirectionalImpact();

        if (!_dropAccepted)
        {
            CancelDrag();
        }
    }

    public void CommitDrop(Transform newParent)
    {
        _dropAccepted = true;
        _pendingDropAccepted = false;
        ClearPendingDirectionalImpact();

        GridManager.Instance?.LayoutPlacedCard(Card);

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
    }

    public void CommitPendingDrop(Transform newParent)
    {
        _dropAccepted = true;
        _pendingDropAccepted = true;
        ClearPendingDirectionalImpact();

        // The queued card is laid out after the current chain finishes.

        // 정식 부착 전까지 플레이어 입력과 슬롯 판정을 가로막지 않는다.
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 1f;
        _rootCanvas = null;
    }

    private bool CanDragHandCardInput()
    {
        return CardManager.Instance != null && CardManager.Instance.CanDragHandCardInput;
    }

    private void RefreshPendingDirectionalImpact(PointerEventData eventData)
    {
        GridSlot hoveredSlot = FindHoveredSlot(eventData);
        if (hoveredSlot == _previewSlot) return;

        _previewSlot = hoveredSlot;
        if (GridManager.Instance == null) return;

        if (_previewSlot == null)
            GridManager.Instance.ClearPendingDirectionalImpact();
        else
            GridManager.Instance.ShowPendingDirectionalImpact(Card, _previewSlot);
    }

    private GridSlot FindHoveredSlot(PointerEventData eventData)
    {
        GridSlot slot = eventData.pointerCurrentRaycast.gameObject != null
            ? eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<GridSlot>()
            : null;
        if (slot != null) return slot;

        if (EventSystem.current == null) return null;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastResults);
        foreach (RaycastResult result in _raycastResults)
        {
            slot = result.gameObject != null
                ? result.gameObject.GetComponentInParent<GridSlot>()
                : null;
            if (slot != null) return slot;
        }

        return null;
    }

    private void ClearPendingDirectionalImpact()
    {
        _previewSlot = null;
        GridManager.Instance?.ClearPendingDirectionalImpact();
    }

    private void CancelDrag()
    {
        _dragBlocked = true;
        ClearPendingDirectionalImpact();

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }

        if (_startParent != null)
        {
            _rectTransform.SetParent(_startParent, false);
            _rectTransform.SetSiblingIndex(_startSiblingIndex);
            _rectTransform.anchoredPosition = _startAnchoredPosition;
        }

        _rootCanvas = null;
        TutorialManager.Instance?.OnCardDragCancelled(Card);
    }
}
