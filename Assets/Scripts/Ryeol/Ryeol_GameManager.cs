using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테스트 플레이그라운드 씬 전용 GameManager.
/// 클리어/실패 판정 없음. 덱 무제한. 카드 놓기/빼기/되돌리기/재시작 지원.
/// </summary>
public class Ryeol_GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Ryeol_DeckManager deckManager;
    [SerializeField] private Ryeol_UIManager uiManager;

    [Header("Board Settings")]
    [SerializeField] private int rows = 4;
    [SerializeField] private int columns = 5;

    [Header("Chain Settings")]
    [SerializeField] private int maxActivationSteps = 2048;
    [SerializeField] private float chainStepDelay = 0.4f;
    [SerializeField] private float cardFeedbackDuration = 0.22f;

    private bool isResolvingChain;
    private readonly Stack<GameSnapshot> undoStack = new();

    private void Start()
    {
        ResolveRefs();
        CleanupSceneArtifacts();

        if (uiManager != null)
        {
            uiManager.Initialize(this);
        }

        LoadPlayground();
    }

    // ── 외부 호출 ──────────────────────────────────────────

    public bool TryPlaceCard(Card card, BoardSlot targetSlot)
    {
        if (isResolvingChain || card == null || targetSlot == null || !targetSlot.IsEmpty())
        {
            return false;
        }

        undoStack.Push(CaptureSnapshot());

        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        deckManager.NotifyCardPlaced(card);
        StartCoroutine(ResolveChain(card));

        return true;
    }

    /// <summary>보드에서 카드를 다시 패로 돌려보낸다.</summary>
    public bool TryRemoveCard(BoardSlot slot)
    {
        if (isResolvingChain || slot == null || slot.OccupiedCard == null)
        {
            return false;
        }

        undoStack.Push(CaptureSnapshot());

        Card card = slot.OccupiedCard;
        slot.ClearCard();
        card.SetPlaced(null);
        deckManager.ReturnCardToHand(card);

        return true;
    }

    public void UndoLastMove()
    {
        if (undoStack.Count == 0)
        {
            return;
        }

        StopAllCoroutines();
        isResolvingChain = false;

        RestoreSnapshot(undoStack.Pop());
    }

    public void Restart()
    {
        StopAllCoroutines();
        isResolvingChain = false;
        LoadPlayground();
    }

    public bool CanUndo() => undoStack.Count > 0;

    // ── 내부 ───────────────────────────────────────────────

    private void LoadPlayground()
    {
        undoStack.Clear();

        // BoardManager는 기존 코드 그대로 재사용.
        // LevelData 없이 크기만 넘기기 위해 임시 LevelData를 생성한다.
        LevelData tempLevel = ScriptableObject.CreateInstance<LevelData>();
        tempLevel.rows = rows;
        tempLevel.columns = columns;

        boardManager.BuildBoard(tempLevel, null); // owner는 BoardSlot.OnDrop에서만 쓰이므로 null 가능
        Destroy(tempLevel); // 임시 에셋 즉시 해제

        deckManager.BuildInfiniteDeck();

        if (uiManager != null)
        {
            uiManager.RefreshUndoButton();
        }
    }

    private IEnumerator ResolveChain(Card rootCard)
    {
        isResolvingChain = true;
        yield return ActivateChainFrom(rootCard);
        isResolvingChain = false;

        if (uiManager != null)
        {
            uiManager.RefreshUndoButton();
        }
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
                current.CurrentSlot.PulseHighlight(cardFeedbackDuration);

                step++;
                if (step > maxActivationSteps)
                {
                    Debug.LogWarning("Ryeol_GameManager: 안전 한도 초과로 체인 중단.");
                    yield break;
                }

                if (nextState) emitters.Add(current);
            }

            foreach (Card current in currentWave)
            {
                if (current == null) continue;
                StartCoroutine(current.PlayActivationFeedback(cardFeedbackDuration));
            }

            yield return new WaitForSeconds(cardFeedbackDuration);

            HashSet<Card> nextWaveSet = new();
            foreach (Card emitter in emitters)
            {
                if (emitter.Data == null) continue;

                foreach (AbilityDirection dir in emitter.Data.GetAllDirections())
                {
                    BoardSlot neighbor = boardManager.GetNeighbor(emitter.CurrentSlot, dir);
                    if (neighbor != null && neighbor.OccupiedCard != null)
                    {
                        nextWaveSet.Add(neighbor.OccupiedCard);
                    }
                }
            }

            currentWave = new List<Card>(nextWaveSet);

            if (currentWave.Count > 0 && chainStepDelay > 0f)
            {
                yield return new WaitForSeconds(chainStepDelay);
            }
        }
    }

    private GameSnapshot CaptureSnapshot()
    {
        GameSnapshot snap = new();

        foreach (BoardSlot slot in boardManager.Slots.Values)
        {
            if (slot == null || slot.OccupiedCard == null || slot.OccupiedCard.Data == null) continue;

            snap.placedCards.Add(new PlacedCardRuntime
            {
                card = slot.OccupiedCard.Data,
                position = slot.Position,
                isActivated = slot.OccupiedCard.IsActivated
            });
        }

        return snap;
    }

    private void RestoreSnapshot(GameSnapshot snap)
    {
        if (snap == null) return;

        LevelData tempLevel = ScriptableObject.CreateInstance<LevelData>();
        tempLevel.rows = rows;
        tempLevel.columns = columns;
        boardManager.BuildBoard(tempLevel, null);
        Destroy(tempLevel);

        deckManager.BuildInfiniteDeck();

        foreach (PlacedCardRuntime placed in snap.placedCards)
        {
            if (placed?.card == null) continue;

            BoardSlot slot = boardManager.GetSlot(placed.position);
            if (slot == null || !slot.IsEmpty()) continue;

            Card boardCard = deckManager.SpawnBoardCard(placed.card, slot.transform, placed.isActivated);
            if (boardCard == null) continue;

            slot.AssignCard(boardCard);
        }

        if (uiManager != null) uiManager.RefreshUndoButton();
    }

    private void ResolveRefs()
    {
        if (boardManager == null) boardManager = FindFirstObjectByType<BoardManager>();
        if (deckManager == null) deckManager = FindFirstObjectByType<Ryeol_DeckManager>();
        if (uiManager == null) uiManager = FindFirstObjectByType<Ryeol_UIManager>();
    }

    private void CleanupSceneArtifacts()
    {
        foreach (Card c in FindObjectsByType<Card>(FindObjectsSortMode.None)) Destroy(c.gameObject);
        foreach (BoardSlot s in FindObjectsByType<BoardSlot>(FindObjectsSortMode.None)) Destroy(s.gameObject);
    }

    // ── 중첩 타입 ──────────────────────────────────────────

    [System.Serializable]
    private class GameSnapshot
    {
        public List<PlacedCardRuntime> placedCards = new();
    }

    [System.Serializable]
    private class PlacedCardRuntime
    {
        public CardData card;
        public Vector2Int position;
        public bool isActivated;
    }
}
