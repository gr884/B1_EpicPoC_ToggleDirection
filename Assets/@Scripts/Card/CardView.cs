using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

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

    [Header("Preview")]
    [SerializeField] private TMP_Text _previewText; // 예상 데미지/효과 미리보기

    [Header("Cost Feedback")]
    [SerializeField] private float _unaffordableAlpha = 0.4f;

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

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();

        _runtimeState = GetComponent<CardRuntimeState>();
        if (_runtimeState != null)
        {
            _runtimeState.OnChanged += RefreshRuntimeText;
            _runtimeState.OnChanged += RefreshPreviewText;
        }
        CaptureHandLayout();
    }

    void OnDestroy()
    {
        if (_runtimeState != null)
        {
            _runtimeState.OnChanged -= RefreshRuntimeText;
            _runtimeState.OnChanged -= RefreshPreviewText;
        }
        UnsubscribeChainFinished();
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

        if (_previewText != null)
            _previewText.text = "";

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
        bool wasOnGrid = CurrentSlot != null;
        CurrentSlot = slot;

        if (slot != null && !wasOnGrid)
        {
            SubscribeChainFinished();
            RefreshPreviewText();
        }
        else if (slot == null && wasOnGrid)
        {
            UnsubscribeChainFinished();
            if (_previewText != null)
                _previewText.text = "";
        }
    }

    public void SetActivated(bool activated)
    {
        IsActivated = activated;
        RefreshVisual();
        RefreshPreviewText();
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
        float angle = UnityEngine.Random.value > 0.5f ? 10f : -10f;

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

    private void SubscribeChainFinished()
    {
        if (ChainExecutor.Instance != null)
        {
            ChainExecutor.Instance.OnChainFinished += RefreshPreviewText;
            ChainExecutor.Instance.OnToggleCountChanged += RefreshPreviewText;
        }
    }

    private void UnsubscribeChainFinished()
    {
        if (ChainExecutor.Instance != null)
        {
            ChainExecutor.Instance.OnChainFinished -= RefreshPreviewText;
            ChainExecutor.Instance.OnToggleCountChanged -= RefreshPreviewText;
        }
    }

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
        _titleText.text = Data.displayName;
    }

    //* 예상 데미지/효과 미리보기
    private void RefreshPreviewText()
    {
        if (_previewText == null || Data == null || CurrentSlot == null) return;

        int bonusDamage = _runtimeState != null ? _runtimeState.BonusDamage : 0;
        int totemBonus = ChainExecutor.Instance != null ? 
            ChainExecutor.Instance.GetTotemBonus(this) : 0;
        var lines = new System.Text.StringBuilder();

        // Damage + DirectionalDamageBonus가 같이 있으면 한 줄로 합산
        bool hasDirectionalBonus = false;
        int baseDamageTotal = 0;
        foreach (CardEffect e in Data.effects)
        {
            if (e.trigger == EffectTrigger.OnPlaced) continue;
            if (e.effectType == EffectType.DirectionalDamageBonus) hasDirectionalBonus = true;
            if (e.effectType == EffectType.Damage)
                baseDamageTotal += Mathf.Max(1, Mathf.RoundToInt(e.value));
        }

        bool damageLineWritten = false;

        foreach (CardEffect effect in Data.effects)
        {
            if (effect.trigger == EffectTrigger.OnPlaced) continue;

            // Damage는 DirectionalDamageBonus와 합산 처리
            if (effect.effectType == EffectType.Damage && hasDirectionalBonus)
            {
                if (!damageLineWritten)
                {
                    string line = BuildDirectionalPreviewLine(baseDamageTotal, bonusDamage + totemBonus);
                    if (lines.Length > 0) lines.Append("\n");
                    lines.Append(line);
                    damageLineWritten = true;
                }
                continue;
            }

            if (effect.effectType == EffectType.DirectionalDamageBonus) continue;

            string l = BuildPreviewLine(effect, bonusDamage, totemBonus);
            if (!string.IsNullOrEmpty(l))
            {
                if (lines.Length > 0) lines.Append("\n");
                lines.Append(l);
            }
        }

        _previewText.text = lines.ToString();
    }

    private string BuildDirectionalPreviewLine(int baseDamage, int bonusDamage)
    {
        int neighborDamage = 0;
        if (CurrentSlot != null && GridManager.Instance != null)
        {
            foreach (CardDirection dir in Data.GetAllDirections())
            {
                GridSlot n = GridManager.Instance.GetNeighbor(CurrentSlot, dir);
                if (n == null || n.OccupiedCard == null || n.OccupiedCard.Data == null) continue;
                
                var neighborRuntime = n.OccupiedCard.GetComponent<CardRuntimeState>();
                
                int neighborTotemBonus = ChainExecutor.Instance != null ? 
                    ChainExecutor.Instance.GetTotemBonus(n.OccupiedCard) : 0;

                // 해당 방향의 기물이 가지는 효과 확인
                foreach (CardEffect e in n.OccupiedCard.Data.effects)
                {
                    if (e.effectType != EffectType.Damage && 
                        e.effectType != EffectType.CounterDamage && 
                        e.effectType != EffectType.PopularityDamage)
                        continue;

                    // 해당 효과의 기본 데미지
                    int base_ = Mathf.Max(1, Mathf.RoundToInt(e.value));
                    // 기본 데미지에 더해지는 토템의 보너스를 입힌 데미지
                    int totalBase = base_ + neighborTotemBonus;
                    // 부가 데미지가 적용된 데미지
                    int modifiedBase = neighborRuntime != null ? neighborRuntime.GetModifiedDamage(totalBase) : totalBase;

                    // 카운트 기물이라면 토글 횟수 추가
                    if (e.effectType == EffectType.CounterDamage)
                    {
                        int toggleCount = ChainExecutor.Instance != null ? ChainExecutor.Instance.TurnToggleCount : 0;
                        neighborDamage += modifiedBase + toggleCount;
                    }
                    // 인싸 기물이라면 곱연산해 적용
                    else if (e.effectType == EffectType.PopularityDamage)
                    {
                        int popCount = 0;
                        foreach (CardDirection d in Enum.GetValues(typeof(CardDirection)))
                        {
                            if (d == CardDirection.None) continue;
                            GridSlot popNeighbor = GridManager.Instance.GetNeighbor(n.OccupiedCard.CurrentSlot, d);
                            if (popNeighbor != null && !popNeighbor.IsEmpty) popCount++;
                        }
                        neighborDamage += modifiedBase * popCount;
                    }
                    // 기본 데미지 기물이라면 기본 추가
                    else
                    {
                        neighborDamage += modifiedBase;
                    }
                }
            }
        }

        int totalBaseDamage = baseDamage + bonusDamage;

        if (totalBaseDamage > 0)
            return $"{totalBaseDamage} + ({neighborDamage})";
        return $"({neighborDamage})";
    }

    private string BuildPreviewLine(CardEffect effect, int bonusDamage, int totemBonus)
    {
        switch (effect.effectType)
        {
            case EffectType.Damage:
                {
                    int baseVal = Mathf.Max(1, Mathf.RoundToInt(effect.value));
                    int totalBonus = bonusDamage + totemBonus;
                    return totalBonus > 0 ? $"{baseVal + totalBonus}" : $"{baseVal}";
                }
            case EffectType.FinisherDamage:
                {
                    int onCount = 0;
                    if (GridManager.Instance != null)
                        foreach (GridSlot s in GridManager.Instance.Slots.Values)
                            if (s.OccupiedCard != null && s.OccupiedCard.IsActivated)
                                onCount++;
                    
                    // (기본 값 + 추가되는 값) * 켜진 카운트 를 합산해 리턴
                    int baseVal = Mathf.RoundToInt(effect.value);
                    int totalBonus = bonusDamage + totemBonus;
                    return $"{baseVal + totalBonus} × {onCount}";
                }
            case EffectType.CounterDamage:
                {
                    int toggleCount = ChainExecutor.Instance != null ? ChainExecutor.Instance.TurnToggleCount : 0;
                    int baseVal = Mathf.RoundToInt(effect.value);
                    int totalBonus = bonusDamage + totemBonus;
                    return $"{baseVal + totalBonus} + {toggleCount}";
                }
            case EffectType.PopularityDamage:
                {
                    int neighborCount = 0;
                    if (CurrentSlot != null && GridManager.Instance != null)
                        foreach (CardDirection dir in Enum.GetValues(typeof(CardDirection)))
                        {
                            if (dir == CardDirection.None) continue;
                            GridSlot n = GridManager.Instance.GetNeighbor(CurrentSlot, dir);
                            if (n != null && !n.IsEmpty) neighborCount++;
                        }

                    // 기본 데미지 + 토템 버프 + 보너스 데미지 합산
                    int baseVal = Mathf.RoundToInt(effect.value);
                    int totalBonus = bonusDamage + totemBonus;

                    return $"{baseVal + totalBonus} × {neighborCount}";
                }
            case EffectType.Defense:
                return $"{Mathf.Max(1, Mathf.RoundToInt(effect.value))}";
            case EffectType.Heal:
                return $"{Mathf.Max(1, Mathf.RoundToInt(effect.value))}";
            case EffectType.DefenseOnOff:
                return IsActivated
                    ? $"{Mathf.RoundToInt(effect.value)}"
                    : $"{Mathf.RoundToInt(effect.secondaryValue)}";
            default:
                return "";
        }
    }
}