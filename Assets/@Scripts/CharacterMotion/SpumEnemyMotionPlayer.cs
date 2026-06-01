using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpumEnemyMotionPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SPUM_Prefabs _spumPrefab;
    [SerializeField] private Animator _animator;

    [Header("Animation Indices")]
    [SerializeField] private int _idleIndex = 0;
    [SerializeField] private int _moveIndex = 0;
    [SerializeField] private int _attackIndex = 0;
    [SerializeField] private int _damagedIndex = 0;
    [SerializeField] private int _deathIndex = 0;

    [Header("Attack Movement")]
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _attackStopOffsetX = 0.8f;
    [SerializeField] private float _attackTargetYOffset = 0f;
    [SerializeField] private float _returnDuration = 0.12f;
    [SerializeField] private float _arrivalSnapDistance = 0.02f;

    [Header("Fallback Durations")]
    [SerializeField] private float _attackDuration = 0.42f;
    [SerializeField] private float _hitDuration = 0.34f;
    [SerializeField] private float _dieDuration = 0.67f;
    [SerializeField] private float _minimumMotionDuration = 0.05f;

    private readonly Queue<SpumEnemyMotionRequest> _queue = new();
    private Coroutine _queueRoutine;
    private Coroutine _dieRoutine;
    private SpumEnemyMotionRequest _currentRequest;
    private SpumEnemyMotionRequest _activeAttackRequest;
    private bool _attackImpactApplied;
    private bool _isDying;
    private bool _initialized;

    private void Awake()
    {
        InitializeSpum();
    }

    private void OnEnable()
    {
        InitializeSpum();

        if (!_isDying)
            PlayIdle();
    }

    private void OnDisable()
    {
        ClearQueue(complete: true);

        if (_queueRoutine != null)
        {
            StopCoroutine(_queueRoutine);
            _queueRoutine = null;
        }

        if (_dieRoutine != null)
        {
            StopCoroutine(_dieRoutine);
            _dieRoutine = null;
        }

        _currentRequest = null;
        _activeAttackRequest = null;

        if (_animator != null)
            _animator.enabled = true;
    }

    public IEnumerator PlayAttackRoutine(int hitCount = 1)
    {
        yield return PlayAttackRoutine(hitCount, null);
    }

    public IEnumerator PlayAttackRoutine(int hitCount, Action onAttackImpact)
    {
        if (_isDying) yield break;

        SpumEnemyMotionRequest request = Enqueue(
            new SpumEnemyMotionRequest(SpumEnemyMotionType.Attack, hitCount, onAttackImpact));

        while (request != null && !request.IsComplete)
            yield return null;
    }

    public void PlayHit()
    {
        if (_isDying || !isActiveAndEnabled) return;

        Enqueue(new SpumEnemyMotionRequest(SpumEnemyMotionType.Hit));
    }

    public void PlayDie()
    {
        if (_isDying) return;

        _isDying = true;
        ClearQueue(complete: true);

        if (_queueRoutine != null)
        {
            StopCoroutine(_queueRoutine);
            _queueRoutine = null;
        }

        if (isActiveAndEnabled)
            _dieRoutine = StartCoroutine(PlayDieRoutine());
    }

    public IEnumerator WaitForDieMotion()
    {
        if (!_isDying)
            PlayDie();

        while (_dieRoutine != null)
            yield return null;
    }

    public void OnAttackImpact()
    {
        ApplyAttackImpact();
    }

    private SpumEnemyMotionRequest Enqueue(SpumEnemyMotionRequest request)
    {
        _queue.Enqueue(request);

        if (_queueRoutine == null && isActiveAndEnabled)
            _queueRoutine = StartCoroutine(ProcessQueue());

        return request;
    }

    private IEnumerator ProcessQueue()
    {
        while (_queue.Count > 0 && !_isDying)
        {
            _currentRequest = _queue.Dequeue();

            switch (_currentRequest.MotionType)
            {
                case SpumEnemyMotionType.Attack:
                    yield return PlayAttackChain(_currentRequest);
                    break;
                case SpumEnemyMotionType.Hit:
                    yield return PlayHitRoutine();
                    break;
            }

            _currentRequest.Complete();
            _currentRequest = null;
        }

        _queueRoutine = null;
    }

    private IEnumerator PlayAttackChain(SpumEnemyMotionRequest request)
    {
        Vector3 basePosition = transform.position;

        if (TryGetAttackTargetPosition(out Vector3 attackPosition))
        {
            PlayMove();
            FacePosition(attackPosition);

            while (Vector3.Distance(transform.position, attackPosition) > _arrivalSnapDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    attackPosition,
                    Mathf.Max(0.01f, _runSpeed) * Time.deltaTime);
                yield return null;
            }

            transform.position = attackPosition;
        }

        for (int i = 0; i < request.HitCount && !_isDying; i++)
            yield return PlayAttackOnce(request);

        if (!_isDying)
        {
            yield return ReturnToBasePosition(basePosition);
            FacePlayer();
            PlayIdle();
        }
    }

    private IEnumerator PlayAttackOnce(SpumEnemyMotionRequest request)
    {
        _activeAttackRequest = request;
        _attackImpactApplied = false;

        PlaySpum(PlayerState.ATTACK, _attackIndex);

        float attackDuration = GetMotionDuration(PlayerState.ATTACK, _attackIndex, _attackDuration);
        if (attackDuration > 0f)
            yield return new WaitForSeconds(attackDuration);

        ApplyAttackImpact();
        _activeAttackRequest = null;
    }

    private IEnumerator ReturnToBasePosition(Vector3 basePosition)
    {
        bool disabledAnimator = false;
        if (_animator != null && _animator.enabled)
        {
            _animator.enabled = false;
            disabledAnimator = true;
        }

        if (_returnDuration > 0f)
        {
            Vector3 returnStart = transform.position;
            float elapsed = 0f;

            while (elapsed < _returnDuration)
            {
                float t = elapsed / _returnDuration;
                transform.position = Vector3.Lerp(returnStart, basePosition, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        transform.position = basePosition;

        if (disabledAnimator)
            _animator.enabled = true;
    }

    private IEnumerator PlayHitRoutine()
    {
        PlaySpum(PlayerState.DAMAGED, _damagedIndex);

        float hitDuration = GetMotionDuration(PlayerState.DAMAGED, _damagedIndex, _hitDuration);
        if (hitDuration > 0f)
            yield return new WaitForSeconds(hitDuration);

        if (!_isDying)
            PlayIdle();
    }

    private IEnumerator PlayDieRoutine()
    {
        PlaySpum(PlayerState.DEATH, _deathIndex);

        float dieDuration = GetMotionDuration(PlayerState.DEATH, _deathIndex, _dieDuration);
        if (dieDuration > 0f)
            yield return new WaitForSeconds(dieDuration);

        _dieRoutine = null;
    }

    private void ApplyAttackImpact()
    {
        if (_activeAttackRequest == null || _attackImpactApplied) return;

        _attackImpactApplied = true;
        _activeAttackRequest.OnAttackImpact?.Invoke();
    }

    private bool TryGetAttackTargetPosition(out Vector3 attackPosition)
    {
        attackPosition = transform.position;

        BattleManager battleManager = BattleManager.Instance;
        Player player = battleManager != null ? battleManager.Player : null;
        Transform target = player != null ? player.transform : null;
        if (target == null) return false;

        Vector3 targetPosition = target.position;
        float side = Mathf.Approximately(transform.position.x, targetPosition.x)
            ? 1f
            : Mathf.Sign(transform.position.x - targetPosition.x);

        attackPosition = new Vector3(
            targetPosition.x + side * _attackStopOffsetX,
            targetPosition.y + _attackTargetYOffset,
            transform.position.z);
        return true;
    }

    private void PlayIdle()
    {
        PlaySpum(PlayerState.IDLE, _idleIndex);
    }

    private void PlayMove()
    {
        PlaySpum(PlayerState.MOVE, _moveIndex);
    }

    private void PlaySpum(PlayerState state, int index)
    {
        InitializeSpum();

        if (_spumPrefab == null) return;

        int safeIndex = GetSafeIndex(state, index);
        if (safeIndex < 0) return;

        _spumPrefab.PlayAnimation(state, safeIndex);
    }

    private int GetSafeIndex(PlayerState state, int index)
    {
        if (_spumPrefab == null || _spumPrefab.StateAnimationPairs == null)
            return -1;

        string stateName = state.ToString();
        if (!_spumPrefab.StateAnimationPairs.TryGetValue(stateName, out List<AnimationClip> clips)
            || clips == null
            || clips.Count == 0)
            return -1;

        return Mathf.Clamp(index, 0, clips.Count - 1);
    }

    private float GetMotionDuration(PlayerState state, int index, float fallback)
    {
        if (_spumPrefab != null && _spumPrefab.StateAnimationPairs != null)
        {
            string stateName = state.ToString();
            if (_spumPrefab.StateAnimationPairs.TryGetValue(stateName, out List<AnimationClip> clips)
                && clips != null
                && clips.Count > 0)
            {
                int safeIndex = Mathf.Clamp(index, 0, clips.Count - 1);
                AnimationClip clip = clips[safeIndex];
                if (clip != null)
                    return Mathf.Max(_minimumMotionDuration, clip.length);
            }
        }

        return Mathf.Max(_minimumMotionDuration, fallback);
    }

    private void InitializeSpum()
    {
        if (_initialized) return;

        if (_spumPrefab == null)
            _spumPrefab = GetComponentInChildren<SPUM_Prefabs>(true);

        if (_animator == null)
            _animator = _spumPrefab != null && _spumPrefab._anim != null
                ? _spumPrefab._anim
                : GetComponentInChildren<Animator>(true);

        if (_spumPrefab == null || _animator == null) return;

        _spumPrefab._anim = _animator;

        if (!_spumPrefab.allListsHaveItemsExist())
            _spumPrefab.PopulateAnimationLists();

        _spumPrefab.OverrideControllerInit();
        _initialized = true;
    }

    private void FacePlayer()
    {
        BattleManager battleManager = BattleManager.Instance;
        Player player = battleManager != null ? battleManager.Player : null;
        if (player != null)
            FacePosition(player.transform.position);
    }

    private void FacePosition(Vector3 targetPosition)
    {
        if (_spumPrefab == null) return;

        float deltaX = targetPosition.x - transform.position.x;
        if (Mathf.Abs(deltaX) < 0.001f) return;

        Vector3 scale = _spumPrefab.transform.localScale;
        scale.x = deltaX > 0f ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        _spumPrefab.transform.localScale = scale;
    }

    private void ClearQueue(bool complete)
    {
        while (_queue.Count > 0)
        {
            SpumEnemyMotionRequest request = _queue.Dequeue();
            if (complete)
                request.Complete();
        }

        if (complete)
            _currentRequest?.Complete();

        _activeAttackRequest = null;
        _attackImpactApplied = false;
    }
}
