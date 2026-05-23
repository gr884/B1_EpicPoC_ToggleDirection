using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BoardSlot : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color _baseColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color _pulseColor = new Color(1f, 1f, 0.2f, 1f);

    public Vector2Int Position { get; private set; }
    public Card OccupiedCard { get; private set; }

    private SpriteRenderer _spriteRenderer;
    private Coroutine _highlightRoutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _spriteRenderer.color = _baseColor;
    }

    public void Setup(Vector2Int position)
    {
        Position = position;
        OccupiedCard = null;

        if (_spriteRenderer != null)
            _spriteRenderer.color = _baseColor;
    }

    public bool IsEmpty() => OccupiedCard == null;

    public void AssignCard(Card card)
    {
        OccupiedCard = card;
        if (card != null)
        {
            card.SetPlaced(this);
            card.transform.position = transform.position;
            card.transform.SetParent(transform);
        }
    }

    public void ClearCard()
    {
        if (OccupiedCard != null)
        {
            OccupiedCard.transform.SetParent(null);
            OccupiedCard.SetPlaced(null);
        }
        OccupiedCard = null;
    }

    public void PulseHighlight(float duration = 0.2f)
    {
        if (_spriteRenderer == null) return;

        if (_highlightRoutine != null)
            StopCoroutine(_highlightRoutine);

        _highlightRoutine = StartCoroutine(PulseRoutine(duration));
    }

    private IEnumerator PulseRoutine(float duration)
    {
        _spriteRenderer.color = _pulseColor;
        yield return new WaitForSeconds(duration * 0.5f);
        _spriteRenderer.color = _baseColor;
        yield return new WaitForSeconds(duration * 0.5f);
        _highlightRoutine = null;
    }
}