using System;
using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;

public class BattleManager : SingletonBehaviour<BattleManager>
{
    public enum BattlePhase { PlayerTurn, PreserveSelect, ResolvingPlayerTurn, EnemyTurn }

    [Header("Actors")]
    [SerializeField] private Player _player;
    [SerializeField] private Enemy _enemy;

    [Header("Battle Settings")]
    [SerializeField] private List<EnemyDataSO> _enemyList = new();

    [Header("Damage Number")]
    [SerializeField] private DamageNumber _playerDamageNumberPrefab;
    [SerializeField] private Vector3 _playerDamageNumberOffset = new(0f, 1f, 0f);
    [SerializeField, Min(0f)] private float _playerDamageNumberScale = 0.5f;

    private int _currentEnemyIndex = 0;
    private bool _isBattleActive;
    private bool _isEndingBattle;

    public BattlePhase CurrentPhase { get; private set; }
    public bool IsProcessing { get; private set; }
    public Player Player => _player;
    public Enemy Enemy => _enemy;
    public int CurrentStageNumber => HasStageInfo ? Mathf.Clamp(_currentEnemyIndex + 1, 1, TotalStageCount) : 0;
    public int TotalStageCount => _enemyList != null ? _enemyList.Count : 0;
    public bool HasStageInfo => TotalStageCount > 0;

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action OnBattleEnded;
    public event Action<int, int> OnStageChanged;

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
        _isEndingBattle = false;
        _player.ResetPendingAttack();
        _enemy.Setup(enemyData);

        OnStageChanged?.Invoke(CurrentStageNumber, TotalStageCount);
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

