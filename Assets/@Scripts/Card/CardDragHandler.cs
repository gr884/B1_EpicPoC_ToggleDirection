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
    private bool _dropAccepted;

    public CardView Card { get; private set; }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        Card = GetComponent<CardView>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enabled || Card == null) return;
        if (Card.CurrentSlot != null) return; // 그리드에 배치된 카드는 드래그 불가

        _rootCanvas = FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) return;

        _dropAccepted = false;
        _startWorldPosition = _rectTransform.position;
        _startParent = transform.parent;

        _rectTransform.SetParent(_rootCanvas.transform, true);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enabled || _rootCanvas == null || Card.CurrentSlot != null) return;
        _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enabled) return;

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        // 드롭 실패 시 원래 위치로 복귀
        if (!_dropAccepted)
        {
            _rectTransform.SetParent(_startParent, true);
            _rectTransform.position = _startWorldPosition;
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