using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoardSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler
{
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color baseColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Color pulseColor = new Color(1f, 1f, 0.2f, 1f);
    [SerializeField] private Outline previewOutline;
    [SerializeField] private Color previewTargetColor = new Color(0.35f, 0.85f, 1f, 1f);
    [SerializeField] private Color previewCenterColor = new Color(1f, 0.92f, 0.35f, 1f);
    [SerializeField] private Color previewBlockedCenterColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Vector2 previewThickness = new Vector2(4f, 4f);
    [SerializeField] private Image previewFillImage;

    public Vector2Int Position { get; private set; }
    public Card OccupiedCard { get; private set; }

    private GameManager gameManager;
    private Coroutine highlightRoutine;

    private void Awake()
    {
        if (highlightImage != null)
        {
            highlightImage.color = baseColor;
        }

        EnsurePreviewVisuals();
    }

    public void Setup(Vector2Int position, GameManager owner)
    {
        Position = position;
        gameManager = owner;
        OccupiedCard = null;

        if (highlightImage != null)
        {
            highlightImage.color = baseColor;
        }

        SetPreview(false, false, true);
    }

    public bool IsEmpty()
    {
        return OccupiedCard == null;
    }

    public void AssignCard(Card card)
    {
        OccupiedCard = card;
        if (card != null)
        {
            card.SetPlaced(this);
        }
    }

    public void ClearCard()
    {
        OccupiedCard = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (gameManager == null)
        {
            return;
        }

        CardDragHandler dragHandler = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<CardDragHandler>()
            : null;

        if (dragHandler == null || dragHandler.Card == null)
        {
            return;
        }

        bool placed = gameManager.TryPlaceCardFromHand(dragHandler.Card, this);
        gameManager.ClearPlacementPreview();
        if (placed)
        {
            dragHandler.CommitDrop(transform);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (gameManager == null || eventData == null || eventData.pointerDrag == null)
        {
            return;
        }

        CardDragHandler dragHandler = eventData.pointerDrag.GetComponent<CardDragHandler>();
        if (dragHandler == null || dragHandler.Card == null)
        {
            return;
        }

        gameManager.ShowPlacementPreview(dragHandler.Card, this);
    }

    public void PulseHighlight(float duration = 0.2f)
    {
        if (highlightImage == null)
        {
            return;
        }

        if (highlightRoutine != null)
        {
            StopCoroutine(highlightRoutine);
        }

        highlightRoutine = StartCoroutine(PulseRoutine(duration));
    }

    private IEnumerator PulseRoutine(float duration)
    {
        if (highlightImage == null)
        {
            yield break;
        }

        highlightImage.color = pulseColor;
        yield return new WaitForSeconds(duration * 0.5f);
        highlightImage.color = baseColor;
        yield return new WaitForSeconds(duration * 0.5f);

        highlightRoutine = null;
    }

    public void SetPreview(bool on, bool isCenter, bool isPlaceable)
    {
        EnsurePreviewVisuals();

        Color c = isCenter
            ? (isPlaceable ? previewCenterColor : previewBlockedCenterColor)
            : previewTargetColor;

        if (previewOutline != null)
        {
            previewOutline.effectColor = c;
            previewOutline.enabled = on;
        }

        if (previewFillImage != null)
        {
            previewFillImage.color = new Color(c.r, c.g, c.b, on ? 0.22f : 0f);
            previewFillImage.enabled = on;
            previewFillImage.transform.SetAsLastSibling();
        }
    }

    private void EnsurePreviewVisuals()
    {
        if (previewOutline == null)
        {
            previewOutline = GetComponent<Outline>();
            if (previewOutline == null)
            {
                previewOutline = gameObject.AddComponent<Outline>();
            }
        }

        previewOutline.effectDistance = previewThickness;
        previewOutline.useGraphicAlpha = false;
        previewOutline.enabled = false;

        if (previewFillImage == null)
        {
            Transform existing = transform.Find("PreviewFill");
            if (existing != null)
            {
                previewFillImage = existing.GetComponent<Image>();
            }
        }

        if (previewFillImage == null)
        {
            GameObject go = new GameObject("PreviewFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            previewFillImage = go.GetComponent<Image>();
            previewFillImage.raycastTarget = false;
            previewFillImage.enabled = false;
        }
    }
}
