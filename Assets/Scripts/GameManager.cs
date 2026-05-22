using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private enum BattleState
    {
        EnemySpawning,
        PlayerAction,
        ResolvingCardEffects,
        ResolvingCombat,
        GameOver
    }

    [Header("Refs")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerBattleController playerController;
    [SerializeField] private UserCardPool userCardPool;
    [SerializeField] private BattleActorView playerActor;
    [SerializeField] private EnemyBattleController enemyController;
    [SerializeField] private BattleActorView enemyActor;
    [SerializeField] private Transform enemySpawnPoint;

    [Header("Rounds")]
    [SerializeField] private List<BattleRoundSO> rounds = new();
    [SerializeField] private int startRoundIndex;

    [Header("Safety")]
    [SerializeField] private int maxActivationSteps = 2048;
    [SerializeField] private int maxEmitsPerCardInContext = 4;
    [SerializeField] private float chainStepDelay = 0.2f;
    [SerializeField] private float cardFeedbackDuration = 0.22f;
    [SerializeField] private float combatStepDelay = 0.35f;

    private int currentRoundIndex;
    private int turnNumber;
    private BattleRoundSO currentRound;
    private UserCardPool activeUserCardPool;
    private GameObject spawnedEnemyObject;
    private BattleState state = BattleState.EnemySpawning;
    private readonly Stack<GameSnapshot> undoStack = new();
    private readonly List<CardData> playedPlayerCardsThisTurn = new();

    public event System.Action<int, int> DamagePreviewChanged;

    private void Start()
    {
        ResolveRefs();
        CleanupSceneArtifacts();

        if (rounds == null || rounds.Count == 0)
        {
            Debug.LogError("GameManager: Round list is empty.");
            return;
        }

        if (boardManager == null || deckManager == null)
        {
            Debug.LogError("GameManager: Missing BoardManager or DeckManager.");
            return;
        }

        currentRoundIndex = Mathf.Clamp(startRoundIndex, 0, Mathf.Max(0, rounds.Count - 1));

        if (uiManager != null)
        {
            uiManager.Initialize(this);
        }

        StartBattleRound();
    }

    public bool TryPlaceCardFromHand(Card card, BoardSlot targetSlot)
    {
        if (state != BattleState.PlayerAction || card == null || card.IsEnemy || card.IsPlacedOnBoard || targetSlot == null || !targetSlot.IsEmpty())
        {
            return false;
        }

        Debug.Log($"GameManager: Player place card {card.Data?.displayName} at {targetSlot.Position}.");
        undoStack.Push(CaptureSnapshot());

        targetSlot.AssignCard(card);
        card.SetActivated(true);
        card.SetDraggable(false);
        deckManager.RemoveFromHand(card);

        if (card.Data != null)
        {
            playedPlayerCardsThisTurn.Add(card.Data);
        }

        PublishDamagePreview();
        StartCoroutine(ResolvePlacedCardEffects(card));
        return true;
    }

    public void EndTurn()
    {
        if (state != BattleState.PlayerAction)
        {
            Debug.Log($"GameManager: EndTurn ignored in state {state}.");
            return;
        }

        StartCoroutine(ResolveCombat());
    }

    public void RestartCurrentLevel()
    {
        StopAllCoroutines();
        StartBattleRound();
    }

    public void UndoLastMove()
    {
        if (state != BattleState.PlayerAction || undoStack.Count == 0)
        {
            Debug.Log("GameManager: Undo ignored.");
            return;
        }

        Debug.Log("GameManager: Undo last player placement.");
        RestoreSnapshot(undoStack.Pop());
        PublishDamagePreview();
    }

    public void LoadNextLevel()
    {
        int next = currentRoundIndex + 1;
        if (rounds == null || next >= rounds.Count)
        {
            return;
        }

        currentRoundIndex = next;
        RestartCurrentLevel();
    }

    public bool IsLastLevel()
    {
        return rounds != null && rounds.Count > 0 && currentRoundIndex >= rounds.Count - 1;
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

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerBattleController>();
        }

        if (userCardPool == null)
        {
            userCardPool = playerController != null && playerController.CardPool != null ? playerController.CardPool : FindFirstObjectByType<UserCardPool>();
        }

        if (playerActor == null)
        {
            playerActor = playerController != null ? playerController.ActorView : null;
            if (playerActor == null)
            {
                GameObject found = FindSceneObjectByName("PlayerActor");
                playerActor = found != null ? found.GetComponent<BattleActorView>() : null;
            }
        }

        if (enemySpawnPoint == null)
        {
            GameObject found = FindSceneObjectByName("EnemySpawnPoint");
            enemySpawnPoint = found != null ? found.transform : null;
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

    private void StartBattleRound()
    {
        if (rounds == null || rounds.Count == 0)
        {
            Debug.LogError("GameManager: Round list is empty.");
            return;
        }

        currentRound = rounds[currentRoundIndex];
        if (currentRound == null)
        {
            Debug.LogError($"GameManager: Missing BattleRoundSO at index {currentRoundIndex}.");
            return;
        }

        if (currentRound.gridData == null)
        {
            Debug.LogError($"GameManager: Missing GridDataSO on round {currentRound.name}.");
            return;
        }

        if (currentRound.enemyData == null)
        {
            Debug.LogError($"GameManager: Missing EnemyDataSO on round {currentRound.name}.");
            return;
        }

        if (playerController != null)
        {
            playerController.InitializeForBattle();
        }
        else
        {
            playerActor?.ResetHp();
        }

        playerActor = playerController != null && playerController.ActorView != null ? playerController.ActorView : playerActor;
        userCardPool = playerController != null && playerController.CardPool != null ? playerController.CardPool : userCardPool;
        activeUserCardPool = userCardPool;
        if (activeUserCardPool == null)
        {
            Debug.LogError("GameManager: Missing active UserCardPool.");
            return;
        }

        turnNumber = 0;
        state = BattleState.EnemySpawning;
        undoStack.Clear();
        playedPlayerCardsThisTurn.Clear();
        deckManager.ClearCurrentHand();
        boardManager.BuildBoard(currentRound.gridData, this);
        activeUserCardPool.ResetForBattle();
        SpawnRoundEnemy(currentRound.enemyData);

        Debug.Log($"GameManager: Round {currentRoundIndex + 1} start. round={currentRound.name}");
        StartTurn();
    }

    private void StartTurn()
    {
        if (currentRound == null)
        {
            Debug.LogError("GameManager: Cannot start turn without current round.");
            return;
        }

        turnNumber++;
        state = BattleState.EnemySpawning;
        undoStack.Clear();
        playedPlayerCardsThisTurn.Clear();

        Debug.Log($"GameManager: Turn {turnNumber} start.");
        SpawnEnemyCards(currentRound);
        PublishDamagePreview();

        List<CardData> hand = activeUserCardPool.DrawCards(activeUserCardPool.DrawCount);
        deckManager.BuildHand(hand);

        state = BattleState.PlayerAction;
        Debug.Log($"GameManager: Player action start. hand={deckManager.HandCount}");
    }

    private void SpawnRoundEnemy(EnemyDataSO enemyData)
    {
        if (spawnedEnemyObject != null)
        {
            Destroy(spawnedEnemyObject);
            spawnedEnemyObject = null;
            enemyController = null;
            enemyActor = null;
        }

        if (enemySpawnPoint == null)
        {
            GameObject found = FindSceneObjectByName("EnemySpawnPoint");
            enemySpawnPoint = found != null ? found.transform : null;
        }

        if (enemySpawnPoint == null)
        {
            Debug.LogError("GameManager: Missing EnemySpawnPoint.");
            return;
        }

        spawnedEnemyObject = new GameObject("EnemyActor", typeof(BattleActorView), typeof(EnemyBattleController));
        spawnedEnemyObject.transform.SetPositionAndRotation(enemySpawnPoint.position, enemySpawnPoint.rotation);
        spawnedEnemyObject.transform.localScale = Vector3.one;

        enemyController = spawnedEnemyObject.GetComponent<EnemyBattleController>();
        enemyController.Initialize(enemyData);
        enemyActor = enemyController.ActorView;

        Debug.Log($"GameManager: Spawn enemy {enemyData.displayName} at {enemySpawnPoint.position}.");
    }

    private void SpawnEnemyCards(BattleRoundSO round)
    {
        if (round == null || round.enemyDeck == null || round.enemyDeck.cards == null || round.enemyDeck.cards.Count == 0)
        {
            Debug.LogWarning("GameManager: Enemy deck is empty.");
            return;
        }

        List<BoardSlot> emptySlots = boardManager.GetEmptySlots();
        if (emptySlots.Count == 0)
        {
            Debug.LogWarning("GameManager: No empty slot for enemy spawn.");
            return;
        }

        int spawnCount = Mathf.Min(Mathf.Max(0, round.enemyCardsPerTurn), round.enemyDeck.cards.Count, emptySlots.Count);
        if (spawnCount < round.enemyCardsPerTurn)
        {
            Debug.LogWarning($"GameManager: Enemy spawn count reduced. requested={round.enemyCardsPerTurn}, actual={spawnCount}");
        }

        List<CardData> selectedCards = PickRandomCards(round.enemyDeck.cards, spawnCount);
        ShuffleSlots(emptySlots);

        for (int i = 0; i < selectedCards.Count; i++)
        {
            CardData cardData = selectedCards[i];
            BoardSlot slot = emptySlots[i];
            Card boardCard = deckManager.SpawnBoardCard(cardData, slot.transform, CardTeam.Enemy, true);
            if (boardCard == null)
            {
                continue;
            }

            slot.AssignCard(boardCard);
            Debug.Log($"GameManager: Enemy card {cardData.displayName} spawned at {slot.Position}. No initial effect.");
        }
    }

    private IEnumerator ResolvePlacedCardEffects(Card rootCard)
    {
        state = BattleState.ResolvingCardEffects;
        yield return EmitDirectionChain(rootCard);
        state = BattleState.PlayerAction;
        PublishDamagePreview();
        Debug.Log("GameManager: Card effect chain resolved.");
    }

    private IEnumerator EmitDirectionChain(Card rootCard)
    {
        if (rootCard == null)
        {
            yield break;
        }

        Queue<Card> emitQueue = new();
        Dictionary<Card, int> emitCounts = new();
        emitQueue.Enqueue(rootCard);

        int step = 0;
        while (emitQueue.Count > 0)
        {
            Card emitter = emitQueue.Dequeue();
            if (emitter == null || emitter.CurrentSlot == null || emitter.Data == null)
            {
                continue;
            }

            emitCounts.TryGetValue(emitter, out int count);
            if (count >= maxEmitsPerCardInContext)
            {
                Debug.LogWarning($"GameManager: Emit skipped by per-card limit. card={emitter.Data.displayName}");
                continue;
            }
            emitCounts[emitter] = count + 1;

            Debug.Log($"GameManager: Emit direction effect from {emitter.Data.displayName} at {emitter.CurrentSlot.Position}.");

            foreach (AbilityDirection dir in emitter.Data.GetAllDirections())
            {
                BoardSlot neighbor = boardManager.GetNeighbor(emitter.CurrentSlot, dir);
                Card target = neighbor != null ? neighbor.OccupiedCard : null;
                if (target == null)
                {
                    continue;
                }

                bool wasActivated = target.IsActivated;
                bool nextState = !wasActivated;
                target.SetActivated(nextState);
                target.CurrentSlot?.PulseHighlight(cardFeedbackDuration);
                StartCoroutine(target.PlayActivationFeedback(cardFeedbackDuration));

                Debug.Log($"GameManager: Toggle {target.Data?.displayName} at {target.CurrentSlot?.Position}: {wasActivated} -> {nextState}");
                PublishDamagePreview();

                step++;
                if (step > maxActivationSteps)
                {
                    Debug.LogWarning("GameManager: Activation chain stopped by safety limit.");
                    yield break;
                }

                if (!wasActivated && nextState)
                {
                    emitQueue.Enqueue(target);
                }
            }

            if (chainStepDelay > 0f)
            {
                yield return new WaitForSeconds(chainStepDelay);
            }
        }
    }

    private IEnumerator ResolveCombat()
    {
        state = BattleState.ResolvingCombat;
        Debug.Log("GameManager: End turn. Resolve combat.");

        int playerAttackPower = boardManager.CountActivatedCards(CardTeam.Ally);
        int enemyAttackPower = boardManager.CountActivatedCards(CardTeam.Enemy);

        Debug.Log($"GameManager: Damage calculation. playerAttackPower={playerAttackPower}, enemyAttackPower={enemyAttackPower}");

        if (playerAttackPower > 0)
        {
            enemyActor?.TakeDamage(playerAttackPower);
            yield return new WaitForSeconds(combatStepDelay);
        }

        if (enemyActor != null && enemyActor.IsDead)
        {
            state = BattleState.GameOver;
            Debug.Log("GameManager: Enemy defeated.");
            uiManager?.ShowClear();
            yield break;
        }

        if (enemyAttackPower > 0)
        {
            playerActor?.TakeDamage(enemyAttackPower);
            yield return new WaitForSeconds(combatStepDelay);
        }

        if (playerActor != null && playerActor.IsDead)
        {
            state = BattleState.GameOver;
            Debug.Log("GameManager: Player defeated.");
            uiManager?.ShowFail();
            yield break;
        }

        CleanupTurnCards();
        StartTurn();
    }

    private void CleanupTurnCards()
    {
        List<CardData> discardCards = new();
        discardCards.AddRange(deckManager.CaptureHandState());
        discardCards.AddRange(playedPlayerCardsThisTurn);
        activeUserCardPool.DiscardMany(discardCards);

        deckManager.ClearCurrentHand();
        boardManager.RemoveAllCards();
        undoStack.Clear();
        PublishDamagePreview();

        Debug.Log($"GameManager: Turn cleanup. discardedPlayerCards={discardCards.Count}");
    }

    private GameSnapshot CaptureSnapshot()
    {
        GameSnapshot snapshot = new()
        {
            handCards = deckManager.CaptureHandState(),
            playedCards = new List<CardData>(playedPlayerCardsThisTurn),
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
                team = slot.OccupiedCard.Team,
                isActivated = slot.OccupiedCard.IsActivated
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

        boardManager.RemoveAllCards();
        deckManager.BuildHand(snapshot.handCards);
        playedPlayerCardsThisTurn.Clear();
        playedPlayerCardsThisTurn.AddRange(snapshot.playedCards);

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

            Card boardCard = deckManager.SpawnBoardCard(placed.card, slot.transform, placed.team, placed.isActivated);
            if (boardCard == null)
            {
                continue;
            }

            slot.AssignCard(boardCard);
        }
    }

    private void PublishDamagePreview()
    {
        if (boardManager == null)
        {
            DamagePreviewChanged?.Invoke(0, 0);
            return;
        }

        int playerDamage = boardManager.CountActivatedCards(CardTeam.Ally);
        int enemyDamage = boardManager.CountActivatedCards(CardTeam.Enemy);
        DamagePreviewChanged?.Invoke(playerDamage, enemyDamage);
        Debug.Log($"GameManager: Damage preview changed. playerDamage={playerDamage}, enemyDamage={enemyDamage}");
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            if (go == null || !go.scene.IsValid())
            {
                continue;
            }

            if (go.name == objectName)
            {
                return go;
            }
        }

        return null;
    }

    private static List<CardData> PickRandomCards(List<CardData> source, int count)
    {
        List<CardData> pool = new();
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
            {
                pool.Add(source[i]);
            }
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (pool[i], pool[swapIndex]) = (pool[swapIndex], pool[i]);
        }

        if (pool.Count > count)
        {
            pool.RemoveRange(count, pool.Count - count);
        }

        return pool;
    }

    private static void ShuffleSlots(List<BoardSlot> slots)
    {
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (slots[i], slots[swapIndex]) = (slots[swapIndex], slots[i]);
        }
    }

    [System.Serializable]
    private class GameSnapshot
    {
        public List<CardData> handCards = new();
        public List<CardData> playedCards = new();
        public List<PlacedCardRuntime> placedCards = new();
    }

    [System.Serializable]
    private class PlacedCardRuntime
    {
        public CardData card;
        public Vector2Int position;
        public CardTeam team = CardTeam.Ally;
        public bool isActivated;
    }
}
