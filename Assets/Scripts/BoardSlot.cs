using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoardSlot : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color baseColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Color pulseColor = new Color(1f, 1f, 0.2f, 1f);

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
        if (OccupiedCard != null)
        {
            OccupiedCard.SetPlaced(null);
        }

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
        if (placed)
        {
            dragHandler.CommitDrop(transform);
        }
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
}
