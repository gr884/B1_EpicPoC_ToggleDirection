using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : SingletonBehaviour<GridManager>
{
    [Header("Refs")]
    [SerializeField] private RectTransform _gridRoot;
    [SerializeField] private GridSlot _slotPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _maxRows = 7;
    [SerializeField] private int _maxColumns = 7;
    [SerializeField] private int _startRows = 3;
    [SerializeField] private int _startColumns = 3;

    public int ActiveRows { get; private set; }
    public int ActiveColumns { get; private set; }

    private readonly Dictionary<Vector2Int, GridSlot> _slots = new();
    public IReadOnlyDictionary<Vector2Int, GridSlot> Slots => _slots;

    public void Init()
    {
        BuildGrid();
        Debug.Log("[GridManager] Init");
    }

    public void BuildGrid()
    {
        ClearGrid();

        ActiveRows = _startRows;
        ActiveColumns = _startColumns;

        GridLayoutGroup layout = _gridRoot.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = _maxColumns;
        }

        int colOffset = (_maxColumns - _startColumns) / 2;
        int rowOffset = (_maxRows - _startRows) / 2;

        for (int row = 0; row < _maxRows; row++)
        {
            for (int col = 0; col < _maxColumns; col++)
            {
                int yFromBottom = (_maxRows - 1) - row;
                Vector2Int pos = new(col, yFromBottom);

                GridSlot slot = Instantiate(_slotPrefab, _gridRoot);
                slot.Setup(pos);

                bool isActive = col >= colOffset && col < colOffset + _startColumns
                             && yFromBottom >= rowOffset && yFromBottom < rowOffset + _startRows;
                slot.SetLocked(!isActive);

                _slots[pos] = slot;
            }
        }

        Debug.Log($"[GridManager] 그리드 생성 완료 (활성: {_startRows}x{_startColumns} / 최대: {_maxRows}x{_maxColumns})");
    }

    // ── 확장 ───────────────────────────────────────────────

    public void ExpandGrid(int newRows, int newColumns)
    {
        int clampedRows = Mathf.Clamp(newRows, ActiveRows, _maxRows);
        int clampedCols = Mathf.Clamp(newColumns, ActiveColumns, _maxColumns);

        if (clampedRows == ActiveRows && clampedCols == ActiveColumns) return;

        ActiveRows = clampedRows;
        ActiveColumns = clampedCols;

        int colOffset = (_maxColumns - ActiveColumns) / 2;
        int rowOffset = (_maxRows - ActiveRows) / 2;

        foreach (var kv in _slots)
        {
            bool isActive = kv.Key.x >= colOffset && kv.Key.x < colOffset + ActiveColumns
                         && kv.Key.y >= rowOffset && kv.Key.y < rowOffset + ActiveRows;
            kv.Value.SetLocked(!isActive);
        }

        Debug.Log($"[GridManager] 그리드 확장 → {ActiveRows}x{ActiveColumns}");
    }

    // ── 조회 ───────────────────────────────────────────────

    public GridSlot GetSlot(Vector2Int position)
    {
        _slots.TryGetValue(position, out GridSlot slot);
        return slot;
    }

    public GridSlot GetNeighbor(GridSlot origin, ItemDirection direction)
    {
        if (origin == null || direction == ItemDirection.None) return null;

        GridSlot neighbor = GetSlot(origin.Position + DirectionToDelta(direction));
        if (neighbor != null && neighbor.IsLocked) return null;

        return neighbor;
    }

    private void ClearGrid()
    {
        _slots.Clear();
        for (int i = _gridRoot.childCount - 1; i >= 0; i--)
            Destroy(_gridRoot.GetChild(i).gameObject);
    }

    private static Vector2Int DirectionToDelta(ItemDirection direction) => direction switch
    {
        ItemDirection.Up => new Vector2Int(0, 1),
        ItemDirection.UpRight => new Vector2Int(1, 1),
        ItemDirection.Right => new Vector2Int(1, 0),
        ItemDirection.DownRight => new Vector2Int(1, -1),
        ItemDirection.Down => new Vector2Int(0, -1),
        ItemDirection.DownLeft => new Vector2Int(-1, -1),
        ItemDirection.Left => new Vector2Int(-1, 0),
        ItemDirection.UpLeft => new Vector2Int(-1, 1),
        _ => Vector2Int.zero
    };

    protected override void Dispose()
    {
        base.Dispose();
    }
}