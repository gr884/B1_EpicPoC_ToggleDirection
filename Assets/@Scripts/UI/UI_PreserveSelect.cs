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

    [Header("Settings")]
    [SerializeField] private int _maxPreserveCount = 2;

    private readonly List<CardView> _selectedCards = new();

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
            RegisterCardClick(card);
        }

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
            UnregisterCardClick(card);
        }

        _selectedCards.Clear();
        SetVisible(false);
    }

    private void RegisterCardClick(CardView card)
    {
        Button btn = card.GetComponent<Button>();
        if (btn == null) btn = card.gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnCardClicked(card));
    }

    private void UnregisterCardClick(CardView card)
    {
        Button btn = card.GetComponent<Button>();
        if (btn != null) btn.onClick.RemoveAllListeners();
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