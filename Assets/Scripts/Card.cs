using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;

    public CardData Data { get; private set; }
    public bool IsActivated { get; private set; }
    public bool IsPlacedOnBoard { get; private set; }
    public BoardSlot CurrentSlot { get; private set; }

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Reset()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
    }

    public void Initialize(CardData data, bool startsActivated = false)
    {
        Data = data;
        IsActivated = startsActivated;
        CurrentSlot = null;
        IsPlacedOnBoard = false;

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.icon : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (titleText != null)
        {
            titleText.text = data != null ? data.displayName : "Card";
            titleText.alignment = TextAlignmentOptions.TopLeft;
        }

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
        {
            dragHandler.enabled = draggable;
        }
    }

    private void RefreshVisual()
    {
        if (backgroundImage == null)
        {
            return;
        }

        backgroundImage.color = IsActivated ? activeColor : inactiveColor;
    }

    public IEnumerator PlayActivationFeedback(float duration)
    {
        if (rectTransform == null)
        {
            yield break;
        }

        Vector3 baseScale = Vector3.one;
        Quaternion baseRotation = Quaternion.identity;
        float angle = Random.value > 0.5f ? 10f : -10f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float wiggle = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t);
            float punch = Mathf.Sin(t * Mathf.PI);

            rectTransform.localRotation = Quaternion.Euler(0f, 0f, wiggle * angle);
            rectTransform.localScale = new Vector3(1f + 0.08f * punch, 1f - 0.08f * punch, 1f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.localRotation = baseRotation;
        rectTransform.localScale = baseScale;
    }
}
