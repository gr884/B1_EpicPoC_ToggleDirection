using System;
using System.Collections;
using UnityEngine;

public class EnemyCharacterMotionPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator _animator;

    [Header("State Names")]
    [SerializeField] private string _idleStateName = "Idle";
    [SerializeField] private string _runStateName = "Run";
    [SerializeField] private string _attackStateName = "Attack";
    [SerializeField] private string _hitStateName = "Hit";
    [SerializeField] private string _dieStateName = "Die";

    [Header("Attack Movement")]
    [SerializeField] private float _runSpeed = 7f;
    [SerializeField] private float _attackStopOffsetX = 0.8f;
    [SerializeField] private float _attackTargetYOffset = 0f;
    [SerializeField] private float _returnDuration = 0.12f;
    [SerializeField] private float _arrivalSnapDistance = 0.02f;

    [Header("Fallback Durations")]
    [SerializeField] private float _attackDuration = 0.84f;
    [SerializeField] private float _hitDuration = 0.42f;
    [SerializeField] private float _dieDuration = 1.25f;
    [SerializeField] private float _minimumMotionDuration = 0.05f;

    private Coroutine _hitRoutine;
    private Coroutine _dieRoutine;
    private bool _isDying;

    private int _idleStateHash;
    private int _runStateHash;
    private int _attackStateHash;
    private int _hitStateHash;
    private int _dieStateHash;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        RefreshHashes();
    }

    private void OnEnable()
    {
        if (!_isDying)
            PlayIdle();
    }

    private void OnDisable()
    {
        if (_hitRoutine != null)
        {
            StopCoroutine(_hitRoutine);
            _hitRoutine = null;
        }

        if (_dieRoutine != null)
        {
            StopCoroutine(_dieRoutine);
            _dieRoutine = null;
        }

        if (_animator != null)
            _animator.enabled = true;
    }

    private void OnValidate()
    {
        RefreshHashes();
    }

    public IEnumerator PlayAttackRoutine(int hitCount = 1)
    {
        yield return PlayAttackRoutine(hitCount, null);
    }

    public IEnumerator PlayAttackRoutine(int hitCount, Action onAttackFinished)
    {
        if (_isDying) yield break;

        int attackCount = Mathf.Max(1, hitCount);
        Vector3 basePosition = transform.position;

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

        for (int i = 0; i < attackCount; i++)
        {
            yield return PlayAttackOnce();
            onAttackFinished?.Invoke();
        }

        yield return ReturnToBasePosition(basePosition);

        if (!_isDying)
            PlayIdle();
    }

    private IEnumerator PlayAttackOnce()
    {
        PlayState(_attackStateHash);

        float attackDuration = GetMotionDuration("Mushroom_Attack", _attackDuration);
        if (attackDuration > 0f)
            yield return new WaitForSeconds(attackDuration);
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

    public void PlayHit()
    {
        if (_isDying || !isActiveAndEnabled) return;

        if (_hitRoutine != null)
            StopCoroutine(_hitRoutine);

        _hitRoutine = StartCoroutine(PlayHitRoutine());
    }

    public void PlayDie()
    {
        if (_isDying) return;

        _isDying = true;

        if (_hitRoutine != null)
        {
            StopCoroutine(_hitRoutine);
            _hitRoutine = null;
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

    private IEnumerator PlayHitRoutine()
    {
        PlayState(_hitStateHash);

        float hitDuration = GetMotionDuration("Mushroom_Hit", _hitDuration);
        if (hitDuration > 0f)
            yield return new WaitForSeconds(hitDuration);

        if (!_isDying)
            PlayIdle();

        _hitRoutine = null;
    }

    private IEnumerator PlayDieRoutine()
    {
        PlayState(_dieStateHash);

        float dieDuration = GetMotionDuration("Mushroom_Die", _dieDuration);
        if (dieDuration > 0f)
            yield return new WaitForSeconds(dieDuration);

        _dieRoutine = null;
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

    private void PlayRun()
    {
        PlayState(_runStateHash);
    }

    private void PlayIdle()
    {
        PlayState(_idleStateHash);
    }

    private void PlayState(int stateHash)
    {
        if (_animator == null) return;
        _animator.Play(stateHash, 0, 0f);
    }

    private float GetMotionDuration(string clipName, float fallback)
    {
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == clipName)
                    return Mathf.Max(_minimumMotionDuration, clip.length);
            }
        }

        return Mathf.Max(_minimumMotionDuration, fallback);
    }

    private void RefreshHashes()
    {
        _idleStateHash = Animator.StringToHash(_idleStateName);
        _runStateHash = Animator.StringToHash(_runStateName);
        _attackStateHash = Animator.StringToHash(_attackStateName);
        _hitStateHash = Animator.StringToHash(_hitStateName);
        _dieStateHash = Animator.StringToHash(_dieStateName);
    }
}