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
            // 이전 파형 하이라이트 초기화
            foreach (GridSlot slot in _previewHighlightedSlots)
                if (slot != null) slot.SetPreviewHighlight(false);
            _previewHighlightedSlots.Clear();

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

            // 이번 파형 활성화 슬롯 하이라이트
            foreach (GridSlot slot in emitters)
            {
                slot.SetPreviewHighlight(true);
                _previewHighlightedSlots.Add(slot);
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

    private void SimulateChain(GridSlot origin, Dictionary<GridSlot, bool> simulatedState)
    {
        if (origin == null || origin.OccupiedItem == null) return;

        List<GridSlot> currentWave = new() { origin };
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
                if (step > 2048) return;

                if (nextState) emitters.Add(current);
            }

            HashSet<GridSlot> nextWaveSet = new();
            foreach (GridSlot emitter in emitters)
            {
                ItemData data = emitter.OccupiedItem.Data;
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
    }

    protected override void Dispose()
    {
        OnExecutionStarted = null;
        OnExecutionFinished = null;
        base.Dispose();
    }
}