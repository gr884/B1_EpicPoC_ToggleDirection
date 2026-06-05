using System.Collections;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;

public class CharacterMotionQueuePlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator _animator;

    [Header("State Names")]
    [SerializeField] private string _idleStateName = "Idle";
    [SerializeField] private string _runStateName = "Run";
    [SerializeField] private string _attackStateName = "Attack";
    [SerializeField] private string _defendStateName = "Defend";
    [SerializeField] private string _hurtStateName = "Hurt";

    [Header("Attack Movement")]
    [SerializeField] private float _runSpeed = 8f;
    [SerializeField] private float _attackStopOffsetX = 0.8f;
    [SerializeField] private float _attackTargetYOffset = 0f;
    [SerializeField] private float _returnDuration = 0.12f;
    [SerializeField] private float _arrivalSnapDistance = 0.02f;

    [Header("Fallback Durations")]
    [SerializeField] private float _attackDuration = 0.75f;
    [SerializeField] private float _defendDuration = 0.75f;
    [SerializeField] private float _hurtDuration = 0.5f;
    [SerializeField] private float _minimumMotionDuration = 0.05f;

    [Header("Damage Number")]
    [SerializeField] private DamageNumber _enemyDamageNumberPrefab;
    [SerializeField] private Vector3 _enemyDamageNumberOffset = new(0f, 1f, 0f);
    [SerializeField, Min(0f)] private float _enemyDamageNumberScale = 0.5f;

    private readonly Queue<CharacterMotionRequest> _motionQueue = new();
    private Coroutine _playRoutine;
    private CharacterMotionRequest _activeAttackRequest;
    private bool _hasActiveAttackRequest;
    private bool _attackImpactApplied;
    private Vector3 _restPosition;

    private int _idleStateHash;
    private int _runStateHash;
    private int _attackStateHash;
    private int _defendStateHash;
    private int _hurtStateHash;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        _restPosition = transform.position;
        RefreshHashes();
    }

    private void OnEnable()
    {
        CharacterMotionEvents.CardMotionRequested += EnqueueMotion;
        PlayIdle();
    }

    private void OnDisable()
    {
        CharacterMotionEvents.CardMotionRequested -= EnqueueMotion;
        _motionQueue.Clear();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (_animator != null)
            _animator.enabled = true;
    }

    public void CancelQueuedMotions()
    {
        _motionQueue.Clear();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _activeAttackRequest = default;
        _hasActiveAttackRequest = false;
        _attackImpactApplied = false;

        transform.position = _restPosition;

        if (_animator != null)
            _animator.enabled = true;

        PlayIdle();
    }

    private void OnValidate()
    {
        RefreshHashes();
    }

    public IEnumerator PlayAttackRoutine(int damageAmount)
    {
        if (damageAmount <= 0)
            yield break;

        while (_playRoutine != null)
            yield return null;

        CharacterMotionRequest request = new CharacterMotionRequest(
            CharacterMotionType.Attack,
            null,
            null,
            EffectType.Damage,
            Vector2Int.zero,
            damageAmount);

        yield return PlayAttackChain(request);
    }

    private void EnqueueMotion(CharacterMotionRequest request)
    {
        if (request.MotionType == CharacterMotionType.Idle)
            return;

        _motionQueue.Enqueue(request);

        if (_playRoutine == null && isActiveAndEnabled)
            _playRoutine = StartCoroutine(PlayQueuedMotions());
    }

    private IEnumerator PlayQueuedMotions()
    {
        while (_motionQueue.Count > 0)
        {
            CharacterMotionRequest request = _motionQueue.Dequeue();

            if (request.MotionType == CharacterMotionType.Attack)
            {
                yield return PlayAttackChain(request);
                yield return null;
                continue;
            }

            PlayMotion(request.MotionType);

            float duration = GetMotionDuration(request.MotionType);
            if (duration > 0f)
                yield return new WaitForSeconds(duration);

            PlayIdle();
            yield return null;
        }

        _playRoutine = null;
    }

    private IEnumerator PlayAttackChain(CharacterMotionRequest firstRequest)
    {
        Vector3 basePosition = transform.position;
        _restPosition = basePosition;

        if (TryGetAttackTargetPosition(out Vector3 attackPosition))
        {
            PlayRun();

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

        yield return PlayAttackOnce(firstRequest);

        while (true)
        {
            yield return null;

            if (_motionQueue.Count <= 0 || _motionQueue.Peek().MotionType != CharacterMotionType.Attack)
                break;

            CharacterMotionRequest nextAttackRequest = _motionQueue.Dequeue();
            yield return PlayAttackOnce(nextAttackRequest);
        }

        yield return ReturnToBasePosition(basePosition);
        PlayIdle();
    }

    private IEnumerator PlayAttackOnce(CharacterMotionRequest request)
    {
        _activeAttackRequest = request;
        _hasActiveAttackRequest = true;
        _attackImpactApplied = false;

        PlayMotion(CharacterMotionType.Attack);

        float attackDuration = GetMotionDuration(CharacterMotionType.Attack);
        if (attackDuration > 0f)
            yield return new WaitForSeconds(attackDuration);

        ApplyAttackImpact();
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

    public void OnAttackImpact()
    {
        ApplyAttackImpact();
    }

    private void ApplyAttackImpact()
    {
        if (!_hasActiveAttackRequest || _attackImpactApplied)
            return;

        _attackImpactApplied = true;

        if (_activeAttackRequest.DamageAmount > 0 && BattleManager.Instance != null)
        {
            int dealtDamage = BattleManager.Instance.DealDamageToEnemy(_activeAttackRequest.DamageAmount);
            ShowEnemyDamageNumber(dealtDamage);
        }

        _activeAttackRequest = default;
        _hasActiveAttackRequest = false;
    }

    private void ShowEnemyDamageNumber(int damageAmount)
    {
        if (_enemyDamageNumberPrefab == null || damageAmount <= 0)
            return;

        Enemy enemy = BattleManager.Instance != null ? BattleManager.Instance.Enemy : null;
        Transform target = enemy != null ? enemy.ViewTransform : null;
        Vector3 position = (target != null ? target.position : transform.position) + _enemyDamageNumberOffset;

        DamageNumber damageNumber = _enemyDamageNumberPrefab.Spawn(position, damageAmount);
        if (damageNumber != null)
            damageNumber.transform.localScale = Vector3.one * _enemyDamageNumberScale;
    }

    private bool TryGetAttackTargetPosition(out Vector3 attackPosition)
    {
        attackPosition = transform.position;

        BattleManager battleManager = BattleManager.Instance;
        Enemy enemy = battleManager != null ? battleManager.Enemy : null;
        Transform target = enemy != null ? enemy.transform : null;
        if (target == null) return false;

        Vector3 targetPosition = target.position;
        attackPosition = new Vector3(
            targetPosition.x - _attackStopOffsetX,
            targetPosition.y + _attackTargetYOffset,
            transform.position.z);
        return true;
    }

    private void PlayMotion(CharacterMotionType motionType)
    {
        if (_animator == null) return;

        int stateHash = motionType switch
        {
            CharacterMotionType.Attack => _attackStateHash,
            CharacterMotionType.Defend => _defendStateHash,
            CharacterMotionType.Hurt => _hurtStateHash,
            _ => _idleStateHash
        };

        _animator.Play(stateHash, 0, 0f);
    }

    private void PlayRun()
    {
        if (_animator == null) return;
        _animator.Play(_runStateHash, 0, 0f);
    }

    private void PlayIdle()
    {
        if (_animator == null) return;
        _animator.Play(_idleStateHash, 0, 0f);
    }

    private float GetMotionDuration(CharacterMotionType motionType)
    {
        string clipName = motionType switch
        {
            CharacterMotionType.Attack => "Knight_Attack",
            CharacterMotionType.Defend => "Knight_Defend",
            CharacterMotionType.Hurt => "Knight_Hurt",
            _ => "Knight_Idle"
        };

        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == clipName)
                    return Mathf.Max(_minimumMotionDuration, clip.length);
            }
        }

        float fallback = motionType switch
        {
            CharacterMotionType.Attack => _attackDuration,
            CharacterMotionType.Defend => _defendDuration,
            CharacterMotionType.Hurt => _hurtDuration,
            _ => 0f
        };

        return Mathf.Max(_minimumMotionDuration, fallback);
    }

    private void RefreshHashes()
    {
        _idleStateHash = Animator.StringToHash(_idleStateName);
        _runStateHash = Animator.StringToHash(_runStateName);
        _attackStateHash = Animator.StringToHash(_attackStateName);
        _defendStateHash = Animator.StringToHash(_defendStateName);
        _hurtStateHash = Animator.StringToHash(_hurtStateName);
    }
}
