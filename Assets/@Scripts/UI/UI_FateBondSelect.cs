using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 운명 공동체 카드 배치 직후, 묶을 짝 1장을 그리드에서 선택하는 UI.
/// UI_PreserveSelect의 투명 히트영역 패턴을 차용.
/// 후보: 그리드의 플레이어 일반 카드(자기 자신·캐스팅·축전기 제외).
/// 선택 즉시 양방향 짝을 맺고 콜백으로 배치 흐름을 재개한다.
/// </summary>
public class UI_FateBondSelect : MonoBehaviour
{
    public static UI_FateBondSelect Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _hitAreaRoot;

    private readonly List<GameObject> _hitAreas = new();
    private readonly List<CardView> _highlightedCards = new();
    private RectTransform _rootRect;
    private Canvas _canvas;
    private Action _onComplete;

    private void Awake()
    {
        Instance = this;
        _rootRect = (_hitAreaRoot != null ? _hitAreaRoot : transform) as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 짝 선택을 시작한다. sourceCard는 방금 배치된 운명 공동체 카드.
    /// 후보가 없으면 즉시 onComplete를 호출하고 종료.
    /// </summary>
    public void Begin(CardView sourceCard, Action onComplete)
    {
        _onComplete = onComplete;

        List<CardView> candidates = CollectCandidates(sourceCard);
        if (candidates.Count == 0)
        {
            Finish();
            return;
        }

        foreach (CardView card in candidates)
        {
            card.SetHighlight(true);
            _highlightedCards.Add(card);
            CreateHitArea(sourceCard, card);
        }

        SetVisible(true);
    }

    private List<CardView> CollectCandidates(CardView sourceCard)
    {
        List<CardView> result = new();
        if (GridManager.Instance == null) return result;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            if (card == null || card == sourceCard) continue;
            if (card.IsEnemy) continue;
            if (card.Data == null) continue;
            // 일반 카드만 — 특수 카드 제외
            if (card.Data.isCastingCard || card.Data.isCapacitorCard) continue;
            // 이미 다른 카드와 묶인 경우 제외 (1:1 보장)
            if (card.FateBondPartner != null) continue;
            // 서로의 화살표가 닿으면 왕복 토글이 생기므로 후보에서 제외
            if (ChainExecutor.CardsPointAtEachOther(sourceCard, card)) continue;

            result.Add(card);
        }

        return result;
    }

    private void CreateHitArea(CardView sourceCard, CardView card)
    {
        if (card == null || _rootRect == null) return;

        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect == null) return;

        GameObject hitArea = new("FateBondSelectHitArea",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        hitArea.transform.SetParent(_rootRect, false);

        RectTransform hitRect = hitArea.GetComponent<RectTransform>();
        MatchRectToCard(hitRect, cardRect);

        Image image = hitArea.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;

        Button btn = hitArea.GetComponent<Button>();
        btn.onClick.AddListener(() => OnCardClicked(sourceCard, card));
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

    private void OnCardClicked(CardView sourceCard, CardView target)
    {
        if (sourceCard == null || target == null) { Finish(); return; }

        // 양방향 짝 성립
        sourceCard.SetFateBondPartner(target);
        target.SetFateBondPartner(sourceCard);

        Finish();
    }

    private void Finish()
    {
        foreach (CardView card in _highlightedCards)
            if (card != null)
                card.SetHighlight(false);
        _highlightedCards.Clear();

        ClearHitAreas();
        SetVisible(false);

        Action cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }

    private void ClearHitAreas()
    {
        foreach (GameObject hitArea in _hitAreas)
            if (hitArea != null)
                Destroy(hitArea);
        _hitAreas.Clear();
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}