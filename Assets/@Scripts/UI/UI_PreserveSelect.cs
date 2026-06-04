using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 턴 확정 후 그리드 카드 보존 선택 UI.
/// 플레이어가 유지할 카드를 최대 N장 클릭으로 선택.
/// 선택 시 Preserve 스택 1 부여, 해제 시 제거.
/// </summary>
public class UI_PreserveSelect : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private RectTransform _hitAreaRoot;

    [Header("Settings")]
    [SerializeField] private int _maxPreserveCount = 2;

    private readonly List<CardView> _selectedCards = new();
    private readonly List<GameObject> _hitAreas = new();
    private RectTransform _rootRect;
    private Canvas _canvas;

    private void Awake()
    {
        _rootRect = (_hitAreaRoot != null ? _hitAreaRoot : transform) as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        _confirmButton.onClick.AddListener(OnConfirmClicked);
        BattleManager.Instance.OnPhaseChanged += OnPhaseChanged;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(BattleManager.BattlePhase phase)
    {
        if (phase == BattleManager.BattlePhase.PreserveSelect)
            Show();
        else
            Hide();
    }

    private void Show()
    {
        _selectedCards.Clear();

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.IsEmpty) continue;
            CardView card = slot.OccupiedCard;
            if (card.IsEnemy) continue;

            card.SetSelected(false);
            CreateHitArea(card);
        }

        if (_confirmButton != null)
            _confirmButton.transform.SetAsLastSibling();

        RefreshCount();
        SetVisible(true);
    }

    private void Hide()
    {
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.IsEmpty) continue;
            CardView card = slot.OccupiedCard;
            if (card.IsEnemy) continue;
            card.SetSelected(false);
        }

        _selectedCards.Clear();
        ClearHitAreas();
        SetVisible(false);
    }

    private void CreateHitArea(CardView card)
    {
        if (card == null || _rootRect == null) return;

        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect == null) return;

        GameObject hitArea = new("PreserveSelectHitArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        hitArea.transform.SetParent(_rootRect, false);

        RectTransform hitRect = hitArea.GetComponent<RectTransform>();
        MatchRectToCard(hitRect, cardRect);

        Image image = hitArea.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        Button btn = hitArea.GetComponent<Button>();
        btn.onClick.AddListener(() => OnCardClicked(card));
        _hitAreas.Add(hitArea);
    }

    private void MatchRectToCard(RectTransform hitRect, RectTransform cardRect)
    {
        Vector3[] corners = new Vector3[4];
        cardRect.GetWorldCorners(corners);

        Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootRect,
            RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]),
            uiCamera,
            out Vector2 min);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootRect,
            RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]),
            uiCamera,
            out Vector2 max);

        hitRect.anchorMin = new Vector2(0.5f, 0.5f);
        hitRect.anchorMax = new Vector2(0.5f, 0.5f);
        hitRect.pivot = new Vector2(0.5f, 0.5f);
        hitRect.anchoredPosition = (min + max) * 0.5f;
        hitRect.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
    }

    private void ClearHitAreas()
    {
        foreach (GameObject hitArea in _hitAreas)
            if (hitArea != null)
                Destroy(hitArea);

        _hitAreas.Clear();
    }

    private void OnCardClicked(CardView card)
    {
        if (_selectedCards.Contains(card))
        {
            // 선택 해제 — Preserve 스택 제거
            card.RemovePreserve(1);
            card.SetSelected(false);
            _selectedCards.Remove(card);
        }
        else
        {
            if (_selectedCards.Count >= _maxPreserveCount) return;

            // 선택 — Preserve 스택 1 부여
            card.AddPreserve(1);
            card.SetSelected(true);
            _selectedCards.Add(card);
        }

        RefreshCount();
    }

    private void OnConfirmClicked()
    {
        BattleManager.Instance.ConfirmPreserveSelect();
    }

    private void RefreshCount()
    {
        if (_countText != null)
            _countText.text = $"{_selectedCards.Count} / {_maxPreserveCount}";
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
