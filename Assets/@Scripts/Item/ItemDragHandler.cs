using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private Transform _startParent;
    private Vector3 _startWorldPosition;
    private bool _dropAccepted;

    public ItemView Item { get; private set; }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        Item = GetComponent<ItemView>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enabled || Item == null) return;

        _rootCanvas = FindFirstObjectByType<Canvas>();
        if (_rootCanvas == null) return;

        _dropAccepted = false;
        _startWorldPosition = _rectTransform.position;

        // 드래그 시작 시 데이터를 지우지 않고 부모만 최상위 Canvas로 변경
        _startParent = transform.parent;
        _rectTransform.SetParent(_rootCanvas.transform, true);

        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enabled || _rootCanvas == null) return;
        _rectTransform.anchoredPosition += eventData.delta / _rootCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enabled) return;

        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
        
        if (_dropAccepted) return;

        bool isNearBackpack = false;

        float backpackThresholdRadius = 140f; //

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.IsLocked) continue;

            float distance = Vector2.Distance(slot.transform.position, Input.mousePosition);
            if (distance <= backpackThresholdRadius)
            {
                isNearBackpack = true;
                break; 
            }
        }

        if (isNearBackpack)
        {
            _rectTransform.SetParent(_startParent, true);
            _rectTransform.position = _startWorldPosition;
            Debug.Log("[DragHandler] 가방 근처에 애매하게 걸쳐서 원래 위치로 튕김!");
        }
        else
        {
            if (Item.IsPlaced)
            {
                BackpackManager.Instance.TryRemoveItem(Item);
            }
            Debug.Log("[DragHandler] 가방과 완전히 떨어진 곳에 자유 배치 완료!");
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