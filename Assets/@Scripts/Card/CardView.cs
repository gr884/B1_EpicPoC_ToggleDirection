using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private CardRuntimeState _runtimeState;

    [Header("Refs")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private GameObject _preserveRoot;
    [SerializeField] private TMP_Text _preserveText;
    [SerializeField] private GameObject _selectionOverlay;
    [SerializeField] private GameObject _highlightOverlay;

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
    public CardInstance Instance { get; private set; }
    public bool IsActivated { get; private set; }
    public bool IsEnemy { get; private set; }
    public GridSlot CurrentSlot { get; private set; }
    public int PreserveStack { get; private set; }
    public int ContaminateCurseCount { get; private set; }

    public void SetContaminateCurseCount(int count)
    {
        ContaminateCurseCount = count;
    }

    public void AddPreserve(int amount)
    {
        PreserveStack += Mathf.Max(0, amount);
        RefreshPreserveUI();
    }

    public void RemovePreserve(int amount)
    {
        PreserveStack = Mathf.Max(0, PreserveStack - amount);
        RefreshPreserveUI();
    }

    /// <summary>보존 스택 1 차감. 스택이 있으면 true(유지), 없으면 false(버려야 함) 반환.</summary>
    public bool ConsumePreserve()
    {
        if (PreserveStack <= 0) return false;
        PreserveStack--;
        RefreshPreserveUI();
        return true;
    }

    private void RefreshPreserveUI()
    {
        if (_preserveRoot != null)
            _preserveRoot.SetActive(PreserveStack > 0);
        if (_preserveText != null)
            _preserveText.text = PreserveStack.ToString();
    }

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _handAnchorMin;
    private Vector2 _handAnchorMax;
    private Vector2 _handPivot;
    private Vector2 _handSizeDelta;
    private Vector2 _handOffsetMin;
    private Vector2 _handOffsetMax;
    private bool _hasHandLayout;

    [Header("Cost Feedback")]
    [SerializeField] private float _unaffordableAlpha = 0.4f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();

        _runtimeState = GetComponent<CardRuntimeState>();
        if (_runtimeState != null) _runtimeState.OnChanged += RefreshRuntimeText;
        CaptureHandLayout();
    }

    void OnDestroy()
    {
        if (_runtimeState != null) _runtimeState.OnChanged -= RefreshRuntimeText;
    }

    public void Initialize(CardData data, bool isEnemy = false, bool startsActivated = false)
    {
        Initialize(data != null ? new CardInstance(data) : null, isEnemy, startsActivated);
    }

    public void Initialize(CardInstance instance, bool isEnemy = false, bool startsActivated = false)
    {
        Instance = instance;
        Data = instance != null ? instance.SourceData : null;
        IsActivated = startsActivated;
        IsEnemy = isEnemy;
        CurrentSlot = null;
        PreserveStack = 0;
        ContaminateCurseCount = 0;
        RefreshPreserveUI();

        if (_runtimeState != null)
            _runtimeState.Initialize(this, instance, isEnemy);

        if (_selectionOverlay != null)
            _selectionOverlay.SetActive(false);
        if (_highlightOverlay != null)
            _highlightOverlay.SetActive(false);

        if (_iconImage != null)
        {
            _iconImage.sprite = Data != null ? Data.icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_titleText != null)
            _titleText.text = Data != null ? Data.displayName : "";

        if (_costText != null)
        {
            _costText.gameObject.SetActive(!isEnemy);
            _costText.text = Data != null ? Data.cost.ToString() : "";
        }

        RefreshRuntimeText();
        RefreshDirectionIcons();
        RefreshVisual();
    }

    public void ApplyHandLayout()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (!_hasHandLayout)
            CaptureHandLayout();

        if (_rectTransform != null)
        {
            _rectTransform.anchorMin = _handAnchorMin;
            _rectTransform.anchorMax = _handAnchorMax;
            _rectTransform.pivot = _handPivot;
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.sizeDelta = _handSizeDelta;
            _rectTransform.offsetMin = _handOffsetMin;
            _rectTransform.offsetMax = _handOffsetMax;
            _rectTransform.localPosition = Vector3.zero;
            _rectTransform.localRotation = Quaternion.identity;
            _rectTransform.localScale = Vector3.one;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }
    }

    public void ApplyGridLayout()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_rectTransform != null)
        {
            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            _rectTransform.localRotation = Quaternion.identity;
            _rectTransform.localScale = Vector3.one;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }
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
        if (drag != null) drag.SetDraggable(draggable);
    }

    public void SetSelected(bool selected)
    {
        if (_selectionOverlay != null)
            _selectionOverlay.SetActive(selected);
    }

    public void SetHighlight(bool highlight)
    {
        if (_highlightOverlay != null)
            _highlightOverlay.SetActive(highlight);
    }

    public void SetAffordable(bool affordable)
    {
        if (_canvasGroup == null) return;
        // 드래그 중 alpha는 CardDragHandler가 관리하므로, 손패에 있을 때만 적용
        if (CurrentSlot == null)
            _canvasGroup.alpha = affordable ? 1f : _unaffordableAlpha;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (CurrentSlot == null || IsEnemy) return;
        CardManager.Instance.TryRecallCard(this);
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

    private void CaptureHandLayout()
    {
        if (_rectTransform == null) return;

        _handAnchorMin = _rectTransform.anchorMin;
        _handAnchorMax = _rectTransform.anchorMax;
        _handPivot = _rectTransform.pivot;
        _handSizeDelta = _rectTransform.sizeDelta;
        _handOffsetMin = _rectTransform.offsetMin;
        _handOffsetMax = _rectTransform.offsetMax;
        _hasHandLayout = true;
    }

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

    //* 실시간 텍스트 수정
    private void RefreshRuntimeText()
    {
        if (_titleText == null || Data == null) return;
        if (_runtimeState == null || _runtimeState.BonusDamage <= 0)
            _titleText.text = Data.displayName;
        else
        {
            int baseDamage = 0;
            foreach (var e in Data.effects)
                if (e.effectType == EffectType.Damage)
                    baseDamage += Mathf.Max(1, Mathf.RoundToInt(e.value));
            _titleText.text = $"{Data.displayName}\n({baseDamage}+{_runtimeState.BonusDamage})";
        }
    }
}
