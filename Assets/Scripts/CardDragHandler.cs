using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Canvas rootCanvas;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform startParent;
    private Vector2 startAnchoredPosition;
    private bool dropAccepted;
    private GameManager gameManager;

    public Card Card { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        Card = GetComponent<Card>();

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        gameManager = FindFirstObjectByType<GameManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (Card == null || Card.IsPlacedOnBoard || rootCanvas == null)
        {
            return;
        }

        dropAccepted = false;
        startParent = rectTransform.parent;
        startAnchoredPosition = rectTransform.anchoredPosition;
        rectTransform.SetParent(rootCanvas.transform, true);
        gameManager?.ClearPlacementPreview();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (Card == null || Card.IsPlacedOnBoard || rootCanvas == null)
        {
            return;
        }

        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        BoardSlot hoveredSlot = FindHoveredSlot(eventData);
        if (hoveredSlot != null)
        {
            gameManager?.ShowPlacementPreview(Card, hoveredSlot);
        }
        else
        {
            gameManager?.ClearPlacementPreview();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (Card == null || Card.IsPlacedOnBoard)
        {
            return;
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (dropAccepted)
        {
            gameManager?.ClearPlacementPreview();
            return;
        }

        rectTransform.SetParent(startParent, true);
        rectTransform.anchoredPosition = startAnchoredPosition;
        gameManager?.ClearPlacementPreview();
    }

    public void CommitDrop(Transform newParent)
    {
        dropAccepted = true;

        rectTransform.SetParent(newParent, false);
        FitToParent();

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }

    private void FitToParent()
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static BoardSlot FindHoveredSlot(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return null;
        }

        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            BoardSlot direct = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<BoardSlot>();
            if (direct != null)
            {
                return direct;
            }
        }

        for (int i = 0; i < eventData.hovered.Count; i++)
        {
            GameObject hoveredObj = eventData.hovered[i];
            if (hoveredObj == null)
            {
                continue;
            }

            BoardSlot slot = hoveredObj.GetComponentInParent<BoardSlot>();
            if (slot != null)
            {
                return slot;
            }
        }

        if (EventSystem.current != null)
        {
            List<RaycastResult> results = new();
            EventSystem.current.RaycastAll(eventData, results);
            for (int i = 0; i < results.Count; i++)
            {
                GameObject go = results[i].gameObject;
                if (go == null)
                {
                    continue;
                }

                BoardSlot slot = go.GetComponentInParent<BoardSlot>();
                if (slot != null)
                {
                    return slot;
                }
            }
        }

        return null;
    }
}
