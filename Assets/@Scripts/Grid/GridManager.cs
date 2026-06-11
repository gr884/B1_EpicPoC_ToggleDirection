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
    public RectTransform GridRoot => _gridRoot;

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
        HashSet<CardView> cards = new();
        foreach (GridSlot slot in _slots.Values)
            if (slot.OccupiedCard != null)
                cards.Add(slot.OccupiedCard);
        foreach (CardView card in cards)
            RemoveCard(card);

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
        Shuffle(emptySlots);
        foreach (GridSlot slot in emptySlots)
            if (CanPlaceCard(data, slot))
                return SpawnEnemyCard(data, cardPrefab, slot, startsActivated);
        return null;
    }

    public int GetEnemyCardCount()
    {
        HashSet<CardView> enemies = new();
        foreach (GridSlot slot in _slots.Values)
            if (!slot.IsEmpty && slot.OccupiedCard.IsEnemy)
                enemies.Add(slot.OccupiedCard);
        return enemies.Count;
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
            if (slot.IsEmpty && (CardManager.Instance == null || !CardManager.Instance.IsSlotReserved(slot)))
                result.Add(slot);
        return result;
    }

    public bool TryGetPlacementSlots(CardData data, GridSlot anchor, out List<GridSlot> slots)
    {
        return CardPlacementResolver.TryResolveSlots(data, anchor, this, out slots);
    }

    public bool CanPlaceCard(CardData data, GridSlot anchor, CardView ignoredCard = null)
    {
        if (!TryGetPlacementSlots(data, anchor, out List<GridSlot> slots))
            return false;

        foreach (GridSlot slot in slots)
            if (slot.OccupiedCard != null && slot.OccupiedCard != ignoredCard)
                return false;
        return true;
    }

    public bool PlaceCard(CardView card, GridSlot anchor)
    {
        if (card?.Data == null || !CanPlaceCard(card.Data, anchor, card))
            return false;
        if (!TryGetPlacementSlots(card.Data, anchor, out List<GridSlot> slots))
            return false;

        RemoveCard(card);
        foreach (GridSlot slot in slots)
            slot.SetOccupant(card);
        card.SetPlaced(anchor, slots);
        LayoutPlacedCard(card);
        return true;
    }

    public void RemoveCard(CardView card)
    {
        if (card == null) return;

        foreach (GridSlot slot in _slots.Values)
            if (slot.OccupiedCard == card)
                slot.SetOccupant(null);
        card.SetPlaced(null, null);
    }

    public void LayoutPlacedCard(CardView card)
    {
        if (card == null || card.CurrentSlot == null) return;

        RectTransform cardRect = card.transform as RectTransform;
        if (cardRect == null) return;

        // The dragged card comes from the root canvas. Do not preserve its world Z
        // when moving it back under the grid canvas.
        cardRect.SetParent(_gridRoot, false);
        LayoutElement layoutElement = card.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = card.gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
        Vector3[] corners = new Vector3[4];
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        foreach (GridSlot slot in card.OccupiedSlots)
        {
            RectTransform slotRect = slot != null ? slot.transform as RectTransform : null;
            if (slotRect == null) continue;
            slotRect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 local = _gridRoot.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        }

        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 center = (min + max) * 0.5f;
        cardRect.anchoredPosition3D = new Vector3(center.x, center.y, 0f);
        cardRect.sizeDelta = max - min;
        cardRect.localPosition = new Vector3(cardRect.localPosition.x, cardRect.localPosition.y, 0f);
        cardRect.localRotation = Quaternion.identity;
        cardRect.localScale = Vector3.one;
        card.RefreshPieceShape(true);
    }

    public List<GridSlot> GetDirectionalImpactSlots(CardView sourceCard, GridSlot attachSlot)
    {
        if (sourceCard?.Data == null || attachSlot == null || sourceCard.Data.isRecaller)
            return new List<GridSlot>();
        return CardTargetResolver.ResolveToggleSlots(sourceCard.Data, attachSlot, this);
    }

    public void ShowPendingDirectionalImpact(CardView sourceCard, GridSlot attachSlot)
    {
        ClearPendingDirectionalImpact();
        if (sourceCard == null || attachSlot == null) return;
        if (CardManager.Instance == null || !CardManager.Instance.CanPreviewPlaceCard(sourceCard, attachSlot)) return;

        CreatePendingPlacementPreviewEffect(attachSlot);
        if (TryGetPlacementSlots(sourceCard.Data, attachSlot, out List<GridSlot> placementSlots))
            foreach (GridSlot slot in placementSlots)
                CreatePendingPlacementPreviewEffect(slot);

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
        GameObject obj = PoolManager.Instance.Get(cardPrefab, _gridRoot);
        CardView card = obj.GetComponent<CardView>();
        if (card == null) return null;

        card.Initialize(data, isEnemy: true, startsActivated: startsActivated);
        card.SetDraggable(false);
        if (!PlaceCard(card, slot))
        {
            PoolManager.Instance.Return(obj);
            return null;
        }
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
