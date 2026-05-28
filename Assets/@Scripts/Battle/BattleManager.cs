using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : SingletonBehaviour<BattleManager>
{
    public enum TurnState { PlayerTurn, EnemyTurn }

    [Header("Actor Views")]
    [SerializeField] private BattleActorView _playerView;
    [SerializeField] private BattleActorView _enemyView;

    // ── 전투 상태 ──────────────────────────────────────────
    public TurnState CurrentTurn { get; private set; }
    public bool IsChainRunning { get; private set; }
    public int PlayerHp => _playerView != null ? _playerView.CurrentHp : 0;

    // ── 이벤트 ────────────────────────────────────────────
    public event Action<TurnState> OnTurnStateChanged;
    public event Action<EnemyAction> OnEnemyIntentChanged;
    public event Action<bool> OnBattleEnded;

    // ── 내부 상태 ─────────────────────────────────────────
    private EnemyDataSO _currentEnemyData;
    private int _playerMaxHp;
    private int _enemyPatternIndex;
    private int _enemyBuffDamage;
    private int _enemyCurrentDefense;

    public void Init()
    {
        ChainExecutor.Instance.OnChainStarted += OnChainStarted;
        ChainExecutor.Instance.OnChainFinished += OnChainFinished;
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        Debug.Log("[BattleManager] Init");
    }

    private void OnChainStarted() => IsChainRunning = true;

    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
            _playerMaxHp = 0;
    }

    // ── 전투 시작 ──────────────────────────────────────────

    public void StartBattle(int playerHp, EnemyDataSO enemyData)
    {
        _currentEnemyData = enemyData;
        _enemyPatternIndex = 0;
        _enemyBuffDamage = 0;
        _enemyCurrentDefense = 0;

        int enemyHp = enemyData != null ? enemyData.maxHp : 20;
        string enemyName = enemyData != null ? enemyData.displayName : "Enemy";

        if (_playerMaxHp == 0) _playerMaxHp = playerHp;

        _playerView.Setup("Player", _playerMaxHp, playerHp);
        _enemyView.Setup(enemyName, enemyHp);

        _playerView.OnDied += () => EndBattle(false);
        _enemyView.OnDied += () => EndBattle(true);

        CardManager.Instance.StartBattleDraw();

        OnEnemyIntentChanged?.Invoke(GetCurrentEnemyIntent());
        EnterPlayerTurn();

        Debug.Log($"[BattleManager] 전투 시작 — 플레이어 HP: {playerHp} / 적 HP: {enemyHp}");
    }

    // ── 배치 시 체인 트리거 ───────────────────────────────

    public void TriggerChainFromCard(CardView card)
    {
        if (CurrentTurn != TurnState.PlayerTurn) return;
        if (IsChainRunning) return;
        if (card == null || card.CurrentSlot == null) return;

        ChainExecutor.Instance.ExecuteFrom(card);
    }

    // ── 확정 버튼 (턴 종료) ───────────────────────────────

    public void ConfirmPlayerTurn()
    {
        if (CurrentTurn != TurnState.PlayerTurn || IsChainRunning) return;
        StartCoroutine(PlayerTurnEndRoutine());
    }

    // ── 카드 묘지 회수 ────────────────────────────────────

    public void RecallCardToGraveyard(CardView card)
    {
        if (CurrentTurn != TurnState.PlayerTurn || IsChainRunning) return;
        if (card == null || card.CurrentSlot == null) return;

        GridSlot slot = card.CurrentSlot;
        UserCardPool.Instance.DiscardToGraveyard(card.GetCardInstance());
        slot.ClearCard();
        PoolManager.Instance.Return(card.gameObject);

        Debug.Log("[BattleManager] 카드 묘지 회수");
    }

    private void OnChainFinished(ChainResult _)
    {
        IsChainRunning = false; // 전파 완료 — 다음 카드 배치 허용
    }

    // ── 플레이어 턴 종료 루틴 ─────────────────────────────

    private IEnumerator PlayerTurnEndRoutine()
    {
        IsChainRunning = true;

        // 큐에 쌓인 모든 효과를 순차 실행 (UI 항목 제거 포함)
        ChainResult result = new ChainResult();
        yield return StartCoroutine(ChainExecutor.Instance.ExecutePendingEffects(result));

        int damage = Mathf.RoundToInt(result.damage);
        int defense = Mathf.RoundToInt(result.defense);
        int heal = Mathf.RoundToInt(result.heal);

        int effectiveDamage = Mathf.Max(0, damage - _enemyCurrentDefense);
        _enemyCurrentDefense = 0;

        _enemyView.TakeDamage(effectiveDamage);
        yield return new WaitForSeconds(0.5f);

        if (_enemyView.IsDead) { IsChainRunning = false; yield break; }

        if (heal > 0) _playerView.Heal(heal);

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(EnemyTurnRoutine(defense));

        IsChainRunning = false;
    }

    // ── 적 턴 루틴 ────────────────────────────────────────

    private IEnumerator EnemyTurnRoutine(int playerDefense)
    {
        CurrentTurn = TurnState.EnemyTurn;
        OnTurnStateChanged?.Invoke(TurnState.EnemyTurn);

        EnemyAction action = GetCurrentEnemyIntent();

        switch (action.actionType)
        {
            case EnemyActionType.Attack:
                int incoming = Mathf.Max(0, action.value + _enemyBuffDamage - playerDefense);
                _enemyBuffDamage = 0;
                _playerView.TakeDamage(incoming);
                break;

            case EnemyActionType.Defense:
                _enemyCurrentDefense += action.value;
                break;

            case EnemyActionType.Buff:
                _enemyBuffDamage += action.value;
                break;
        }

        yield return new WaitForSeconds(0.5f);

        if (_playerView.IsDead) yield break;

        if (_currentEnemyData != null && _currentEnemyData.actions.Count > 0)
            _enemyPatternIndex = (_enemyPatternIndex + 1) % _currentEnemyData.actions.Count;

        OnEnemyIntentChanged?.Invoke(GetCurrentEnemyIntent());

        yield return new WaitForSeconds(0.3f);

        EnterPlayerTurn();
    }

    // ── 플레이어 턴 시작 ──────────────────────────────────

    private void EnterPlayerTurn()
    {
        CurrentTurn = TurnState.PlayerTurn;
        ChainExecutor.Instance.ClearPendingEffects();

        ClearFieldCards();
        CardManager.Instance.DrawToHand(UserCardPool.Instance.InitialDrawCount);
        OnTurnStateChanged?.Invoke(TurnState.PlayerTurn);

        Debug.Log("[BattleManager] 플레이어 턴 시작");
    }

    private void ClearFieldCards()
    {
        List<GridSlot> occupied = new();
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (!slot.IsEmpty) occupied.Add(slot);

        foreach (GridSlot slot in occupied)
        {
            CardView card = slot.OccupiedCard;
            UserCardPool.Instance.DiscardToGraveyard(card.GetCardInstance());
            slot.ClearCard();
            PoolManager.Instance.Return(card.gameObject);
        }
        Debug.Log($"[BattleManager] 필드 카드 전체 제거 — {occupied.Count}장");
    }

    // ── 적 인텐트 ──────────────────────────────────────────

    public EnemyAction GetCurrentEnemyIntent()
    {
        if (_currentEnemyData == null || _currentEnemyData.actions == null || _currentEnemyData.actions.Count == 0)
            return new EnemyAction { actionType = EnemyActionType.Attack, value = 5, description = "ATK" };

        return _currentEnemyData.actions[_enemyPatternIndex % _currentEnemyData.actions.Count];
    }

    // ── 전투 종료 ─────────────────────────────────────────

    private void EndBattle(bool victory)
    {
        OnBattleEnded?.Invoke(victory);
        Debug.Log($"[BattleManager] 전투 종료 — {(victory ? "승리" : "패배")}");
    }

    protected override void Dispose()
    {
        if (ChainExecutor.Instance != null)
        {
            ChainExecutor.Instance.OnChainStarted -= OnChainStarted;
            ChainExecutor.Instance.OnChainFinished -= OnChainFinished;
        }
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;

        OnTurnStateChanged = null;
        OnEnemyIntentChanged = null;
        OnBattleEnded = null;
        base.Dispose();
    }
}
