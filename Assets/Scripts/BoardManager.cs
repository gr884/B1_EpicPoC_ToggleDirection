using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private BoardSlot boardSlotPrefab;
    [SerializeField] private Vector2 slotSpacing = new Vector2(20f, 20f);

    [Header("Sizing")]
    [SerializeField] private bool fitSlotsToPanelHeight = true;
    [SerializeField] private float panelVerticalPadding;

    private readonly Dictionary<Vector2Int, BoardSlot> slots = new();

    public IReadOnlyDictionary<Vector2Int, BoardSlot> Slots => slots;

    public void BuildBoard(GridDataSO gridData, GameManager owner)
    {
        EnsureRefs();
        ClearBoard();

        if (boardRoot == null || boardSlotPrefab == null || gridData == null)
        {
            Debug.LogError("BoardManager: Missing references for board build.");
            return;
        }

        GridLayoutGroup grid = boardRoot.GetComponent<GridLayoutGroup>();
        Vector2 slotSize = GetPrefabSlotSize();
        slotSize = CalculateSlotSizeForPanelHeight(slotSize, gridData.rows);
        bool hasDisabledCells = gridData.disabledCells != null && gridData.disabledCells.Count > 0;
        if (grid != null && !hasDisabledCells)
        {
            grid.enabled = true;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, gridData.columns);
            grid.spacing = slotSpacing;
            grid.cellSize = slotSize;
        }
        else
        {
            if (grid != null)
            {
                grid.enabled = false;
            }
        }

        for (int row = 0; row < gridData.rows; row++)
        {
            for (int col = 0; col < gridData.columns; col++)
            {
                // Position convention: bottom-left is (0, 0), y increases upward.
                int yFromBottom = (gridData.rows - 1) - row;
                Vector2Int pos = new(col, yFromBottom);
                if (!gridData.IsCellEnabled(pos))
                {
                    continue;
                }

                BoardSlot slot = Instantiate(boardSlotPrefab, boardRoot);
                slot.Setup(pos, owner);
                if (grid == null || hasDisabledCells)
                {
                    PositionSlotManually(slot, row, col, gridData.rows, gridData.columns, slotSize);
                }
                slots[pos] = slot;
            }
        }

        Debug.Log($"BoardManager: BuildBoard rows={gridData.rows}, columns={gridData.columns}, slots={slots.Count}, slotSize={slotSize}");
    }

    public BoardSlot GetSlot(Vector2Int position)
    {
        slots.TryGetValue(position, out BoardSlot slot);
        return slot;
    }

    public BoardSlot GetNeighbor(BoardSlot origin, AbilityDirection direction)
    {
        if (origin == null || direction == AbilityDirection.None)
        {
            return null;
        }

        Vector2Int delta = DirectionToDelta(direction);
        Vector2Int targetPos = origin.Position + delta;
        return GetSlot(targetPos);
    }

    public bool AreAllPlacedCardsActivated()
    {
        bool hasCard = false;

        foreach (BoardSlot slot in slots.Values)
        {
            if (slot.OccupiedCard == null)
            {
                continue;
            }

            hasCard = true;
            if (!slot.OccupiedCard.IsActivated)
            {
                return false;
            }
        }

        return hasCard;
    }

    public List<Card> GetAllPlacedCards()
    {
        List<Card> cards = new();

        foreach (BoardSlot slot in slots.Values)
        {
            if (slot == null || slot.OccupiedCard == null)
            {
                continue;
            }

            cards.Add(slot.OccupiedCard);
        }

        return cards;
    }

    public List<BoardSlot> GetEmptySlots()
    {
        List<BoardSlot> emptySlots = new();

        foreach (BoardSlot slot in slots.Values)
        {
            if (slot != null && slot.IsEmpty())
            {
                emptySlots.Add(slot);
            }
        }

        return emptySlots;
    }

    public int CountActivatedCards(CardTeam team)
    {
        int count = 0;

        foreach (BoardSlot slot in slots.Values)
        {
            Card card = slot != null ? slot.OccupiedCard : null;
            if (card != null && card.Team == team && card.IsActivated)
            {
                count++;
            }
        }

        return count;
    }

    public int CountInactiveCards(CardTeam team)
    {
        int count = 0;

        foreach (BoardSlot slot in slots.Values)
        {
            Card card = slot != null ? slot.OccupiedCard : null;
            if (card != null && card.Team == team && !card.IsActivated)
            {
                count++;
            }
        }

        return count;
    }

    public void RemoveAllCards()
    {
        foreach (BoardSlot slot in slots.Values)
        {
            if (slot == null)
            {
                continue;
            }

            Card card = slot.OccupiedCard;
            slot.ClearCard();

            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
    }

    private void EnsureRefs()
    {
        if (boardRoot == null)
        {
            GameObject found = GameObject.Find("BoardRoot");
            if (found == null)
            {
                found = GameObject.Find("MiddleGridRoot");
            }

            if (found != null)
            {
                boardRoot = found.GetComponent<RectTransform>();
            }
        }
    }

    private void ClearBoard()
    {
        slots.Clear();

        if (boardRoot == null)
        {
            return;
        }

        for (int i = boardRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = boardRoot.GetChild(i);
            if (child.GetComponent<BoardSlot>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void PositionSlotManually(BoardSlot slot, int row, int col, int rows, int columns, Vector2 slotSize)
    {
        RectTransform rect = slot != null ? slot.GetComponent<RectTransform>() : null;
        if (rect == null)
        {
            return;
        }

        float stepX = slotSize.x + slotSpacing.x;
        float stepY = slotSize.y + slotSpacing.y;
        float startX = -((columns - 1) * 0.5f) * stepX;
        float startY = ((rows - 1) * 0.5f) * stepY;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = slotSize;
        rect.anchoredPosition = new Vector2(startX + (col * stepX), startY - (row * stepY));
    }

    private Vector2 GetPrefabSlotSize()
    {
        RectTransform slotRect = boardSlotPrefab != null ? boardSlotPrefab.GetComponent<RectTransform>() : null;
        if (slotRect != null && slotRect.sizeDelta.x > 0f && slotRect.sizeDelta.y > 0f)
        {
            return slotRect.sizeDelta;
        }

        return new Vector2(170f, 250f);
    }

    private Vector2 CalculateSlotSizeForPanelHeight(Vector2 baseSize, int rows)
    {
        if (!fitSlotsToPanelHeight || boardRoot == null || rows <= 0 || baseSize.y <= 0f)
        {
            return baseSize;
        }

        RectTransform panel = FindPanelRect(boardRoot);
        if (panel == null)
        {
            return baseSize;
        }

        float panelHeight = GetRectHeight(panel);
        if (panelHeight <= 0f)
        {
            return baseSize;
        }

        float spacingHeight = Mathf.Max(0, rows - 1) * slotSpacing.y;
        float availableHeight = Mathf.Max(1f, panelHeight - spacingHeight - Mathf.Max(0f, panelVerticalPadding));
        float targetHeight = availableHeight / rows;
        float aspect = baseSize.x / baseSize.y;
        return new Vector2(targetHeight * aspect, targetHeight);
    }

    private static RectTransform FindPanelRect(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == "Panel")
            {
                return child.GetComponent<RectTransform>();
            }
        }

        return root;
    }

    private static float GetRectHeight(RectTransform rectTransform)
    {
        float height = rectTransform.rect.height;
        if (height > 0f)
        {
            return height;
        }

        return Mathf.Abs(rectTransform.sizeDelta.y);
    }

    private static Vector2Int DirectionToDelta(AbilityDirection direction)
    {
        return direction switch
        {
            AbilityDirection.Up => new Vector2Int(0, 1),
            AbilityDirection.UpRight => new Vector2Int(1, 1),
            AbilityDirection.Right => new Vector2Int(1, 0),
            AbilityDirection.DownRight => new Vector2Int(1, -1),
            AbilityDirection.Down => new Vector2Int(0, -1),
            AbilityDirection.DownLeft => new Vector2Int(-1, -1),
            AbilityDirection.Left => new Vector2Int(-1, 0),
            AbilityDirection.UpLeft => new Vector2Int(-1, 1),
            _ => Vector2Int.zero
        };
    }
}
