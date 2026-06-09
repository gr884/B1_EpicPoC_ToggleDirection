using System;
using System.Collections;
using UnityEngine;

public class EnemyCharacterMotionPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator _animator;

    [Header("State Names")]
    [SerializeField] private string _idleStateName = "Idle";
    [SerializeField] private string _attackStateName = "Attack";
    [SerializeField] private string _hitStateName = "Hit";
    [SerializeField] private string _dieStateName = "Die";

    [Header("Fallback Durations")]
    [SerializeField] private float _attackDuration = 0.84f;
    [SerializeField] private float _hitDuration = 0.42f;
    [SerializeField] private float _dieDuration = 1.25f;
    [SerializeField] private float _minimumMotionDuration = 0.05f;

    private Coroutine _hitRoutine;
    private Coroutine _dieRoutine;
    private bool _isDying;

    private int _idleStateHash;
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

        for (int i = 0; i < attackCount; i++)
        {
            yield return PlayAttackOnce();
            onAttackFinished?.Invoke();
        }

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
        _attackStateHash = Animator.StringToHash(_attackStateName);
        _hitStateHash = Animator.StringToHash(_hitStateName);
        _dieStateHash = Animator.StringToHash(_dieStateName);
    }
}