using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuGridController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _gridRoot;
    [SerializeField] private MainMenuGridSlot _slotPrefab;
    [SerializeField] private MainMenuCardView _menuCardPrefab;

    [Header("Grid")]
    [SerializeField] private Vector2Int _centerPosition = new(1, 1);

    [Header("Fixed Cards")]
    [SerializeField] private List<MainMenuFixedCardSeed> _fixedCards = new()
    {
        new MainMenuFixedCardSeed
        {
            displayName = "게임 시작",
            position = new Vector2Int(1, 2),
            action = MainMenuCardAction.GameStart
        },
        new MainMenuFixedCardSeed
        {
            displayName = "옵션",
            position = new Vector2Int(0, 1),
            action = MainMenuCardAction.Options
        },
        new MainMenuFixedCardSeed
        {
            displayName = "크레딧",
            position = new Vector2Int(2, 1),
            action = MainMenuCardAction.Credits
        }
    };

    private const int Rows = 3;
    private const int Columns = 3;

    private readonly Dictionary<Vector2Int, MainMenuGridSlot> _slots = new();
    private readonly List<MainMenuCardView> _fixedCardViews = new();

    public event Action<MainMenuCardView> OnHandCardPlaced;

    public MainMenuGridSlot CenterSlot => GetSlot(_centerPosition);

    public void Initialize()
    {
        BuildGrid();
        PlaceFixedCards();
    }

    public bool TryPlaceHandCard(MainMenuCardView card, MainMenuGridSlot targetSlot)
    {
        if (card == null || targetSlot == null) return false;
        if (card.IsFixed) return false;
        if (!targetSlot.IsCenterSlot) return false;
        if (!targetSlot.IsEmpty) return false;

        card.OwnerHand?.RemoveFromHand(card);
        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        OnHandCardPlaced?.Invoke(card);
        return true;
    }

    public MainMenuGridSlot GetSlot(Vector2Int position)
    {
        _slots.TryGetValue(position, out MainMenuGridSlot slot);
        return slot;
    }

    public MainMenuGridSlot GetNeighbor(MainMenuGridSlot origin, MainMenuCardDirection direction)
    {
        if (origin == null || direction == MainMenuCardDirection.None) return null;
        return GetSlot(origin.Position + DirectionToDelta(direction));
    }

    public void ResetFixedCardsOff()
    {
        foreach (MainMenuCardView card in _fixedCardViews)
            if (card != null)
                card.SetActivated(false);
    }

    private void BuildGrid()
    {
        if (_gridRoot == null || _slotPrefab == null) return;

        ClearGrid();

        GridLayoutGroup layout = _gridRoot.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;
        }

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                int yFromBottom = (Rows - 1) - row;
                Vector2Int position = new(col, yFromBottom);
                MainMenuGridSlot slot = Instantiate(_slotPrefab, _gridRoot);
                slot.Setup(position, position == _centerPosition, this);
                _slots[position] = slot;
            }
        }
    }

    private void PlaceFixedCards()
    {
        if (_menuCardPrefab == null) return;

        foreach (MainMenuFixedCardSeed seed in _fixedCards)
        {
            if (seed == null) continue;
            if (seed.position == _centerPosition)
            {
                Debug.LogWarning("[MainMenuGridController] 중앙 슬롯에는 고정 메뉴 카드를 배치할 수 없습니다.");
                continue;
            }

            MainMenuGridSlot slot = GetSlot(seed.position);
            if (slot == null || !slot.IsEmpty) continue;

            MainMenuCardView card = Instantiate(_menuCardPrefab, slot.transform);
            card.InitializeFixed(seed);
            slot.AssignCard(card);
            _fixedCardViews.Add(card);
        }
    }

    private void ClearGrid()
    {
        _slots.Clear();
        _fixedCardViews.Clear();

        if (_gridRoot == null) return;

        for (int i = _gridRoot.childCount - 1; i >= 0; i--)
            Destroy(_gridRoot.GetChild(i).gameObject);
    }

    private static Vector2Int DirectionToDelta(MainMenuCardDirection direction) => direction switch
    {
        MainMenuCardDirection.Up => new Vector2Int(0, 1),
        MainMenuCardDirection.UpRight => new Vector2Int(1, 1),
        MainMenuCardDirection.Right => new Vector2Int(1, 0),
        MainMenuCardDirection.DownRight => new Vector2Int(1, -1),
        MainMenuCardDirection.Down => new Vector2Int(0, -1),
        MainMenuCardDirection.DownLeft => new Vector2Int(-1, -1),
        MainMenuCardDirection.Left => new Vector2Int(-1, 0),
        MainMenuCardDirection.UpLeft => new Vector2Int(-1, 1),
        _ => Vector2Int.zero
    };
}
