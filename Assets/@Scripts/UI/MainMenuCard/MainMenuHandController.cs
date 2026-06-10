using System.Collections.Generic;
using UnityEngine;

public class MainMenuHandController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _handRoot;
    [SerializeField] private MainMenuCardView _handCardPrefab;

    [Header("Starting Hand")]
    [SerializeField] private List<MainMenuHandCardSeed> _startingHand = new()
    {
        new MainMenuHandCardSeed
        {
            displayName = "위쪽",
            direction = MainMenuCardDirection.Up
        },
        new MainMenuHandCardSeed
        {
            displayName = "왼쪽",
            direction = MainMenuCardDirection.Left
        },
        new MainMenuHandCardSeed
        {
            displayName = "오른쪽",
            direction = MainMenuCardDirection.Right
        }
    };

    private readonly List<MainMenuCardView> _hand = new();

    public void Initialize()
    {
        BuildHand();
    }

    public void RemoveFromHand(MainMenuCardView card)
    {
        if (card != null)
            _hand.Remove(card);
    }

    public void ReturnCard(MainMenuCardView card)
    {
        if (card == null || _handRoot == null) return;

        if (!_hand.Contains(card))
            _hand.Add(card);

        card.transform.SetParent(_handRoot, false);
        card.SetCurrentSlot(null);
        card.SetActivated(false);
        card.SetDraggable(true);
        card.ApplyHandLayout();
    }

    private void BuildHand()
    {
        ClearHand();
        if (_handRoot == null || _handCardPrefab == null) return;

        foreach (MainMenuHandCardSeed seed in _startingHand)
        {
            if (seed == null) continue;

            MainMenuCardView card = Instantiate(_handCardPrefab, _handRoot);
            card.InitializeHand(seed, this);
            card.ApplyHandLayout();
            _hand.Add(card);
        }
    }

    private void ClearHand()
    {
        _hand.Clear();

        if (_handRoot == null) return;

        for (int i = _handRoot.childCount - 1; i >= 0; i--)
            Destroy(_handRoot.GetChild(i).gameObject);
    }
}
