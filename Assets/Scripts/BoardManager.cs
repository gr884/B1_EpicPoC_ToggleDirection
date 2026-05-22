using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform boardRoot;
    [SerializeField] private BoardSlot boardSlotPrefab;
    [SerializeField] private Vector2 slotSpacing = new Vector2(20f, 20f);

    private readonly Dictionary<Vector2Int, BoardSlot> slots = new();

    public IReadOnlyDictionary<Vector2Int, BoardSlot> Slots => slots;

    public void BuildBoard(LevelData levelData, GameManager owner)
    {
        EnsureRefs();
        ClearBoard();

        if (boardRoot == null || boardSlotPrefab == null || levelData == null)
        {
            Debug.LogError("BoardManager: Missing references for board build.");
            return;
        }

        GridLayoutGroup grid = boardRoot.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, levelData.columns);
            grid.spacing = slotSpacing;

            RectTransform slotRect = boardSlotPrefab.GetComponent<RectTransform>();
            if (slotRect != null && slotRect.sizeDelta.x > 0f && slotRect.sizeDelta.y > 0f)
            {
                grid.cellSize = slotRect.sizeDelta;
            }
        }

        for (int row = 0; row < levelData.rows; row++)
        {
            for (int col = 0; col < levelData.columns; col++)
            {
                // Position convention: bottom-left is (0, 0), y increases upward.
                int yFromBottom = (levelData.rows - 1) - row;
                Vector2Int pos = new(col, yFromBottom);
                BoardSlot slot = Instantiate(boardSlotPrefab, boardRoot);
                slot.Setup(pos, owner);
                slots[pos] = slot;
            }
        }
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

    private void EnsureRefs()
    {
        if (boardRoot == null)
        {
            GameObject found = GameObject.Find("BoardRoot");
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
            Destroy(boardRoot.GetChild(i).gameObject);
        }
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
