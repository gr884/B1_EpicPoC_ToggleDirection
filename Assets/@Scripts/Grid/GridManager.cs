using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : SingletonBehaviour<GridManager>
{
    [Header("Refs")]
    [SerializeField] private RectTransform _gridRoot;
    [SerializeField] private GridSlot _slotPrefab;
    [SerializeField] private DirectionalImpactTrailEffectPlayer _directionalImpactEffectPlayer;

    [Header("Grid Settings")]
    [SerializeField] private int _rows = 4;
    [SerializeField] private int _columns = 4;

    public int Rows => _rows;
    public int Columns => _columns;
    public event Action OnGridBuilt;

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
        OnGridBuilt?.Invoke();
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
        if (slot == null || !slot.CanPlaceCardAt()) return null;
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
        return GetSlot(origin.Position + RelicShapeUtility.DirectionToDelta(direction));
    }

    public List<GridSlot> GetEmptySlots()
    {
        List<GridSlot> result = new();
        foreach (GridSlot slot in _slots.Values)
            if (slot.CanPlaceCardAt() && (CardManager.Instance == null || !CardManager.Instance.IsSlotReserved(slot)))
                result.Add(slot);
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
        ResolveDirectionalImpactEffectPlayer()?.Show(targets);
    }

    public void ClearPendingDirectionalImpact()
    {
        foreach (GridSlot slot in _pendingDirectionalImpactSlots)
            if (slot != null)
                slot.SetHighlight(false);
        _pendingDirectionalImpactSlots.Clear();
        _directionalImpactEffectPlayer?.Clear();
    }

    public void ShowPendingRelicPlacement(RelicData relic, GridSlot originSlot, int rotationSteps)
    {
        ClearPendingDirectionalImpact();
        if (relic == null || originSlot == null) return;

        bool canPlace = RelicManager.Instance != null
            && RelicManager.Instance.CanPlaceRelic(relic, originSlot.Position, rotationSteps);

        foreach (Vector2Int local in relic.GetOccupiedCells(rotationSteps))
        {
            GridSlot slot = GetSlot(originSlot.Position + local);
            if (slot == null) continue;
            slot.SetHighlight(canPlace);
            _pendingDirectionalImpactSlots.Add(slot);
        }

        if (!canPlace) return;

        List<GridSlot> targets = new();
        foreach (RelicDirectionRay ray in relic.GetDirectionRays(rotationSteps))
        {
            GridSlot current = GetSlot(originSlot.Position + ray.LocalCell);
            if (current == null) continue;

            for (int i = 0; i < ray.Range; i++)
            {
                GridSlot next = GetNeighbor(current, ray.Direction);
                if (next == null) break;
                if (!targets.Contains(next))
                    targets.Add(next);
                current = next;
            }
        }

        ResolveDirectionalImpactEffectPlayer()?.Show(targets);
    }

    // ── 내부 ───────────────────────────────────────────────

    private void CreatePendingPlacementPreviewEffect(GridSlot targetSlot)
    {
        if (targetSlot == null || _pendingDirectionalImpactSlots.Contains(targetSlot)) return;

        targetSlot.SetHighlight(true);
        _pendingDirectionalImpactSlots.Add(targetSlot);
    }

    private DirectionalImpactTrailEffectPlayer ResolveDirectionalImpactEffectPlayer()
    {
        if (_directionalImpactEffectPlayer == null)
            _directionalImpactEffectPlayer = FindFirstObjectByType<DirectionalImpactTrailEffectPlayer>(FindObjectsInactive.Include);
        _directionalImpactEffectPlayer?.AttachToGridRoot(_gridRoot);
        return _directionalImpactEffectPlayer;
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
        {
            Transform child = _gridRoot.GetChild(i);
            if (_directionalImpactEffectPlayer != null && child == _directionalImpactEffectPlayer.transform)
                continue;
            Destroy(child.gameObject);
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    protected override void Dispose()
    {
        base.Dispose();
    }
}
