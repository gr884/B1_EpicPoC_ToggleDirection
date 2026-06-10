using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicManager : SingletonBehaviour<RelicManager>
{
    [Header("Refs")]
    [SerializeField] private RelicView _relicViewPrefab;

    private readonly List<RelicInstance> _placedRelics = new();
    private readonly Dictionary<RelicInstance, RelicView> _views = new();

    public IReadOnlyList<RelicInstance> PlacedRelics => _placedRelics;

    public void Init()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.OnGridBuilt += RebindPlacedRelics;
            RebindPlacedRelics();
        }

        Debug.Log("[RelicManager] Init");
    }

    public bool CanPlaceRelic(RelicData data, Vector2Int origin, int rotationSteps)
    {
        if (data == null || GridManager.Instance == null) return false;

        foreach (Vector2Int local in data.GetOccupiedCells(rotationSteps))
        {
            GridSlot slot = GridManager.Instance.GetSlot(origin + local);
            if (slot == null || !slot.CanPlaceRelicAt())
                return false;
        }

        return true;
    }

    public RelicView PlaceRelic(RelicData data, Vector2Int origin, int rotationSteps)
    {
        if (!CanPlaceRelic(data, origin, rotationSteps))
            return null;

        RelicInstance instance = new(data);
        instance.SetPlacement(origin, rotationSteps);
        _placedRelics.Add(instance);

        RelicView view = BindRelic(instance);
        if (view != null)
            StartCoroutine(ApplyRelicEffects(view, RelicEffectTiming.OnPlaced));

        Debug.Log($"[RelicManager] 유물 배치 — {data.displayName} @ {origin}");
        return view;
    }

    public IEnumerator ApplyRelicEffects(RelicView relic, RelicEffectTiming timing)
    {
        if (relic?.Data?.effects == null) yield break;

        RelicEffectContext context = new(relic, timing);
        foreach (RelicEffectSO effect in relic.Data.effects)
        {
            if (effect == null || !effect.CanApply(context)) continue;
            effect.Apply(context);
            yield return null;
        }
    }

    public IEnumerable<RelicView> GetPlacedRelicViews()
    {
        foreach (RelicView view in _views.Values)
            if (view != null)
                yield return view;
    }

    public void RebindPlacedRelics()
    {
        ClearRelicOccupancy();

        _views.Clear();
        foreach (RelicInstance instance in _placedRelics)
            BindRelic(instance);
    }

    private RelicView BindRelic(RelicInstance instance)
    {
        if (instance?.SourceData == null || GridManager.Instance == null)
            return null;

        GridSlot primarySlot = GridManager.Instance.GetSlot(instance.GridOrigin);
        if (primarySlot == null || _relicViewPrefab == null)
            return null;

        foreach (Vector2Int local in instance.SourceData.GetOccupiedCells(instance.RotationSteps))
        {
            GridSlot slot = GridManager.Instance.GetSlot(instance.GridOrigin + local);
            if (slot != null)
                slot.AssignRelic(null);
        }

        RelicView view = Instantiate(_relicViewPrefab, primarySlot.transform);
        view.Initialize(instance, primarySlot);
        _views[instance] = view;

        foreach (Vector2Int local in instance.SourceData.GetOccupiedCells(instance.RotationSteps))
        {
            GridSlot slot = GridManager.Instance.GetSlot(instance.GridOrigin + local);
            if (slot != null)
                slot.AssignRelic(view);
        }

        return view;
    }

    private void ClearRelicOccupancy()
    {
        if (GridManager.Instance == null) return;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            slot.ClearRelic();
    }

    protected override void Dispose()
    {
        if (GridManager.Instance != null)
            GridManager.Instance.OnGridBuilt -= RebindPlacedRelics;

        _views.Clear();
        base.Dispose();
    }
}
