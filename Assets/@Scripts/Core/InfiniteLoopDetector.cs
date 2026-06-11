using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 체인 실행 전, 그리드 상태를 시뮬레이션해서 무한 루프가 발생할지 사전 판정합니다.
/// 사이드이펙트 없는 순수 탐지 로직입니다.
/// </summary>
public class InfiniteLoopDetector
{
    /// <summary>
    /// root에서 체인을 시작했을 때 무한 루프가 발생하는지 시뮬레이션합니다.
    /// </summary>
    /// <param name="root">체인 시작 카드</param>
    /// <param name="maxSteps">안전 한도 — 초과 시 무한 루프로 간주</param>
    public bool WouldCreateInfiniteLoop(CardView root, int maxSteps)
    {
        if (root == null || root.CurrentSlot == null || GridManager.Instance == null)
            return false;

        List<CardView> placedCards = GetPlacedCards();
        if (!placedCards.Contains(root))
            placedCards.Add(root);
        SortCardsByGridPosition(placedCards);

        Dictionary<CardView, bool> simulatedStates = new();
        foreach (CardView card in placedCards)
            if (card != null)
                simulatedStates[card] = card.IsActivated;

        List<CardView> currentWave = new() { root };
        HashSet<string> visitedStates = new();
        int step = 0;

        while (currentWave.Count > 0)
        {
            string stateKey = BuildLoopStateKey(currentWave, placedCards, simulatedStates);
            if (!visitedStates.Add(stateKey))
            {
                Debug.Log("[InfiniteLoopDetector] 사전 시뮬레이션에서 무한 루프 감지.");
                return true;
            }

            List<CardView> emitters = new();
            foreach (CardView current in currentWave)
            {
                if (current == null || current.CurrentSlot == null) continue;
                if (!simulatedStates.TryGetValue(current, out bool currentState)) continue;

                bool nextState = !currentState;
                simulatedStates[current] = nextState;

                step++;
                if (step > maxSteps)
                {
                    Debug.LogWarning("[InfiniteLoopDetector] 사전 시뮬레이션 안전 한도 초과 — 무한 루프로 처리.");
                    return true;
                }

                if (nextState)
                    emitters.Add(current);
            }

            HashSet<CardView> nextWaveSet = new();
            foreach (CardView emitter in emitters)
            {
                if (emitter == null || emitter.Data == null) continue;

                foreach (GridSlot targetSlot in CardTargetResolver.ResolveToggleSlots(emitter, GridManager.Instance))
                {
                    CardView target = targetSlot.OccupiedCard;
                    if (target != null && simulatedStates.ContainsKey(target))
                        nextWaveSet.Add(target);
                }
            }

            currentWave = new List<CardView>(nextWaveSet);
        }

        return false;
    }

    private static List<CardView> GetPlacedCards()
    {
        List<CardView> result = new();
        HashSet<CardView> uniqueCards = new();
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null && uniqueCards.Add(slot.OccupiedCard))
                result.Add(slot.OccupiedCard);
        return result;
    }

    private static void SortCardsByGridPosition(List<CardView> cards)
    {
        cards.Sort((a, b) =>
        {
            Vector2Int aPosition = a != null && a.CurrentSlot != null ? a.CurrentSlot.Position : Vector2Int.zero;
            Vector2Int bPosition = b != null && b.CurrentSlot != null ? b.CurrentSlot.Position : Vector2Int.zero;
            int yCompare = aPosition.y.CompareTo(bPosition.y);
            return yCompare != 0 ? yCompare : aPosition.x.CompareTo(bPosition.x);
        });
    }

    private static string BuildLoopStateKey(
        List<CardView> currentWave,
        List<CardView> placedCards,
        Dictionary<CardView, bool> simulatedStates)
    {
        HashSet<CardView> waveSet = new(currentWave);
        StringBuilder builder = new();

        foreach (CardView card in placedCards)
            builder.Append(waveSet.Contains(card) ? '1' : '0');

        builder.Append('|');

        foreach (CardView card in placedCards)
            builder.Append(simulatedStates.TryGetValue(card, out bool active) && active ? '1' : '0');

        return builder.ToString();
    }
}
