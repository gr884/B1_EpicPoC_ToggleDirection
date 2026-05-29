using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Refs")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private GameObject _preserveRoot;
    [SerializeField] private TMP_Text _preserveText;
    [SerializeField] private GameObject _selectionOverlay;

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
    public int PreserveStack { get; private set; }

    public void AddPreserve(int amount)
    {
        PreserveStack += Mathf.Max(0, amount);
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

    [Header("Cost Feedback")]
    [SerializeField] private float _unaffordableAlpha = 0.4f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Initialize(CardData data, bool isEnemy = false, bool startsActivated = false)
    {
        Data = data;
        IsActivated = startsActivated;
        IsEnemy = isEnemy;
        CurrentSlot = null;
        PreserveStack = 0;
        RefreshPreserveUI();

        if (_selectionOverlay != null)
            _selectionOverlay.SetActive(false);

        if (_iconImage != null)
        {
            _iconImage.sprite = data != null ? data.icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_titleText != null)
            _titleText.text = data != null ? data.displayName : "";

        if (_costText != null)
        {
            _costText.gameObject.SetActive(!isEnemy);
            _costText.text = data != null ? data.cost.ToString() : "";
        }

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

    public void SetSelected(bool selected)
    {
        if (_selectionOverlay != null)
            _selectionOverlay.SetActive(selected);
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