        // 튜토리얼에서는 보존 선택 UI 건너뜀
        bool isTutorial = GameManager.Instance.CurrentState == GameManager.GameState.Tutorial;
        if (isTutorial)
        {
            EnterPhase(BattlePhase.ResolvingPlayerTurn);
            StartCoroutine(PlayerTurnRoutine());
        }
        else
        {
            EnterPhase(BattlePhase.PreserveSelect);
        }
    }

    public void ConfirmPreserveSelect()
    {
        if (CurrentPhase != BattlePhase.PreserveSelect || IsProcessing) return;

        EnterPhase(BattlePhase.ResolvingPlayerTurn);
        StartCoroutine(PlayerTurnRoutine());
    }

    public void StartTutorialBattle(EnemyDataSO enemyData)
    {
        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied -= HandleEnemyDied;
        _player.OnDied += HandlePlayerDied;
        _enemy.OnDied += HandleEnemyDied;

        _isBattleActive = true;
        _isEndingBattle = false;
        _player.ResetPendingAttack();
        _enemy.Setup(enemyData);

        EnterPhase(BattlePhase.PlayerTurn);
    }

    public void SetTutorialEnemy(EnemyDataSO enemyData)
    {
        _enemy.Setup(enemyData);
    }

    // ── 전투 액션 ──────────────────────────────────────────

    public int DealDamageToEnemy(int damage)
    {
        return _enemy != null ? _enemy.TakeAttack(damage) : 0;
    }

    private int DealDamageToPlayer(int damage)
    {
        int dealtDamage = _player != null ? _player.TakeAttack(damage) : 0;
        ShowPlayerDamageNumber(dealtDamage);
        return dealtDamage;
    }

    private void ShowPlayerDamageNumber(int damageAmount)
    {
        if (_playerDamageNumberPrefab == null || damageAmount <= 0)
            return;

        Vector3 position = (_player != null ? _player.transform.position : transform.position) + _playerDamageNumberOffset;
        DamageNumber damageNumber = _playerDamageNumberPrefab.Spawn(position, damageAmount);
        if (damageNumber != null)
            damageNumber.transform.localScale = Vector3.one * _playerDamageNumberScale;
    }

    // ── 턴 루틴 ────────────────────────────────────────────

    private IEnumerator PlayerTurnRoutine()
    {
        IsProcessing = true;

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        // 턴 종료 시 이펙트 처리
        ChainExecutor.Instance.ApplyTurnEndEffects();

        int pendingDamage = _player.CurrentPendingAttack;
        if (pendingDamage > 0)
        {
            DealDamageToEnemy(pendingDamage);
            _player.ConsumePendingAttack();

            if (!_isBattleActive || _enemy.IsDead) { IsProcessing = false; yield break; }
        }

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

            slot.ClearCard();
            PoolManager.Instance.Return(card.gameObject);
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        IsProcessing = true;

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        CardManager.Instance.DiscardGrid();

        yield return new WaitForSeconds(0.3f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        _enemy.ExecuteIntents();
        if (_enemy.CurrentIntentTurn != null)
        {
            foreach (EnemyIntentData intent in _enemy.CurrentIntentTurn.intents)
            {
                if (intent.type != EnemyIntentType.Attack || intent.value <= 0) continue;

                if (_enemy.MotionPlayer != null)
                {
                    yield return _enemy.PlayAttackMotion(
                        intent.hits,
                        () => DealDamageToPlayer(intent.value));
                }
                else
                {
                    for (int i = 0; i < intent.hits; i++)
                    {
                        DealDamageToPlayer(intent.value);
                        if (intent.hits > 1)
                            yield return new WaitForSeconds(0.2f);
                    }
                }
            }
        }

        _player.ResetDefense();

        yield return new WaitForSeconds(0.5f);

        if (!_isBattleActive) { IsProcessing = false; yield break; }

        _enemy.AdvanceIntent();

        EnterPhase(BattlePhase.PlayerTurn);

        // 튜토리얼: 적이 살아있으면 재시도
        if (GameManager.Instance.CurrentState == GameManager.GameState.Tutorial
            && TutorialManager.Instance != null)
        {
            if (TutorialManager.Instance.CurrentStep == TutorialStep.Turn3_Free
                && !_enemy.IsDead)
            {
                TutorialManager.Instance.OnTurn3FreeFailed();
                IsProcessing = false;
                yield break;
            }

            if (TutorialManager.Instance.CurrentStep == TutorialStep.Turn2_Place
                && !_enemy.IsDead)
            {
                TutorialManager.Instance.OnTurn2Failed();
                IsProcessing = false;
                yield break;
            }
        }

        CardManager.Instance.DiscardAndDraw();
        IsProcessing = false;
    }

    // ── 전투 종료 ──────────────────────────────────────────

    private void HandlePlayerDied() => StartCoroutine(EndBattleRoutine(false));
    private void HandleEnemyDied()
    {
        TutorialManager.Instance?.OnEnemyDefeated();

        if (GameManager.Instance.CurrentState == GameManager.GameState.Tutorial
            && TutorialManager.Instance != null
            && (TutorialManager.Instance.CurrentStep == TutorialStep.Turn3_Guided
                || TutorialManager.Instance.CurrentStep == TutorialStep.Turn3_Free))
            return;

        StartCoroutine(EndBattleRoutine(true));
    }

    private IEnumerator EndBattleRoutine(bool victory)
    {
        _player.ResetDefense();
        _player.ResetPendingAttack();

        if (_isEndingBattle) yield break;
        _isEndingBattle = true;
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

    public void ResetToPlayerTurn()
    {
        StopAllCoroutines();

        _isBattleActive = true;
        _isEndingBattle = false;
        IsProcessing = false;
        _player.ResetPendingAttack();

        _enemy.OnDied -= HandleEnemyDied;
        _player.OnDied -= HandlePlayerDied;
        _enemy.OnDied += HandleEnemyDied;
        _player.OnDied += HandlePlayerDied;

        EnterPhase(BattlePhase.PlayerTurn);
    }

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
        OnStageChanged = null;
        base.Dispose();
    }
}
