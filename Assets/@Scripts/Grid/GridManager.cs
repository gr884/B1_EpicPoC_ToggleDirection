using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : SingletonBehaviour<GridManager>
{
    [Header("Refs")]
    [SerializeField] private RectTransform _gridRoot;
    [SerializeField] private GridSlot _slotPrefab;

    [Header("Grid Settings")]
    [SerializeField] private int _rows = 5;
    [SerializeField] private int _columns = 5;

    public int Rows => _rows;
    public int Columns => _columns;

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

    // ── 리셋 (카드만 제거, 슬롯 유지) ────────────────────────

    public void ResetCards()
    {
        foreach (GridSlot slot in _slots.Values)
            if (!slot.IsEmpty)
                slot.ClearCard();

        Debug.Log("[GridManager] 그리드 카드 리셋");
    }

    // ── 적 카드 배치 ───────────────────────────────────────

    public List<GridSlot> PlaceEnemyCards(EnemyDataSO enemyData, GameObject cardPrefab)
    {
        if (enemyData == null || cardPrefab == null) return new();

        List<GridSlot> placed = enemyData.isRandom
            ? PlaceEnemyCardsRandom(enemyData, cardPrefab)
            : PlaceEnemyCardsFixed(enemyData, cardPrefab);

        Debug.Log($"[GridManager] 적 카드 {placed.Count}개 배치");
        return placed;
    }

    private List<GridSlot> PlaceEnemyCardsRandom(EnemyDataSO enemyData, GameObject cardPrefab)
    {
        List<GridSlot> placed = new();
        if (enemyData.randomCardPool == null || enemyData.randomCardPool.Count == 0) return placed;

        List<GridSlot> emptySlots = GetEmptySlots();
        Shuffle(emptySlots);

        int count = Mathf.Min(enemyData.randomCardCount, emptySlots.Count);
        for (int i = 0; i < count; i++)
        {
            CardData data = enemyData.randomCardPool[Random.Range(0, enemyData.randomCardPool.Count)];
            CardView card = SpawnEnemyCard(data, cardPrefab, emptySlots[i]);
            if (card != null) placed.Add(emptySlots[i]);
        }

        return placed;
    }

    private List<GridSlot> PlaceEnemyCardsFixed(EnemyDataSO enemyData, GameObject cardPrefab)
    {
        List<GridSlot> placed = new();
        if (enemyData.fixedPlacements == null) return placed;

        foreach (EnemyCardPlacement placement in enemyData.fixedPlacements)
        {
            if (placement?.cardData == null) continue;

            GridSlot slot = GetSlot(placement.position);
            if (slot == null || !slot.IsEmpty) continue;

            CardView card = SpawnEnemyCard(placement.cardData, cardPrefab, slot);
            if (card != null) placed.Add(slot);
        }

        return placed;
    }

    private CardView SpawnEnemyCard(CardData data, GameObject cardPrefab, GridSlot slot, bool startsActivated = true)
    {
        GameObject obj = PoolManager.Instance.Get(cardPrefab, Vector3.zero, slot.transform);
        CardView card = obj.GetComponent<CardView>();
        if (card == null) return null;

        card.Initialize(data, isEnemy: true, startsActivated: startsActivated);
        card.SetDraggable(false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        slot.AssignCard(card);
        return card;
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

    // ── 내부 ───────────────────────────────────────────────

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
            int j = Random.Range(0, i + 1);
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