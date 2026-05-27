using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CardListPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private Transform _cardGrid;
    [SerializeField] private Button _closeButton;

    [Header("Card Prefab")]
    [SerializeField] private GameObject _cardPrefab;

    private readonly List<CardView> _entries = new();

    private void Awake()
    {
        _closeButton.onClick.AddListener(Hide);
        SetVisible(false);
    }

    public void Show(string title, IReadOnlyList<CardData> cards)
    {
        _titleText.text = title;

        foreach (CardView entry in _entries)
            PoolManager.Instance.Return(entry.gameObject);
        _entries.Clear();

        foreach (CardData data in cards)
        {
            CardView card = PoolManager.Instance.Get<CardView>(_cardPrefab, _cardGrid);
            card.Initialize(data);
            card.SetDraggable(false);
            _entries.Add(card);
        }

        SetVisible(true);
    }

    public void Hide()
    {
        foreach (CardView entry in _entries)
            PoolManager.Instance.Return(entry.gameObject);
        _entries.Clear();

        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}