using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Refs")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private CanvasGroup _canvasGroup;

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
    [SerializeField] private Color _inactiveColor = new(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color _activeColor = new(0.2f, 0.65f, 0.35f, 1f);
    [SerializeField] private Color _fixedColor = new(0.25f, 0.25f, 0.32f, 1f);
    [SerializeField] private Color _dragColor = new(0.32f, 0.32f, 0.42f, 1f);

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Canvas _dragCanvas;
    private bool _draggable;
    private bool _dragging;

    public bool IsFixed { get; private set; }
    public bool IsActivated { get; private set; }
    public MainMenuCardDirection Direction { get; private set; }
    public MainMenuCardAction Action { get; private set; }
    public MainMenuGridSlot CurrentSlot { get; private set; }
    public MainMenuHandController OwnerHand { get; private set; }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void InitializeFixed(MainMenuFixedCardSeed seed)
    {
        IsFixed = true;
        IsActivated = false;
        Direction = MainMenuCardDirection.None;
        Action = seed != null ? seed.action : MainMenuCardAction.None;
        OwnerHand = null;
        SetText(seed != null ? seed.displayName : "");
        SetIcon(seed != null ? seed.icon : null);
        RefreshDirectionIcons();
        SetDraggable(false);
        RefreshVisual();
    }

    public void InitializeHand(MainMenuHandCardSeed seed, MainMenuHandController ownerHand)
    {
        IsFixed = false;
        IsActivated = false;
        Direction = seed != null ? seed.direction : MainMenuCardDirection.None;
        Action = MainMenuCardAction.None;
        OwnerHand = ownerHand;
        SetText(seed != null ? seed.displayName : "");
        SetIcon(seed != null ? seed.icon : null);
        RefreshDirectionIcons();
        SetDraggable(true);
        RefreshVisual();
    }

    public void SetCurrentSlot(MainMenuGridSlot slot)
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
        _draggable = draggable && !IsFixed;
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }
        RefreshVisual();
    }

    public void ApplyHandLayout()
    {
        EnsureRefs();
        if (_rectTransform == null) return;

        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.localRotation = Quaternion.identity;
        _rectTransform.localScale = Vector3.one;
    }

    public void ApplySlotLayout()
    {
        EnsureRefs();
        if (_rectTransform == null) return;

        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
        _rectTransform.localRotation = Quaternion.identity;
        _rectTransform.localScale = Vector3.one;
    }

    public IEnumerator PlayActivationFeedback(float duration)
    {
        EnsureRefs();
        if (_rectTransform == null)
            yield break;

        Vector3 origin = _rectTransform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? elapsed / duration : 1f;
            float pulse = Mathf.Sin(t * Mathf.PI);
            _rectTransform.localScale = origin * (1f + pulse * 0.12f);
            yield return null;
        }

        _rectTransform.localScale = origin;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_draggable || IsFixed) return;

        EnsureRefs();
        if (_rectTransform == null || _canvas == null) return;

        _dragging = true;
        _dragCanvas = _canvas.rootCanvas != null ? _canvas.rootCanvas : _canvas;
        transform.SetParent(_dragCanvas.transform, true);
        transform.SetAsLastSibling();

        if (_canvasGroup != null)
            _canvasGroup.blocksRaycasts = false;
        RefreshVisual();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _rectTransform == null || _dragCanvas == null) return;

        _rectTransform.anchoredPosition += eventData.delta / _dragCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging) return;

        _dragging = false;
        if (_canvasGroup != null)
            _canvasGroup.blocksRaycasts = true;
        RefreshVisual();

        if (CurrentSlot == null)
            OwnerHand?.ReturnCard(this);

        _dragCanvas = null;
    }

    private void SetText(string displayName)
    {
        if (_titleText != null)
            _titleText.text = displayName;
    }

    private void RefreshDirectionIcons()
    {
        HideAllDirectionIcons();

        if (IsFixed || Direction == MainMenuCardDirection.None) return;

        Image target = GetDirectionImage(Direction);
        if (target != null)
            target.enabled = true;
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

    private Image GetDirectionImage(MainMenuCardDirection direction) => direction switch
    {
        MainMenuCardDirection.Up => _up,
        MainMenuCardDirection.UpRight => _upRight,
        MainMenuCardDirection.Right => _right,
        MainMenuCardDirection.DownRight => _downRight,
        MainMenuCardDirection.Down => _down,
        MainMenuCardDirection.DownLeft => _downLeft,
        MainMenuCardDirection.Left => _left,
        MainMenuCardDirection.UpLeft => _upLeft,
        _ => null
    };

    private void SetIcon(Sprite icon)
    {
        if (_iconImage == null) return;

        _iconImage.sprite = icon;
        _iconImage.enabled = icon != null;
    }

    private void RefreshVisual()
    {
        if (_backgroundImage == null) return;

        if (_dragging)
            _backgroundImage.color = _dragColor;
        else if (IsActivated)
            _backgroundImage.color = _activeColor;
        else
            _backgroundImage.color = IsFixed ? _fixedColor : _inactiveColor;
    }

    private void EnsureRefs()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
    }
}
