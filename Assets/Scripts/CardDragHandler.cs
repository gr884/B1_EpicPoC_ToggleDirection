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
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Card == null || Card.IsPlacedOnBoard || rootCanvas == null)
        {
            return;
        }

        dropAccepted = false;
        startParent = rectTransform.parent;
        startAnchoredPosition = rectTransform.anchoredPosition;
        rectTransform.SetParent(rootCanvas.transform, true);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Card == null || Card.IsPlacedOnBoard || rootCanvas == null)
        {
            return;
        }

        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
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
            return;
        }

        rectTransform.SetParent(startParent, true);
        rectTransform.anchoredPosition = startAnchoredPosition;
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
}
