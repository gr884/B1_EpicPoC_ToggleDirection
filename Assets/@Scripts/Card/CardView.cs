using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private TMP_Text _durabilityText;

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
    public int CurrentDurability { get; private set; }

    // maxDurability == 0이면 무제한 전파
    public bool CanPropagate => Data == null || Data.maxDurability == 0 || CurrentDurability > 0;

    // 런타임 방향 (드로우 시 확률 생성) — null이면 CardData.directions 사용
    private List<CardDirection> _runtimeDirections;
    // 런타임 회전: 0~7, 1 step = 45° 시계 방향
    private int _rotationOffset;
    private bool _isHovered;

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
        _runtimeDirections = null;
        _rotationOffset = 0;
        CurrentDurability = data != null ? data.maxDurability : 0;

        if (_iconImage != null)
        {
            _iconImage.sprite = data != null ? data.icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_titleText != null)
            _titleText.text = data != null ? data.displayName : "";

        RefreshDirectionIcons();
        RefreshVisual();
        RefreshDurabilityText();
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

    public void ReduceDurability()
    {
        if (Data == null || Data.maxDurability == 0) return;
        if (CurrentDurability > 0) CurrentDurability--;
        RefreshDurabilityText();
    }

    public void SetDraggable(bool draggable)
    {
        CardDragHandler drag = GetComponent<CardDragHandler>();
        if (drag != null) drag.enabled = draggable;
    }

    // ── Q/E 회전 입력 ─────────────────────────────────────────

    private void Update()
    {
        if (!_isHovered || Data == null) return;
        if (BattleManager.Instance == null) return;

        bool canRotate = BattleManager.Instance.CurrentTurn == BattleManager.TurnState.PlayerTurn
            && !BattleManager.Instance.IsChainRunning;
        if (!canRotate) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            _rotationOffset = (_rotationOffset + 2) % 8; // 90° 시계
            RefreshDirectionIcons();
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            _rotationOffset = (_rotationOffset + 6) % 8; // 90° 반시계
            RefreshDirectionIcons();
        }
    }

    // ── 런타임 방향 (확률 생성 + 회전 반영) ──────────────────

    public void SetRuntimeDirections(List<CardDirection> dirs)
    {
        _runtimeDirections = dirs;
        _rotationOffset = 0;
        RefreshDirectionIcons();
    }

    public CardInstance GetCardInstance() =>
        new CardInstance { Data = Data, Directions = _runtimeDirections };

    public IEnumerable<CardDirection> GetRuntimeDirections()
    {
        if (Data == null) yield break;

        IEnumerable<CardDirection> source = _runtimeDirections != null
            ? (IEnumerable<CardDirection>)_runtimeDirections
            : Data.GetAllDirections();

        foreach (CardDirection dir in source)
            yield return _rotationOffset == 0 ? dir : RotateDir(dir, _rotationOffset);
    }

    private static CardDirection RotateDir(CardDirection dir, int steps)
    {
        if (dir == CardDirection.None) return CardDirection.None;
        // 열거형 값: Up=1 ~ UpLeft=8 순서로 45° 단위 시계 방향
        int v = (int)dir;
        return (CardDirection)(((v - 1 + steps) % 8) + 1);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (Data != null)
            UI_Tooltip.Instance.Show(Data, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        UI_Tooltip.Instance.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CurrentSlot == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
            BattleManager.Instance.RecallCardToGraveyard(this);
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

        foreach (CardDirection dir in GetRuntimeDirections())
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

    private void RefreshDurabilityText()
    {
        if (_durabilityText == null) return;
        if (Data == null || Data.maxDurability == 0)
        {
            _durabilityText.enabled = false;
            return;
        }
        _durabilityText.enabled = true;
        _durabilityText.text = CurrentDurability.ToString();
    }
}