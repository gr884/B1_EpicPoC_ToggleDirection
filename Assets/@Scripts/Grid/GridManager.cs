using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : SingletonBehaviour<GridManager>
{
    [Header("Refs")]
    [SerializeField] private RectTransform _gridRoot;
    [SerializeField] private GridSlot _slotPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _rows = 4;
    [SerializeField] private int _columns = 4;

    public int Rows => _rows;
    public int Columns => _columns;

    private readonly Dictionary<Vector2Int, GridSlot> _slots = new();
    public IReadOnlyDictionary<Vector2Int, GridSlot> Slots => _slots;
    private readonly List<GridSlot> _pendingDirectionalImpactSlots = new();

    public void Init()
    {
        BuildGrid();
        Debug.Log("[GridManager] Init");
    }

    public void SetGridSize(int rows, int columns)
    {
        _rows = Mathf.Max(1, rows);
        _columns = Mathf.Max(1, columns);
        BuildGrid();
    }

    public void BuildGrid()
    {
        ClearPendingDirectionalImpact();
        ClearGrid();

        GridLayoutGroup layout = _gridRoot.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = _columns;
        }

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _columns; col++)
            {
                int yFromBottom = (_rows - 1) - row;
                Vector2Int pos = new(col, yFromBottom);

                GridSlot slot = Instantiate(_slotPrefab, _gridRoot);
                slot.Setup(pos);
                _slots[pos] = slot;
            }
        }

        Debug.Log($"[GridManager] 그리드 생성 완료 ({_rows}x{_columns})");
    }

    // ── 리셋 ───────────────────────────────────────────────

    public void ResetCards()
    {
        foreach (GridSlot slot in _slots.Values)
            if (!slot.IsEmpty)
                slot.ClearCard();

        Debug.Log("[GridManager] 그리드 카드 리셋");
    }

    // ── 적 카드 배치 (특수 기믹용) ─────────────────────────

    /// <summary>
    /// 특정 위치에 적 카드 한 장 배치.
    /// </summary>
    public CardView PlaceEnemyCard(CardData data, GameObject cardPrefab, Vector2Int position, bool startsActivated = true)
    {
        GridSlot slot = GetSlot(position);
        if (slot == null || !slot.IsEmpty) return null;
        return SpawnEnemyCard(data, cardPrefab, slot, startsActivated);
    }

    /// <summary>
    /// 빈 슬롯 중 랜덤한 위치에 적 카드 한 장 배치.
    /// </summary>
    public CardView PlaceEnemyCardRandom(CardData data, GameObject cardPrefab, bool startsActivated = true)
    {
        List<GridSlot> emptySlots = GetEmptySlots();
        if (emptySlots.Count == 0) return null;

        Shuffle(emptySlots);
        return SpawnEnemyCard(data, cardPrefab, emptySlots[0], startsActivated);
    }

    public int GetEnemyCardCount()
    {
        int count = 0;
        foreach (GridSlot slot in _slots.Values)
            if (!slot.IsEmpty && slot.OccupiedCard.IsEnemy)
                count++;
        return count;
    }

    // ── 조회 ───────────────────────────────────────────────

    public GridSlot GetSlot(Vector2Int position)
    {
        _slots.TryGetValue(position, out GridSlot slot);
        return slot;
    }

    public GridSlot GetNeighbor(GridSlot origin, CardDirection direction)
    {
        if (origin == null || direction == CardDirection.None) return null;
        return GetSlot(origin.Position + DirectionToDelta(direction));
    }

    public List<GridSlot> GetEmptySlots()
    {
        List<GridSlot> result = new();
        foreach (GridSlot slot in _slots.Values)
            if (slot.IsEmpty) result.Add(slot);
        return result;
    }

    public List<GridSlot> GetDirectionalImpactSlots(CardView sourceCard, GridSlot attachSlot)
    {
        List<GridSlot> result = new();
        if (sourceCard?.Data == null || attachSlot == null) return result;
        if (sourceCard.Data.isRecaller) return result;

        int range = Mathf.Max(1, sourceCard.Data.range);
        foreach (CardDirection dir in sourceCard.Data.GetAllDirections())
        {
            GridSlot current = attachSlot;
            for (int i = 0; i < range; i++)
            {
                GridSlot next = GetNeighbor(current, dir);
                if (next == null) break;

                if (!result.Contains(next))
                    result.Add(next);
                current = next;
            }
        }

        return result;
    }

    public void ShowPendingDirectionalImpact(CardView sourceCard, GridSlot attachSlot)
    {
        ClearPendingDirectionalImpact();
        if (sourceCard == null || attachSlot == null) return;
        if (CardManager.Instance == null || !CardManager.Instance.CanPreviewPlaceCard(sourceCard, attachSlot)) return;

        CreatePendingPlacementPreviewEffect(attachSlot);

        List<GridSlot> targets = GetDirectionalImpactSlots(sourceCard, attachSlot);
        foreach (GridSlot target in targets)
            CreatePendingDirectionalImpactEffect(target);
    }

    public void ClearPendingDirectionalImpact()
    {
        foreach (GridSlot slot in _pendingDirectionalImpactSlots)
            if (slot != null)
                slot.SetHighlight(false);
        _pendingDirectionalImpactSlots.Clear();
    }

    // ── 내부 ───────────────────────────────────────────────

    private void CreatePendingPlacementPreviewEffect(GridSlot targetSlot)
    {
        CreatePendingDirectionalImpactEffect(targetSlot);
    }

    private void CreatePendingDirectionalImpactEffect(GridSlot targetSlot)
    {
        if (targetSlot == null || _pendingDirectionalImpactSlots.Contains(targetSlot)) return;

        targetSlot.SetHighlight(true);
        _pendingDirectionalImpactSlots.Add(targetSlot);
    }

    private CardView SpawnEnemyCard(CardData data, GameObject cardPrefab, GridSlot slot, bool startsActivated)
    {
        GameObject obj = PoolManager.Instance.Get(cardPrefab, slot.transform);
        CardView card = obj.GetComponent<CardView>();
        if (card == null) return null;

        card.Initialize(data, isEnemy: true, startsActivated: startsActivated);
        card.SetDraggable(false);
        card.ApplyGridLayout();

        slot.AssignCard(card);
        return card;
    }

    private void ClearGrid()
    {
        _slots.Clear();
        for (int i = _gridRoot.childCount - 1; i >= 0; i--)
            Destroy(_gridRoot.GetChild(i).gameObject);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static Vector2Int DirectionToDelta(CardDirection direction) => direction switch
    {
        CardDirection.Up => new Vector2Int(0, 1),
        CardDirection.UpRight => new Vector2Int(1, 1),
        CardDirection.Right => new Vector2Int(1, 0),
        CardDirection.DownRight => new Vector2Int(1, -1),
        CardDirection.Down => new Vector2Int(0, -1),
        CardDirection.DownLeft => new Vector2Int(-1, -1),
        CardDirection.Left => new Vector2Int(-1, 0),
        CardDirection.UpLeft => new Vector2Int(-1, 1),
        _ => Vector2Int.zero
    };

    protected override void Dispose()
    {
        base.Dispose();
    }
}
