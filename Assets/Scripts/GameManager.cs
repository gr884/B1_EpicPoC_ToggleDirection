using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private UIManager uiManager;

    [Header("Levels")]
    [SerializeField] private List<LevelData> levels = new();
    [SerializeField] private int startLevelIndex;

    [Header("Safety")]
    [SerializeField] private int maxActivationSteps = 2048;
    [SerializeField] private float chainStepDelay = 0.5f;
    [SerializeField] private float cardFeedbackDuration = 0.22f;

    private int currentLevelIndex;
    private bool levelEnded;
    private bool isResolvingChain;
    private readonly Stack<GameSnapshot> undoStack = new();
    private readonly List<BoardSlot> previewSlots = new();

    private void Start()
    {
        ResolveRefs();
        CleanupSceneArtifacts();

        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("GameManager: Level list is empty.");
            return;
        }

        currentLevelIndex = Mathf.Clamp(startLevelIndex, 0, Mathf.Max(0, levels.Count - 1));

        if (uiManager != null)
        {
            uiManager.Initialize(this);
        }

        LoadLevel(currentLevelIndex);
    }

    public bool TryPlaceCardFromHand(Card card, BoardSlot targetSlot)
    {
        if (levelEnded || isResolvingChain || card == null || targetSlot == null || !targetSlot.IsEmpty())
        {
            return false;
        }

        ClearPlacementPreview();

        undoStack.Push(CaptureSnapshot());

        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        deckManager.RemoveFromHand(card);
        StartCoroutine(ResolveChainAndEvaluate(card));

        return true;
    }

    public void RestartCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    public void UndoLastMove()
    {
        if (undoStack.Count == 0)
        {
            return;
        }

        StopAllCoroutines();
        isResolvingChain = false;
        levelEnded = false;

        RestoreSnapshot(undoStack.Pop());
        ClearPlacementPreview();
    }

    public void ShowPlacementPreview(Card card, BoardSlot targetSlot)
    {
        ClearPlacementPreview();

        if (card == null || targetSlot == null || card.Data == null || boardManager == null)
        {
            return;
        }

        // Center slot preview: yellow for placeable, red for blocked.
        bool centerPlaceable = targetSlot.IsEmpty();
        targetSlot.SetPreview(true, true, centerPlaceable);
        previewSlots.Add(targetSlot);

        HashSet<BoardSlot> dedupe = new();
        foreach (AbilityDirection dir in card.Data.GetAllDirections())
        {
            BoardSlot affected = boardManager.GetNeighbor(targetSlot, dir);
            if (affected == null || !dedupe.Add(affected))
            {
                continue;
            }

            affected.SetPreview(true, false, true);
            previewSlots.Add(affected);
        }
    }

    public void ClearPlacementPreview()
    {
        for (int i = 0; i < previewSlots.Count; i++)
        {
            if (previewSlots[i] != null)
            {
                previewSlots[i].SetPreview(false, false, true);
            }
        }

        previewSlots.Clear();
    }

    public void LoadNextLevel()
    {
        int next = currentLevelIndex + 1;
        if (next >= levels.Count)
        {
            return;
        }

        currentLevelIndex = next;
        LoadLevel(currentLevelIndex);
    }

    public bool IsLastLevel()
    {
        return levels != null && levels.Count > 0 && currentLevelIndex >= levels.Count - 1;
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResolveRefs()
    {
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }

        if (deckManager == null)
        {
            deckManager = FindFirstObjectByType<DeckManager>();
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    private void CleanupSceneArtifacts()
    {
        Card[] existingCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (Card card in existingCards)
        {
            Destroy(card.gameObject);
        }

        BoardSlot[] existingSlots = FindObjectsByType<BoardSlot>(FindObjectsSortMode.None);
        foreach (BoardSlot slot in existingSlots)
        {
            Destroy(slot.gameObject);
        }
    }

    private void LoadLevel(int levelIndex)
    {
        if (levels == null || levels.Count == 0)
        {
            Debug.LogError("GameManager: Level list is empty.");
            return;
        }

        if (boardManager == null || deckManager == null)
        {
            Debug.LogError("GameManager: Missing BoardManager or DeckManager.");
            return;
        }

        levelEnded = false;
        isResolvingChain = false;
        undoStack.Clear();
        ClearPlacementPreview();

        LevelData levelData = levels[levelIndex];
        boardManager.BuildBoard(levelData, this);
        deckManager.BuildHand(levelData.handCards);

        SpawnPrePlacedCards(levelData);

        if (uiManager != null)
        {
            uiManager.HideResult();
        }
    }

    private GameSnapshot CaptureSnapshot()
    {
        GameSnapshot snapshot = new()
        {
            handCards = deckManager.CaptureHandState(),
            placedCards = new List<PlacedCardRuntime>()
        };

        foreach (BoardSlot slot in boardManager.Slots.Values)
        {
            if (slot == null || slot.OccupiedCard == null || slot.OccupiedCard.Data == null)
            {
                continue;
            }

            snapshot.placedCards.Add(new PlacedCardRuntime
            {
                card = slot.OccupiedCard.Data,
                position = slot.Position,
                isActivated = slot.OccupiedCard.IsActivated,
                shieldCharges = slot.OccupiedCard.ShieldCharges
            });
        }

        return snapshot;
    }

    private void RestoreSnapshot(GameSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        LevelData levelData = levels[currentLevelIndex];
        boardManager.BuildBoard(levelData, this);
        deckManager.BuildHand(snapshot.handCards);

        for (int i = 0; i < snapshot.placedCards.Count; i++)
        {
            PlacedCardRuntime placed = snapshot.placedCards[i];
            if (placed == null || placed.card == null)
            {
                continue;
            }

            BoardSlot slot = boardManager.GetSlot(placed.position);
            if (slot == null || !slot.IsEmpty())
            {
                continue;
            }

            Card boardCard = deckManager.SpawnBoardCard(placed.card, slot.transform, placed.isActivated);
            if (boardCard == null)
            {
                continue;
            }

            boardCard.SetShieldCharges(placed.shieldCharges);
            slot.AssignCard(boardCard);
        }

        if (uiManager != null)
        {
            uiManager.HideResult();
        }
    }

    private void SpawnPrePlacedCards(LevelData levelData)
    {
        if (levelData == null || levelData.prePlacedCards == null)
        {
            return;
        }

        foreach (PlacedCardSeed seed in levelData.prePlacedCards)
        {
            if (seed == null || seed.card == null)
            {
                continue;
            }

            BoardSlot slot = boardManager.GetSlot(seed.position);
            if (slot == null || !slot.IsEmpty())
            {
                Debug.LogWarning($"GameManager: Invalid pre-placed slot {seed.position}.");
                continue;
            }

            Card boardCard = deckManager.SpawnBoardCard(seed.card, slot.transform, seed.startsActivated);
            if (boardCard == null)
            {
                continue;
            }

            slot.AssignCard(boardCard);
        }
    }

    private System.Collections.IEnumerator ResolveChainAndEvaluate(Card rootCard)
    {
        isResolvingChain = true;
        yield return ActivateChainFrom(rootCard);
        EvaluateEndState();
        isResolvingChain = false;
    }

    private System.Collections.IEnumerator ActivateChainFrom(Card rootCard)
    {
        if (rootCard == null)
        {
            yield break;
        }

        List<ActivationSignal> currentWave = new()
        {
            new ActivationSignal { target = rootCard, hasBufferSource = false }
        };

        int step = 0;

        while (currentWave.Count > 0)
        {
            List<Card> emitters = new();

            for (int i = 0; i < currentWave.Count; i++)
            {
                ActivationSignal signal = currentWave[i];
                Card current = signal.target;
                if (current == null || current.CurrentSlot == null)
                {
                    continue;
                }

                if (current.TryConsumeShieldOnExternalToggle())
                {
                    current.CurrentSlot.PulseHighlight(cardFeedbackDuration);
                    continue;
                }

                bool nextState = !current.IsActivated;
                current.SetActivated(nextState);
                current.CurrentSlot.PulseHighlight(cardFeedbackDuration);

                if (signal.hasBufferSource)
                {
                    current.GrantShieldOneCharge();
                }

                step++;
                if (step > maxActivationSteps)
                {
                    Debug.LogWarning("GameManager: Activation chain stopped by safety limit.");
                    yield break;
                }

                if (nextState)
                {
                    emitters.Add(current);
                }
            }

            for (int i = 0; i < currentWave.Count; i++)
            {
                Card current = currentWave[i].target;
                if (current == null || current.CurrentSlot == null)
                {
                    continue;
                }

                StartCoroutine(current.PlayActivationFeedback(cardFeedbackDuration));
            }
            yield return new WaitForSeconds(cardFeedbackDuration);

            Dictionary<Card, ActivationSignal> nextWaveMap = new();
            foreach (Card emitter in emitters)
            {
                if (emitter.Data == null)
                {
                    continue;
                }

                bool bufferSource = IsBufferCard(emitter);
                foreach (AbilityDirection dir in emitter.Data.GetAllDirections())
                {
                    BoardSlot neighbor = boardManager.GetNeighbor(emitter.CurrentSlot, dir);
                    if (neighbor != null && neighbor.OccupiedCard != null)
                    {
                        Card target = neighbor.OccupiedCard;
                        if (!nextWaveMap.TryGetValue(target, out ActivationSignal signal))
                        {
                            signal = new ActivationSignal
                            {
                                target = target,
                                hasBufferSource = false
                            };
                            nextWaveMap[target] = signal;
                        }

                        signal.hasBufferSource |= bufferSource;
                    }
                }
            }

            currentWave = new List<ActivationSignal>(nextWaveMap.Values);

            if (currentWave.Count > 0 && chainStepDelay > 0f)
            {
                yield return new WaitForSeconds(chainStepDelay);
            }
        }
    }

    private void EvaluateEndState()
    {
        if (deckManager.HandCount > 0)
        {
            return;
        }

        bool success = boardManager.AreAllPlacedCardsActivated();
        levelEnded = true;

        if (uiManager == null)
        {
            return;
        }

        if (success)
        {
            uiManager.ShowClear();
        }
        else
        {
            uiManager.ShowFail();
        }
    }

    [System.Serializable]
    private class GameSnapshot
    {
        public List<CardData> handCards = new();
        public List<PlacedCardRuntime> placedCards = new();
    }

    [System.Serializable]
    private class PlacedCardRuntime
    {
        public CardData card;
        public Vector2Int position;
        public bool isActivated;
        public int shieldCharges;
    }

    private class ActivationSignal
    {
        public Card target;
        public bool hasBufferSource;
    }

    private static bool IsBufferCard(Card card)
    {
        return card != null
            && card.Data != null
            && card.Data.abilityType == CardAbilityType.Buffer;
    }
}
