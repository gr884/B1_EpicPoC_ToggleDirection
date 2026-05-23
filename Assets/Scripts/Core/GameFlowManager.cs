using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowManager : SingletonBehaviour<GameFlowManager>
{
    [Header("Chain Settings")]
    [SerializeField] private int _maxActivationSteps = 2048;
    [SerializeField] private float _chainStepDelay = 0.4f;
    [SerializeField] private float _cardFeedbackDuration = 0.22f;

    // 체인 시작 시 어떤 카드부터 시작할지 보관
    private Card _pendingChainRoot;

    public void Init()
    {
        GameManager.Instance.OnChainStateChanged += OnChainStateChanged;
        GameManager.Instance.OnUndoRequested += OnUndoRequested;
        GameManager.Instance.OnRestartRequested += OnRestartRequested;

        LoadPlayground();

        Debug.Log("[GameFlowManager] Init");
    }

    // ── GameManager 이벤트 수신 ────────────────────────────

    private void OnChainStateChanged(bool isStarting)
    {
        if (isStarting && _pendingChainRoot != null)
        {
            StartCoroutine(ResolveChain(_pendingChainRoot));
            _pendingChainRoot = null;
        }
    }

    private void OnUndoRequested()
    {
        StopAllCoroutines();
        GameManager.Instance.PopAndRestoreSnapshot();
        GameManager.Instance.SetResolvingChain(false);
    }

    private void OnRestartRequested()
    {
        StopAllCoroutines();
        GameManager.Instance.SetResolvingChain(false);
        GameManager.Instance.ClearUndoStack();
        LoadPlayground();
    }

    // ── 외부 호출 ──────────────────────────────────────────

    /// <summary>CardDragHandler에서 호출. 카드 배치 + 체인 예약.</summary>
    public bool TryPlaceCard(Card card, BoardSlot targetSlot)
    {
        _pendingChainRoot = card;
        bool placed = GameManager.Instance.TryPlaceCard(card, targetSlot);
        if (!placed) _pendingChainRoot = null;
        return placed;
    }

    public bool TryRemoveCard(BoardSlot slot)
    {
        return GameManager.Instance.TryRemoveCard(slot);
    }

    // ── 내부 ───────────────────────────────────────────────

    private void LoadPlayground()
    {
        BoardManager.Instance.BuildBoard();
        DeckManager.Instance.BuildInfiniteDeck();
        Debug.Log("[GameFlowManager] 플레이그라운드 로드 완료");
    }

    private IEnumerator ResolveChain(Card rootCard)
    {
        yield return ActivateChainFrom(rootCard);
        GameManager.Instance.SetResolvingChain(false);
    }

    private IEnumerator ActivateChainFrom(Card rootCard)
    {
        if (rootCard == null) yield break;

        List<Card> currentWave = new() { rootCard };
        int step = 0;

        while (currentWave.Count > 0)
        {
            List<Card> emitters = new();

            foreach (Card current in currentWave)
            {
                if (current == null || current.CurrentSlot == null) continue;

                bool nextState = !current.IsActivated;
                current.SetActivated(nextState);
                current.CurrentSlot.PulseHighlight(_cardFeedbackDuration);

                step++;
                if (step > _maxActivationSteps)
                {
                    Debug.LogWarning("[GameFlowManager] 안전 한도 초과로 체인 중단.");
                    yield break;
                }

                if (nextState) emitters.Add(current);
            }

            foreach (Card current in currentWave)
            {
                if (current != null)
                    StartCoroutine(current.PlayActivationFeedback(_cardFeedbackDuration));
            }

            yield return new WaitForSeconds(_cardFeedbackDuration);

            HashSet<Card> nextWaveSet = new();
            foreach (Card emitter in emitters)
            {
                if (emitter.Data == null) continue;

                foreach (AbilityDirection dir in emitter.Data.GetAllDirections())
                {
                    BoardSlot neighbor = BoardManager.Instance.GetNeighbor(emitter.CurrentSlot, dir);
                    if (neighbor != null && neighbor.OccupiedCard != null)
                        nextWaveSet.Add(neighbor.OccupiedCard);
                }
            }

            currentWave = new List<Card>(nextWaveSet);

            if (currentWave.Count > 0 && _chainStepDelay > 0f)
                yield return new WaitForSeconds(_chainStepDelay);
        }
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnChainStateChanged -= OnChainStateChanged;
            GameManager.Instance.OnUndoRequested -= OnUndoRequested;
            GameManager.Instance.OnRestartRequested -= OnRestartRequested;
        }

        base.Dispose();
    }
}