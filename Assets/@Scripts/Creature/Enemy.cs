using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private BattleActorView _view;
    [SerializeField] private Transform _motionRoot;
    [SerializeField] private Vector3 _motionSpawnOffset = new(0f, -0.3f, 0f);
    [SerializeField] private BattleActorMotionTarget _motionTarget;
    [SerializeField] private SpumEnemyMotionPlayer _motionPlayer;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private CardData _contaminateCardData;
    [SerializeField] private CardData _curseCardData;

    private EnemyDataSO _data;
    private int _intentIndex = -1;
    private BattleActorMotionTarget _fallbackMotionTarget;
    private SpumEnemyMotionPlayer _fallbackMotionPlayer;
    private SpumEnemyMotionPlayer _spawnedMotionPlayer;

    public bool IsDead => _view != null && _view.IsDead;
    public int CurrentDefense { get; private set; }
    public EnemyIntentTurn CurrentIntentTurn { get; private set; }
    public CardData CurseCardData => _curseCardData;
    public BattleActorMotionTarget MotionTarget => _motionTarget;
    public Transform ViewTransform => _motionTarget != null ? _motionTarget.AttackPoint : transform;
    public SpumEnemyMotionPlayer MotionPlayer => _motionPlayer;

    public event Action OnDied;
    public event Action<EnemyIntentTurn> OnIntentChanged;

    // ── 초기화 ─────────────────────────────────────────────

    private void Awake()
    {
        _fallbackMotionTarget = _motionTarget;
        _fallbackMotionPlayer = _motionPlayer;
    }

    public void Setup(EnemyDataSO data)
    {
        _data = data;
        CurrentDefense = 0;
        ApplyMotionPrefab(data != null ? data.motionPrefab : null);

        _view.OnDied -= HandleDied;
        _view.OnDied += HandleDied;
        string displayName = data != null ? data.displayName : "Enemy";
        int maxHp = data != null ? data.maxHp : 20;
        _view.Setup(displayName, maxHp);

        // 첫 플레이어 턴에 표시할 Intent 로드 (실행 아님)
        RefreshIntent();
    }

    // ── 전투 로직 ──────────────────────────────────────────

    public int TakeAttack(int damage)
    {
        int blocked = Mathf.Min(CurrentDefense, damage);
        CurrentDefense -= blocked;
        int remaining = damage - blocked;
        int finalDamage = Mathf.Max(0, remaining);
        bool willDie = finalDamage > 0 && _view != null && _view.CurrentHp - finalDamage <= 0;

        if (willDie)
            _motionPlayer?.PlayDie();
        else if (finalDamage > 0)
            _motionPlayer?.PlayHit();

        _view.SetDefense(CurrentDefense);
        _view.TakeDamage(finalDamage);
        return finalDamage;
    }

    public IEnumerator PlayAttackMotion()
    {
        if (_motionPlayer != null)
            yield return _motionPlayer.PlayAttackRoutine();
    }

    public IEnumerator PlayAttackMotion(int hitCount)
    {
        if (_motionPlayer != null)
            yield return _motionPlayer.PlayAttackRoutine(hitCount);
    }

    public IEnumerator PlayAttackMotion(int hitCount, Action onAttackFinished)
    {
        if (_motionPlayer != null)
            yield return _motionPlayer.PlayAttackRoutine(hitCount, onAttackFinished);
    }

    public IEnumerator WaitForDieMotion()
    {
        if (_motionPlayer != null)
            yield return _motionPlayer.WaitForDieMotion();
    }

    public int GetIntentValue(EnemyIntentType type)
    {
        if (CurrentIntentTurn == null) return 0;

        int total = 0;
        foreach (EnemyIntentData intent in CurrentIntentTurn.intents)
            if (intent.type == type)
                total += intent.value;
        return total;
    }

    /// <summary>
    /// 적 턴에 현재 Intent를 실행합니다.
    /// Attack은 BattleManager에서 GetIntentValue로 처리.
    /// Defend는 여기서 Defense로 쌓음.
    /// </summary>
    public void ExecuteIntents()
    {
        if (CurrentIntentTurn == null) return;

        CurrentDefense = 0;
        foreach (EnemyIntentData intent in CurrentIntentTurn.intents)
        {
            switch (intent.type)
            {
                case EnemyIntentType.Defend:
                    CurrentDefense += intent.value;
                    break;
                case EnemyIntentType.Contaminate:
                    ExecuteContaminate(intent.spawnCount, intent.cursePerCard);
                    break;
            }
        }

        _view.SetDefense(CurrentDefense);
        Debug.Log($"[Enemy] Intent 실행 완료 — Defense {CurrentDefense}");
    }

    private void ExecuteContaminate(int count, int cursePerCard)
    {
        if (_contaminateCardData == null || _cardPrefab == null) return;

        var emptySlots = GridManager.Instance.GetEmptySlots();
        Shuffle(emptySlots);
        int placed = 0;

        foreach (GridSlot slot in emptySlots)
        {
            if (placed >= count) break;
            CardView card = GridManager.Instance.PlaceEnemyCard(
                _contaminateCardData, _cardPrefab, slot.Position, startsActivated: true);
            if (card != null)
            {
                card.SetContaminateCurseCount(cursePerCard);
                placed++;
            }
        }

        Debug.Log($"[Enemy] 오염 카드 {placed}개 배치");
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void AdvanceIntent()
    {
        if (_data?.intentPattern == null || _data.intentPattern.Count == 0) return;
        RefreshIntent();
    }

    // ── 내부 ───────────────────────────────────────────────

    private void ApplyMotionPrefab(SpumEnemyMotionPlayer motionPrefab)
    {
        if (_spawnedMotionPlayer != null)
        {
            Destroy(_spawnedMotionPlayer.gameObject);
            _spawnedMotionPlayer = null;
        }

        if (motionPrefab == null)
        {
            SetFallbackMotionActive(true);
            _motionTarget = _fallbackMotionTarget;
            _motionPlayer = _fallbackMotionPlayer;
            return;
        }

        SetFallbackMotionActive(false);

        Transform parent = _motionRoot != null ? _motionRoot : transform;
        _spawnedMotionPlayer = Instantiate(motionPrefab, parent, false);
        _spawnedMotionPlayer.transform.localPosition = _motionSpawnOffset;
        _spawnedMotionPlayer.transform.localRotation = Quaternion.identity;
        _spawnedMotionPlayer.transform.localScale = motionPrefab.transform.localScale;

        _motionPlayer = _spawnedMotionPlayer;
        _motionTarget = _spawnedMotionPlayer.GetComponent<BattleActorMotionTarget>();
        if (_motionTarget == null)
            _motionTarget = _spawnedMotionPlayer.GetComponentInChildren<BattleActorMotionTarget>(true);
    }

    private void SetFallbackMotionActive(bool active)
    {
        if (_fallbackMotionPlayer == null || _fallbackMotionPlayer.gameObject == gameObject) return;
        _fallbackMotionPlayer.gameObject.SetActive(active);
    }

    private void RefreshIntent()
    {
        if (_data?.intentPattern == null || _data.intentPattern.Count == 0)
        {
            CurrentIntentTurn = null;
            OnIntentChanged?.Invoke(null);
            return;
        }

        bool isTutorial = GameManager.Instance != null
            && GameManager.Instance.CurrentState == GameManager.GameState.FirstRunTutorial;

        if (isTutorial)
            _intentIndex = (_intentIndex + 1) % _data.intentPattern.Count;
        else
            _intentIndex = UnityEngine.Random.Range(0, _data.intentPattern.Count);

        CurrentIntentTurn = _data.intentPattern[_intentIndex];
        OnIntentChanged?.Invoke(CurrentIntentTurn);
    }

    private void HandleDied() => OnDied?.Invoke();

    private void OnDestroy()
    {
        if (_view != null)
            _view.OnDied -= HandleDied;

        OnDied = null;
        OnIntentChanged = null;
    }
}
