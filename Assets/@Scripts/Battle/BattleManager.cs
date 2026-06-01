using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : SingletonBehaviour<BattleManager>
{
    public enum BattlePhase { PlayerTurn, EnemyTurn }

    [Header("Actors")]
    [SerializeField] private Player _player;
    [SerializeField] private Enemy _enemy;

    [Header("Battle Settings")]
    [SerializeField] private List<EnemyDataSO> _enemyList = new();

    private int _currentEnemyIndex = 0;
    private bool _isBattleActive;

    public BattlePhase CurrentPhase { get; private set; }
    public bool IsProcessing { get; private set; }
    public Player Player => _player;
    public Enemy Enemy => _enemy;

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action OnBattleEnded;

    // ── 초기화 ─────────────────────────────────────────────

    public void Init()
    {
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        Debug.Log("[BattleManager] Init");
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
            _player.Setup(_player.MaxHp);
    }

    // ── 전투 시작 ──────────────────────────────────────────

    public void StartBattle()
    {
        _currentEnemyIndex = 0;
        StartBattleInternal();
    }

    public void NextBattle()
    {
        StartBattleInternal();
    }

    private void StartBattleInternal()
    {
        EnemyDataSO enemyData = _currentEnemyIndex < _enemyList.Count
            ? _enemyList[_currentEnemyIndex]
            : null;

        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied -= HandleEnemyDied;
        _player.OnDied += HandlePlayerDied;
        _enemy.OnDied += HandleEnemyDied;

        _isBattleActive = true;
        _enemy.Setup(enemyData);

        EnterPhase(BattlePhase.PlayerTurn);

        Debug.Log($"[BattleManager] 전투 시작 — {_currentEnemyIndex + 1}/{_enemyList.Count}");
    }

    // ── 플레이어 턴 확정 ───────────────────────────────────

    public void ConfirmPlayerTurn()
    {
        if (CurrentPhase != BattlePhase.PlayerTurn || IsProcessing) return;
        if (GameManager.Instance.CurrentState == GameManager.GameState.Tutorial
            && !TutorialManager.Instance.CanConfirm()) return;

        TutorialManager.Instance?.OnTurnConfirmed();
        StartCoroutine(PlayerTurnRoutine());
    }

    public void StartTutorialBattle(EnemyDataSO enemyData)
    {
        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied -= HandleEnemyDied;
        _player.OnDied += HandlePlayerDied;
        _enemy.OnDied += HandleEnemyDied;

        _isBattleActive = true;
        _enemy.Setup(enemyData);

        EnterPhase(BattlePhase.PlayerTurn);
    }

    public void SetTutorialEnemy(EnemyDataSO enemyData)
    {
        _enemy.Setup(enemyData);
    }

    // ── 전투 액션 ──────────────────────────────────────────

    public void DealDamageToEnemy(int damage)
    {
        _enemy.TakeAttack(damage);
    }

    // ── 턴 루틴 ────────────────────────────────────────────

    private IEnumerator PlayerTurnRoutine()
    {
        IsProcessing = true;

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        // 오염 카드 처리 — 아직 ON 상태인 오염 카드당 저주 카드 삽입
        ProcessContaminateCards();

        // 손패 저주 카드 데미지
        int curseDamage = CardManager.Instance.GetCurseHandDamage();
        if (curseDamage > 0)
        {
            _player.TakeAttack(curseDamage);
            Debug.Log($"[BattleManager] 손패 저주 카드 데미지 {curseDamage}");
            yield return new WaitForSeconds(0.3f);
        }

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        IsProcessing = false;
        EnterPhase(BattlePhase.EnemyTurn);
        StartCoroutine(EnemyTurnRoutine());
    }

    private void ProcessContaminateCards()
    {
        List<GridSlot> contaminateSlots = new();

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot.IsEmpty) continue;
            CardView card = slot.OccupiedCard;
            if (!card.IsEnemy || card.ContaminateCurseCount <= 0) continue;
            contaminateSlots.Add(slot);
        }

        foreach (GridSlot slot in contaminateSlots)
        {
            CardView card = slot.OccupiedCard;

            // 아직 ON 상태면 저주 카드 삽입
            if (card.IsActivated)
            {
                CardData curseCard = _enemy.CurseCardData;
                if (curseCard != null)
                {
                    for (int i = 0; i < card.ContaminateCurseCount; i++)
                        CardManager.Instance.InsertCurseCard(curseCard);
                    Debug.Log($"[BattleManager] 오염 카드 미제거 — 저주 카드 {card.ContaminateCurseCount}장 삽입");
                }
            }
            else
            {
                Debug.Log("[BattleManager] 오염 카드 제거 성공 — 저주 없음");
            }

            // ON/OFF 상관없이 오염 카드 그리드에서 제거
            slot.ClearCard();
            PoolManager.Instance.Return(card.gameObject);
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        IsProcessing = true;

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        // 플레이어 카드 먼저 정리
        CardManager.Instance.DiscardGrid();

        yield return new WaitForSeconds(0.3f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        // 현재 Intent 실행: Defend → Defense 쌓음, Attack → hits만큼 반복 피격, Contaminate → 오염 카드 배치
        _enemy.ExecuteIntents();
        if (_enemy.CurrentIntentTurn != null)
        {
            foreach (EnemyIntentData intent in _enemy.CurrentIntentTurn.intents)
            {
                if (intent.type != EnemyIntentType.Attack || intent.value <= 0) continue;
                for (int i = 0; i < intent.hits; i++)
                {
                    if (_enemy.MotionPlayer != null)
                        yield return _enemy.PlayAttackMotion();

                    _player.TakeAttack(intent.value);
                    if (intent.hits > 1)
                        yield return new WaitForSeconds(0.2f);
                }
            }
        }

        // 적 공격 이후 플레이어 Defense 리셋
        _player.ResetDefense();

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        // 다음 플레이어 턴에 보여줄 Intent로 갱신 (실행 아님)
        _enemy.AdvanceIntent();

        IsProcessing = false;
        EnterPhase(BattlePhase.PlayerTurn);
        CardManager.Instance.DiscardAndDraw();
    }

    // ── 전투 종료 ──────────────────────────────────────────

    private void HandlePlayerDied() => StartCoroutine(EndBattleRoutine(false));
    private void HandleEnemyDied()
    {
        TutorialManager.Instance?.OnEnemyDefeated();
        StartCoroutine(EndBattleRoutine(true));
    }

    private IEnumerator EndBattleRoutine(bool victory)
    {
        _isBattleActive = false;
        IsProcessing = false;
        Debug.Log($"[BattleManager] 전투 종료 — {(victory ? "승리" : "패배")}");

        if (victory && _enemy != null && _enemy.MotionPlayer != null)
            yield return _enemy.WaitForDieMotion();

        yield return new WaitForSecondsRealtime(1f);

        if (!victory)
        {
            GameManager.Instance.GameOver();
            yield break;
        }

        _currentEnemyIndex++;

        if (_currentEnemyIndex >= _enemyList.Count)
        {
            GameManager.Instance.GameClear();
            yield break;
        }

        OnBattleEnded?.Invoke();
    }

    // ── 유틸 ───────────────────────────────────────────────

    private void EnterPhase(BattlePhase phase)
    {
        CurrentPhase = phase;

        if (phase == BattlePhase.PlayerTurn)
            _player.RestoreCost();

        OnPhaseChanged?.Invoke(phase);
        Debug.Log($"[BattleManager] Phase → {phase}");
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;

        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied -= HandleEnemyDied;

        OnPhaseChanged = null;
        OnBattleEnded = null;
        base.Dispose();
    }
}