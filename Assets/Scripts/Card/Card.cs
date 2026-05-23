using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(SpriteRenderer))]
public class Card : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private SpriteRenderer _iconRenderer;
    [SerializeField] private TextMeshPro _titleText;

    [Header("Colors")]
    [SerializeField] private Color _activeColor = Color.white;
    [SerializeField] private Color _inactiveColor = Color.gray;

    public CardData Data { get; private set; }
    public bool IsActivated { get; private set; }
    public bool IsPlacedOnBoard { get; private set; }
    public BoardSlot CurrentSlot { get; private set; }

    private void Reset()
    {
        if (_backgroundRenderer == null)
            _backgroundRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(CardData data, bool startsActivated = false)
    {
        Data = data;
        IsActivated = startsActivated;
        CurrentSlot = null;
        IsPlacedOnBoard = false;

        if (_iconRenderer != null)
        {
            _iconRenderer.sprite = data != null ? data.icon : null;
            _iconRenderer.enabled = _iconRenderer.sprite != null;
        }

        if (_titleText != null)
            _titleText.text = data != null ? data.displayName : "Card";

        RefreshVisual();
    }

    public void SetPlaced(BoardSlot slot)
    {
        CurrentSlot = slot;
        IsPlacedOnBoard = slot != null;
    }

    public void SetActivated(bool activated)
    {
        IsActivated = activated;
        RefreshVisual();
    }

    public void SetDraggable(bool draggable)
    {
        CardDragHandler dragHandler = GetComponent<CardDragHandler>();
        if (dragHandler != null)
            dragHandler.enabled = draggable;
    }

    private void RefreshVisual()
    {
        if (_backgroundRenderer == null) return;
        _backgroundRenderer.color = IsActivated ? _activeColor : _inactiveColor;
    }

    public IEnumerator PlayActivationFeedback(float duration)
    {
        float elapsed = 0f;
        float angle = Random.value > 0.5f ? 10f : -10f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float wiggle = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t);
            float punch = Mathf.Sin(t * Mathf.PI);

            transform.localRotation = Quaternion.Euler(0f, 0f, wiggle * angle);
            transform.localScale = new Vector3(1f + 0.08f * punch, 1f - 0.08f * punch, 1f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}