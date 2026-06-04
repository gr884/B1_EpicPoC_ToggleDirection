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
        _dragBlocked = true; // 기본값 차단, 정상 진행 시에만 false로
        if (!enabled || Card == null) return;
        if (!_isDraggable) return;
        if (Card.CurrentSlot != null) return;
        if (BattleManager.Instance != null && BattleManager.Instance.IsProcessing) return;
        if (BattleManager.Instance != null && BattleManager.Instance.CurrentPhase != BattleManager.BattlePhase.PlayerTurn) return;
        if (ChainExecutor.Instance != null && ChainExecutor.Instance.IsExecuting) return;

        // 튜토리얼에서 막힌 카드면 드래그 차단
        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanDragCard(Card))
            return;

        _rootCanvas = FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) return;

        _dragBlocked = false; // 여기까지 왔으면 정상 드래그
        _dropAccepted = false;
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
        _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enabled || _dragBlocked) return;

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        if (!_dropAccepted)
        {
            _rectTransform.SetParent(_startParent, false);
            _rectTransform.SetSiblingIndex(_startSiblingIndex);
            _rectTransform.anchoredPosition = _startAnchoredPosition;
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
