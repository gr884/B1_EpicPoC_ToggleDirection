using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainExecutor : SingletonBehaviour<ChainExecutor>
{
    [Header("Chain Settings")]
    [SerializeField] private int _maxActivationSteps = 2048;
    [SerializeField] private float _itemFeedbackDuration = 0.22f;

    // 이벤트
    public event Action OnExecutionStarted;
    public event Action OnExecutionFinished;

    private readonly List<GridSlot> _previewHighlightedSlots = new();

    public void Init()
    {
        Debug.Log("[ChainExecutor] Init");
    }

    // ── 실행 ───────────────────────────────────────────────

    public void RequestExecute()
    {
        if (!GameManager.Instance.IsPlaying) return;
        StartCoroutine(ExecuteAllChains());
    }

    private IEnumerator ExecuteAllChains()
    {
        OnExecutionStarted?.Invoke();

        // 실행 중 드래그 막기
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.OccupiedItem != null)
                slot.OccupiedItem.SetDraggable(false);
        }

        // 모든 아이템 비활성화 초기화
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.OccupiedItem != null)
                slot.OccupiedItem.SetActivated(false);
        }

        // 모든 아이템 동시 시발점으로 체인 실행
        List<ItemView> allItems = new();
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.OccupiedItem != null)
                allItems.Add(slot.OccupiedItem);
        }

        yield return ActivateChainFrom(allItems);

        // 실행 완료 후 드래그 복구
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.OccupiedItem != null)
                slot.OccupiedItem.SetDraggable(true);
        }

        OnExecutionFinished?.Invoke();
    }

    private IEnumerator ActivateChainFrom(List<ItemView> rootItems)
    {
        List<ItemView> currentWave = rootItems;
        int step = 0;

        while (currentWave.Count > 0)
        {
            List<ItemView> emitters = new();

            foreach (ItemView current in currentWave)
            {
                if (current == null || current.CurrentSlot == null) continue;

                bool nextState = !current.IsActivated;
                current.SetActivated(nextState);
                current.CurrentSlot.SetPreviewHighlight(nextState);

                step++;
                if (step > _maxActivationSteps)
                {
                    Debug.LogWarning("[ChainExecutor] 안전 한도 초과로 체인 중단.");
                    yield break;
                }

                if (nextState) emitters.Add(current);
            }

            foreach (ItemView current in currentWave)
            {
                if (current != null)
                    StartCoroutine(current.PlayActivationFeedback(_itemFeedbackDuration));
            }

            yield return new WaitForSeconds(_itemFeedbackDuration);

            HashSet<ItemView> nextWaveSet = new();
            foreach (ItemView emitter in emitters)
            {
                if (emitter.Data == null) continue;

                foreach (ItemDirection dir in emitter.Data.GetAllDirections())
                {
                    GridSlot neighbor = GridManager.Instance.GetNeighbor(emitter.CurrentSlot, dir);
                    if (neighbor != null && neighbor.OccupiedItem != null)
                        nextWaveSet.Add(neighbor.OccupiedItem);
                }
            }

            currentWave = new List<ItemView>(nextWaveSet);
        }
    }

    private Coroutine _previewCoroutine;

    // ── 미리보기 ───────────────────────────────────────────

    public void ShowPreview(GridSlot originSlot)
    {
        ClearPreview();

        if (originSlot == null || originSlot.OccupiedItem == null) return;
        if (!GameManager.Instance.IsPlaying) return;

        if (_previewCoroutine != null)
            StopCoroutine(_previewCoroutine);

        _previewCoroutine = StartCoroutine(ShowPreviewRoutine(originSlot));
    }

    private IEnumerator ShowPreviewRoutine(GridSlot originSlot)
    {
        List<GridSlot> currentWave = new() { originSlot };
        Dictionary<GridSlot, bool> simulatedState = new();
        int step = 0;

        while (currentWave.Count > 0)
        {
            List<GridSlot> emitters = new();

            foreach (GridSlot current in currentWave)
            {
                if (current == null || current.OccupiedItem == null) continue;

                bool currentState = simulatedState.TryGetValue(current, out bool s)
                    ? s : current.OccupiedItem.IsActivated;

                bool nextState = !currentState;
                simulatedState[current] = nextState;

                step++;
                if (step > 2048) yield break;

                if (nextState) emitters.Add(current);
            }

            // 이번 파형 활성화된 슬롯 하이라이트 (누적)
            foreach (GridSlot slot in emitters)
            {
                if (!_previewHighlightedSlots.Contains(slot))
                    _previewHighlightedSlots.Add(slot);
                slot.SetPreviewHighlight(true);
            }

            // 이번 파형에서 꺼진 슬롯은 하이라이트 제거
            foreach (GridSlot slot in currentWave)
            {
                if (!emitters.Contains(slot))
                {
                    slot.SetPreviewHighlight(false);
                    _previewHighlightedSlots.Remove(slot);
                }
            }

            yield return new WaitForSeconds(0.2f);

            HashSet<GridSlot> nextWaveSet = new();
            foreach (GridSlot emitter in emitters)
            {
                ItemData data = emitter.OccupiedItem?.Data;
                if (data == null) continue;

                foreach (ItemDirection dir in data.GetAllDirections())
                {
                    GridSlot neighbor = GridManager.Instance.GetNeighbor(emitter, dir);
                    if (neighbor != null && neighbor.OccupiedItem != null)
                        nextWaveSet.Add(neighbor);
                }
            }

            currentWave = new List<GridSlot>(nextWaveSet);
        }

        _previewCoroutine = null;
    }

    public void ClearPreview()
    {
        if (_previewCoroutine != null)
        {
            StopCoroutine(_previewCoroutine);
            _previewCoroutine = null;
        }

        foreach (GridSlot slot in _previewHighlightedSlots)
        {
            if (slot != null)
                slot.SetPreviewHighlight(false);
        }
        _previewHighlightedSlots.Clear();
    }

    // ── 효과 집계 ──────────────────────────────────────────

    public Dictionary<EffectType, float> CalculateEffects(IReadOnlyDictionary<Vector2Int, ItemView> placedItems)
    {
        var result = new Dictionary<EffectType, float>();

        if (placedItems == null || placedItems.Count == 0)
            return result;

        foreach (var kv in placedItems)
        {
            ItemView item = kv.Value;
            if (item?.Data == null || item.Data.effects == null) continue;

            foreach (ItemEffect effect in item.Data.effects)
            {
                int count = CountByScope(effect.scope, kv.Key, placedItems);
                if (count < effect.threshold) continue;

                if (!result.ContainsKey(effect.effectType))
                    result[effect.effectType] = 0f;
                result[effect.effectType] += effect.value;
            }
        }

        return result;
    }

    private static HashSet<Vector2Int> SimulateChainFromSelf(
        Vector2Int origin,
        IReadOnlyDictionary<Vector2Int, ItemView> placedItems)
    {
        var stateMap = new Dictionary<Vector2Int, bool>();
        var onPositions = new HashSet<Vector2Int>();
        List<Vector2Int> currentWave = new() { origin };
        int step = 0;

        while (currentWave.Count > 0)
        {
            List<Vector2Int> emitters = new();

            foreach (Vector2Int pos in currentWave)
            {
                if (!placedItems.TryGetValue(pos, out ItemView item) || item == null) continue;

                bool next = !stateMap.TryGetValue(pos, out bool s) ? true : !s;
                stateMap[pos] = next;

                step++;
                if (step > 2048) return onPositions;

                if (next) { emitters.Add(pos); onPositions.Add(pos); }
            }

            HashSet<Vector2Int> nextWaveSet = new();
            foreach (Vector2Int emitterPos in emitters)
            {
                if (!placedItems.TryGetValue(emitterPos, out ItemView emitter) || emitter?.Data == null) continue;

                foreach (ItemDirection dir in emitter.Data.GetAllDirections())
                {
                    GridSlot neighbor = GridManager.Instance.GetNeighbor(
                        GridManager.Instance.GetSlot(emitterPos), dir);

                    if (neighbor != null && placedItems.ContainsKey(neighbor.Position)
                        && !stateMap.ContainsKey(neighbor.Position))
                        nextWaveSet.Add(neighbor.Position);
                }
            }

            currentWave = new List<Vector2Int>(nextWaveSet);
        }

        return onPositions;
    }

    private static int CountByScope(
        CountScope scope,
        Vector2Int pos,
        IReadOnlyDictionary<Vector2Int, ItemView> placedItems)
    {
        HashSet<Vector2Int> onFromSelf = SimulateChainFromSelf(pos, placedItems);

        switch (scope)
        {
            case CountScope.Row:
                int rowCount = 0;
                foreach (Vector2Int p in onFromSelf)
                    if (p.y == pos.y) rowCount++;
                return rowCount;

            case CountScope.Column:
                int colCount = 0;
                foreach (Vector2Int p in onFromSelf)
                    if (p.x == pos.x) colCount++;
                return colCount;

            case CountScope.Total:
                return onFromSelf.Count;

            default:
                return 0;
        }
    }

    protected override void Dispose()
    {
        OnExecutionStarted = null;
        OnExecutionFinished = null;
        base.Dispose();
    }
}