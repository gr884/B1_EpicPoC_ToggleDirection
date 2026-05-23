using System.Collections.Generic;
using UnityEngine;

public class BoardManager : SingletonBehaviour<BoardManager>
{
    [Header("Refs")]
    [SerializeField] private BoardSlot _boardSlotPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _rows = 4;
    [SerializeField] private int _columns = 5;
    [SerializeField] private float _slotSize = 1.2f;
    [SerializeField] private Vector2 _boardOrigin = Vector2.zero;

    public int Rows => _rows;
    public int Columns => _columns;

    private readonly Dictionary<Vector2Int, BoardSlot> _slots = new();
    public IReadOnlyDictionary<Vector2Int, BoardSlot> Slots => _slots;

    public void Init()
    {
        Debug.Log("[BoardManager] Init");
    }

    public void BuildBoard()
    {
        ClearBoard();

        if (_boardSlotPrefab == null)
        {
            Debug.LogError("[BoardManager] BoardSlot 프리팹이 없습니다.");
            return;
        }

        float offsetX = (_columns - 1) * _slotSize * 0.5f;
        float offsetY = (_rows - 1) * _slotSize * 0.5f;

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _columns; col++)
            {
                int yFromBottom = (_rows - 1) - row;
                Vector2Int pos = new(col, yFromBottom);

                float worldX = _boardOrigin.x + col * _slotSize - offsetX;
                float worldY = _boardOrigin.y + yFromBottom * _slotSize - offsetY;

                BoardSlot slot = Instantiate(_boardSlotPrefab, new Vector3(worldX, worldY, 0f), Quaternion.identity, transform);
                slot.Setup(pos);
                _slots[pos] = slot;
            }
        }

        Debug.Log($"[BoardManager] 보드 생성 완료 {_rows}x{_columns}");
    }

    public BoardSlot GetSlot(Vector2Int position)
    {
        _slots.TryGetValue(position, out BoardSlot slot);
        return slot;
    }

    public BoardSlot GetNeighbor(BoardSlot origin, AbilityDirection direction)
    {
        if (origin == null || direction == AbilityDirection.None) return null;

        Vector2Int delta = DirectionToDelta(direction);
        return GetSlot(origin.Position + delta);
    }

    private void ClearBoard()
    {
        _slots.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private static Vector2Int DirectionToDelta(AbilityDirection direction) => direction switch
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

    protected override void Dispose()
    {
        base.Dispose();
    }
}