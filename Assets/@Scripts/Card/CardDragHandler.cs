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
    private Vector3 _startWorldPosition;
    private int _startSiblingIndex;
    private bool _dropAccepted;
    private bool _dragBlocked;
    private bool _isDraggable = true;

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
        _dragBlocked = false;
        if (!enabled || Card == null) return;
        if (!_isDraggable) return;
        if (Card.CurrentSlot != null) return;
        if (BattleManager.Instance != null && BattleManager.Instance.IsProcessing) return;

        // 튜토리얼에서 막힌 카드면 드래그 차단
        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanDragCard(Card))
        {
            _dragBlocked = true;
            return;
        }

        _rootCanvas = FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) return;

        _dropAccepted = false;
        _startWorldPosition = _rectTransform.position;
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
        _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enabled || _dragBlocked) return;

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        if (!_dropAccepted)
        {
            _rectTransform.SetParent(_startParent, true);
            _rectTransform.SetSiblingIndex(_startSiblingIndex);
            _rectTransform.position = _startWorldPosition;
            TutorialManager.Instance?.OnCardDragCancelled(Card);
        }
    }

    public void CommitDrop(Transform newParent)
    {
        _dropAccepted = true;

        _rectTransform.SetParent(newParent, false);
        FitToParent();

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
    }

    private void FitToParent()
    {
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
        _rectTransform.localScale = Vector3.one;
    }
}