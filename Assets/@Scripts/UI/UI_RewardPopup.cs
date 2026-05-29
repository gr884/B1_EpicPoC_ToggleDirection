using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_RewardPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Transform _cardContainer;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _skipButton;

    [Header("Card Pool")]
    [SerializeField] private List<CardData> _cardPool = new();

    [Header("Settings")]
    [SerializeField] private int _rewardCount = 3;

    private readonly List<CardView> _displayedCards = new();
    private CardData _selectedCard;

    private void Start()
    {
        _confirmButton.onClick.AddListener(HandleConfirm);
        _skipButton.onClick.AddListener(HandleSkip);
        BattleManager.Instance.OnBattleEnded += Show;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnBattleEnded -= Show;
    }

    private void Show()
    {
        _selectedCard = null;
        _confirmButton.interactable = false;
        ClearCards();

        List<CardData> picks = PickRandom(_cardPool, _rewardCount);
        foreach (CardData data in picks)
        {
            CardView card = PoolManager.Instance.Get<CardView>(_cardPrefab, _cardContainer);
            card.Initialize(data);
            card.SetDraggable(false);

            Button btn = card.GetComponent<Button>();
            if (btn == null) btn = card.gameObject.AddComponent<Button>();

            CardData captured = data;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => HandleCardClicked(captured, card));

            _displayedCards.Add(card);
        }

        SetVisible(true);
    }

    private void HandleCardClicked(CardData data, CardView cardView)
    {
        _selectedCard = data;
        _confirmButton.interactable = true;

        foreach (CardView c in _displayedCards)
            c.SetSelected(c == cardView);
    }

    private void HandleConfirm()
    {
        if (_selectedCard == null) return;
        CardManager.Instance.AddCard(_selectedCard);
        Debug.Log($"[UI_RewardPopup] 카드 선택 확정 — {_selectedCard.displayName}");
        Close();
    }

    private void HandleSkip()
    {
        Debug.Log("[UI_RewardPopup] 보상 스킵");
        Close();
    }

    private void Close()
    {
        ClearCards();
        SetVisible(false);
        GameFlowManager.Instance.OnRewardClosed();
    }

    private void ClearCards()
    {
        foreach (CardView card in _displayedCards)
        {
            card.SetSelected(false);
            Button btn = card.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveAllListeners();
            PoolManager.Instance.Return(card.gameObject);
        }
        _displayedCards.Clear();
        _selectedCard = null;
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private static List<CardData> PickRandom(List<CardData> pool, int count)
    {
        List<CardData> copy = new(pool);
        List<CardData> result = new();

        count = Mathf.Min(count, copy.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, copy.Count);
            result.Add(copy[idx]);
            copy.RemoveAt(idx);
        }
        return result;
    }
}