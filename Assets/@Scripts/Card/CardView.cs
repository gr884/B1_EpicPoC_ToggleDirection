using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _titleText;

    [Header("Direction Icons")]
    [SerializeField] private Image _upLeft;
    [SerializeField] private Image _up;
    [SerializeField] private Image _upRight;
    [SerializeField] private Image _left;
    [SerializeField] private Image _right;
    [SerializeField] private Image _downLeft;
    [SerializeField] private Image _down;
    [SerializeField] private Image _downRight;

    [Header("Colors")]
    [SerializeField] private Color _activeColor = Color.white;
    [SerializeField] private Color _inactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color _enemyActiveColor = new Color(0.2f, 0.55f, 1f, 1f);
    [SerializeField] private Color _enemyInactiveColor = new Color(0.55f, 0.55f, 1f, 1f);

    public CardData Data { get; private set; }
    public bool IsActivated { get; private set; }
    public bool IsEnemy { get; private set; }
    public GridSlot CurrentSlot { get; private set; }

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(CardData data, bool isEnemy = false, bool startsActivated = false)
    {
        Data = data;
        IsActivated = startsActivated;
        IsEnemy = isEnemy;
        CurrentSlot = null;

        if (_iconImage != null)
        {
            _iconImage.sprite = data != null ? data.icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_titleText != null)
            _titleText.text = data != null ? data.displayName : "";

        RefreshDirectionIcons();
        RefreshVisual();
    }

    public void SetPlaced(GridSlot slot)
    {
        CurrentSlot = slot;
    }

    public void SetActivated(bool activated)
    {
        IsActivated = activated;
        RefreshVisual();
    }

    public void SetDraggable(bool draggable)
    {
        CardDragHandler drag = GetComponent<CardDragHandler>();
        if (drag != null) drag.enabled = draggable;
    }

    // ── UI 이벤트 ──────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Data != null)
            UI_Tooltip.Instance.Show(Data, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UI_Tooltip.Instance.Hide();
    }

    // ── 피드백 ─────────────────────────────────────────────

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

    // ── 내부 ───────────────────────────────────────────────

    private void RefreshDirectionIcons()
    {
        HideAllDirectionIcons();

        if (Data == null) return;

        foreach (CardDirection dir in Data.GetAllDirections())
        {
            Image target = GetDirectionImage(dir);
            if (target != null) target.enabled = true;
        }
    }

    private void HideAllDirectionIcons()
    {
        if (_up != null) _up.enabled = false;
        if (_upRight != null) _upRight.enabled = false;
        if (_right != null) _right.enabled = false;
        if (_downRight != null) _downRight.enabled = false;
        if (_down != null) _down.enabled = false;
        if (_downLeft != null) _downLeft.enabled = false;
        if (_left != null) _left.enabled = false;
        if (_upLeft != null) _upLeft.enabled = false;
    }

    private Image GetDirectionImage(CardDirection direction) => direction switch
    {
        CardDirection.Up => _up,
        CardDirection.UpRight => _upRight,
        CardDirection.Right => _right,
        CardDirection.DownRight => _downRight,
        CardDirection.Down => _down,
        CardDirection.DownLeft => _downLeft,
        CardDirection.Left => _left,
        CardDirection.UpLeft => _upLeft,
        _ => null
    };

    private void RefreshVisual()
    {
        if (_backgroundImage == null) return;

        if (IsEnemy)
            _backgroundImage.color = IsActivated ? _enemyActiveColor : _enemyInactiveColor;
        else
            _backgroundImage.color = IsActivated ? _activeColor : _inactiveColor;
    }
}