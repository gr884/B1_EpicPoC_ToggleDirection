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
    [SerializeField] private Image shieldOverlayImage;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color normalIconColor = Color.white;
    [SerializeField] private Color bufferIconColor = new Color(0.55f, 0.8f, 1f, 1f);
    [SerializeField] private Color shieldOverlayColor = new Color(0.35f, 0.65f, 1f, 0.35f);

    public CardData Data { get; private set; }
    public bool IsActivated { get; private set; }
    public bool IsPlacedOnBoard { get; private set; }
    public BoardSlot CurrentSlot { get; private set; }
    public int ShieldCharges { get; private set; }

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        EnsureShieldOverlay();
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
        ShieldCharges = 0;

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

    public void SetShieldCharges(int value)
    {
        ShieldCharges = Mathf.Max(0, value);
        RefreshShieldVisual();
    }

    public void GrantShieldOneCharge()
    {
        ShieldCharges = 1;
        RefreshShieldVisual();
    }

    public bool TryConsumeShieldOnExternalToggle()
    {
        if (ShieldCharges <= 0)
        {
            return false;
        }

        ShieldCharges--;
        RefreshShieldVisual();
        return true;
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
        if (backgroundImage != null)
        {
            // Buffer card background must stay in the same scheme as normal cards.
            backgroundImage.color = IsActivated ? activeColor : inactiveColor;
        }

        if (iconImage != null)
        {
            bool isBuffer = Data != null && Data.abilityType == CardAbilityType.Buffer;
            iconImage.color = isBuffer ? bufferIconColor : normalIconColor;
        }

        RefreshShieldVisual();
    }

    private void RefreshShieldVisual()
    {
        EnsureShieldOverlay();
        if (shieldOverlayImage != null)
        {
            shieldOverlayImage.gameObject.SetActive(ShieldCharges > 0);
            shieldOverlayImage.color = shieldOverlayColor;
        }
    }

    private void EnsureShieldOverlay()
    {
        if (shieldOverlayImage != null)
        {
            return;
        }

        Transform found = transform.Find("ShieldOverlay");
        if (found != null)
        {
            shieldOverlayImage = found.GetComponent<Image>();
            return;
        }

        GameObject go = new GameObject("ShieldOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        shieldOverlayImage = go.GetComponent<Image>();
        shieldOverlayImage.color = shieldOverlayColor;
        shieldOverlayImage.raycastTarget = false;
        go.transform.SetAsLastSibling();
        go.SetActive(false);
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
