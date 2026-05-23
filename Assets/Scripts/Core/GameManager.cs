using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonBehaviour<GameManager>
{
    // 상태
    private bool _isResolvingChain;
    public bool IsResolvingChain => _isResolvingChain;

    // undo 스택
    private readonly Stack<GameSnapshot> _undoStack = new();
    public bool CanUndo => _undoStack.Count > 0;

    // 이벤트
    public event Action<bool> OnChainStateChanged;   // true = 체인 시작, false = 체인 종료
    public event Action OnUndoStackChanged;
    public event Action OnRestartRequested;
    public event Action OnUndoRequested;

    public void Init()
    {
        Debug.Log("[GameManager] Init");
    }

    // ── 상태 변경 ──────────────────────────────────────────

    public void SetResolvingChain(bool value)
    {
        _isResolvingChain = value;
        OnChainStateChanged?.Invoke(value);
    }

    // ── 카드 배치/제거 ─────────────────────────────────────

    public bool TryPlaceCard(Card card, BoardSlot targetSlot)
    {
        if (_isResolvingChain || card == null || targetSlot == null || !targetSlot.IsEmpty())
            return false;

        _undoStack.Push(CaptureSnapshot());
        OnUndoStackChanged?.Invoke();

        targetSlot.AssignCard(card);
        card.SetDraggable(false);
        DeckManager.Instance.NotifyCardPlaced(card);

        SetResolvingChain(true);

        return true;
    }

    public bool TryRemoveCard(BoardSlot slot)
    {
        if (_isResolvingChain || slot == null || slot.OccupiedCard == null)
            return false;

        _undoStack.Push(CaptureSnapshot());
        OnUndoStackChanged?.Invoke();

        Card card = slot.OccupiedCard;
        slot.ClearCard();
        DeckManager.Instance.ReturnCardToHand(card);

        return true;
    }

    // ── Undo / Restart ─────────────────────────────────────

    public void RequestUndo()
    {
        if (_isResolvingChain || _undoStack.Count == 0) return;
        OnUndoRequested?.Invoke();
    }

    public void RequestRestart()
    {
        OnRestartRequested?.Invoke();
    }

    public void PopAndRestoreSnapshot()
    {
        if (_undoStack.Count == 0) return;
        RestoreSnapshot(_undoStack.Pop());
        OnUndoStackChanged?.Invoke();
    }

    public void ClearUndoStack()
    {
        _undoStack.Clear();
        OnUndoStackChanged?.Invoke();
    }

    // ── 스냅샷 ─────────────────────────────────────────────

    public GameSnapshot CaptureSnapshot()
    {
        GameSnapshot snap = new();

        foreach (BoardSlot slot in BoardManager.Instance.Slots.Values)
        {
            if (slot?.OccupiedCard?.Data == null) continue;

            snap.PlacedCards.Add(new PlacedCardRecord
            {
                Data = slot.OccupiedCard.Data,
                Position = slot.Position,
                IsActivated = slot.OccupiedCard.IsActivated
            });
        }

        return snap;
    }

    public void RestoreSnapshot(GameSnapshot snap)
    {
        if (snap == null) return;

        BoardManager.Instance.BuildBoard();
        DeckManager.Instance.BuildInfiniteDeck();

        foreach (PlacedCardRecord record in snap.PlacedCards)
        {
            if (record?.Data == null) continue;

            BoardSlot slot = BoardManager.Instance.GetSlot(record.Position);
            if (slot == null || !slot.IsEmpty()) continue;

            Card boardCard = DeckManager.Instance.SpawnBoardCard(
                record.Data,
                slot.transform.position,
                record.IsActivated
            );
            if (boardCard == null) continue;

            slot.AssignCard(boardCard);
        }
    }

    protected override void Dispose()
    {
        base.Dispose();
    }

    // ── 중첩 타입 ──────────────────────────────────────────

    public class GameSnapshot
    {
        public List<PlacedCardRecord> PlacedCards = new();
    }

    public class PlacedCardRecord
    {
        public CardData Data;
        public Vector2Int Position;
        public bool IsActivated;
    }
}