using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using UnityEngine.EventSystems;

public class ItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _titleText;

    [Header("Colors")]
    [SerializeField] private Color _activeColor = Color.white;
    [SerializeField] private Color _inactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0.2f, 1f);

    private bool _isHighlighted;

    public ItemData Data { get; private set; }
    public bool IsActivated { get; private set; }
    public bool IsPlaced { get; private set; }
    public bool IsPickupItem { get; private set; }
    public GridSlot CurrentSlot { get; private set; }

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(ItemData data, bool startsActivated = false, bool isPickupItem = false)
    {
        Data = data;
        IsActivated = startsActivated;
        IsPickupItem = isPickupItem;
        CurrentSlot = null;
        IsPlaced = false;

        if (_iconImage != null)
        {
            _iconImage.sprite = data != null ? data.icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_titleText != null)
            _titleText.text = data != null ? data.displayName : "Item";

        RefreshVisual();
    }

    public void SetPlaced(GridSlot slot)
    {
        CurrentSlot = slot;
        IsPlaced = slot != null;
    }

    public void SetPickupItem(bool isPickup)
    {
        IsPickupItem = isPickup;
    }

    public void SetActivated(bool activated)
    {
        IsActivated = activated;
        RefreshVisual();
    }

    public void SetDraggable(bool draggable)
    {
        ItemDragHandler drag = GetComponent<ItemDragHandler>();
        if (drag != null) drag.enabled = draggable;
    }

    public void SetHighlight(bool on)
    {
        _isHighlighted = on;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (_backgroundImage == null) return;

        if (_isHighlighted)
            _backgroundImage.color = _highlightColor;
        else
            _backgroundImage.color = IsActivated ? _activeColor : _inactiveColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Data != null)
            UI_Tooltip.Instance.Show(Data, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UI_Tooltip.Instance.Hide();
    }

    public IEnumerator PlayActivationFeedback(float duration)
    {
        if (_rectTransform == null) yield break;

        float elapsed = 0f;
        float angle = Random.value > 0.5f ? 10f : -10f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float wiggle = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t);
            float punch = Mathf.Sin(t * Mathf.PI);

            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, wiggle * angle);
            _rectTransform.localScale = new Vector3(1f + 0.08f * punch, 1f - 0.08f * punch, 1f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _rectTransform.localRotation = Quaternion.identity;
        _rectTransform.localScale = Vector3.one;
    }
}