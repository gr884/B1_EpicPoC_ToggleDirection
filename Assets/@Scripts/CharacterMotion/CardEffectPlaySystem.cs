using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardEffectPlaySystem : MonoBehaviour
{
    public enum QueuedEffectTiming
    {
        OnActivated,
        OnTurnEnd,
        OnTurnStart,
        Direct,
    }

    public enum VisualMovementMode
    {
        Auto,
        LinearTransform,
        RigidbodyProjectile,
    }

    [Serializable]
    public sealed class CardEffectVisualSetting
    {
        public CardData card;
        public bool matchAnyEffectType;
        public EffectType effectType = EffectType.Damage;
        public Sprite projectileSprite;
        public GameObject vfxPrefab;
        public Transform targetOverride;
        public Vector2 spawnOffset;
        public Vector2 targetOffset;
        public Vector2 size = Vector2.one;
        [Min(0f)] public float duration = 0.28f;
        public VisualMovementMode movementMode = VisualMovementMode.Auto;
        [Min(0f)] public float projectileForce = 1000f;
        [Min(0f)] public float forwardSpawnOffset = 0.3f;
        [Min(0f)] public float impactDistance = 0.35f;
        [Min(0f)] public float maxFlightDuration = 5f;
    }

    public readonly struct QueuedEffectRequest
    {
        public QueuedEffectRequest(
            CardView sourceCard,
            CardData cardData,
            EffectType effectType,
            QueuedEffectTiming timing,
            Action onImpact = null,
            Transform targetOverride = null,
            float value = 0f)
        {
            SourceCard = sourceCard;
            CardData = cardData;
            EffectType = effectType;
            Timing = timing;
            OnImpact = onImpact;
            TargetOverride = targetOverride;
            Value = value;
        }

        public CardView SourceCard { get; }
        public CardData CardData { get; }
        public EffectType EffectType { get; }
        public QueuedEffectTiming Timing { get; }
        public Action OnImpact { get; }
        public Transform TargetOverride { get; }
        public float Value { get; }
    }

    [Header("Effect Visuals")]
    [SerializeField] private List<CardEffectVisualSetting> _visualSettings = new();
    [SerializeField] private Transform _defaultTarget;
    [SerializeField] private Vector2 _defaultProjectileSize = Vector2.one;
    [SerializeField, Min(0f)] private float _defaultProjectileDuration = 0.28f;
    [SerializeField] private int _projectileSortingOrder = 100;

    private const float DefaultProjectileForce = 1000f;
    private const float DefaultForwardSpawnOffset = 0.3f;
    private const float DefaultImpactDistance = 0.35f;
    private const float DefaultMaxFlightDuration = 5f;

    private sealed class QueuedEffectHandle
    {
        public QueuedEffectHandle(QueuedEffectRequest request, Action<bool> onComplete)
        {
            Request = request;
            OnComplete = onComplete;
        }

        public QueuedEffectRequest Request { get; }
        public Action<bool> OnComplete { get; }
    }

    private readonly Queue<QueuedEffectHandle> _effectQueue = new();
    private readonly List<GameObject> _spawnedVisuals = new();
    private Coroutine _playRoutine;
    private Action<bool> _activeCompletion;

    private void OnDisable()
    {
        CancelQueuedEffects();
    }

    public void EnqueueActivatedEffect(
        CardView sourceCard,
        EffectType effectType,
        Action onImpact = null,
        Transform targetOverride = null,
        float value = 0f)
    {
        EnqueueEffect(sourceCard, effectType, QueuedEffectTiming.OnActivated, onImpact, targetOverride, value);
    }

    public void EnqueueTurnEndEffect(
        CardView sourceCard,
        EffectType effectType,
        Action onImpact = null,
        Transform targetOverride = null,
        float value = 0f)
    {
        EnqueueEffect(sourceCard, effectType, QueuedEffectTiming.OnTurnEnd, onImpact, targetOverride, value);
    }

    public void EnqueueTurnStartEffect(
        CardView sourceCard,
        EffectType effectType,
        Action onImpact = null,
        Transform targetOverride = null,
        float value = 0f)
    {
        EnqueueEffect(sourceCard, effectType, QueuedEffectTiming.OnTurnStart, onImpact, targetOverride, value);
    }

    public void EnqueueDirectEffect(
        CardView sourceCard,
        EffectType effectType,
        Action onImpact = null,
        Transform targetOverride = null,
        float value = 0f)
    {
        EnqueueEffect(sourceCard, effectType, QueuedEffectTiming.Direct, onImpact, targetOverride, value);
    }

    public void PlayAppliedEffects(CardView sourceCard, IEnumerable<EffectType> effectTypes)
    {
        if (sourceCard == null || effectTypes == null)
            return;

        HashSet<EffectType> requestedTypes = effectTypes as HashSet<EffectType> ?? new HashSet<EffectType>(effectTypes);

        if (requestedTypes.Contains(EffectType.Damage))
            EnqueueDirectEffect(sourceCard, EffectType.Damage);

        if (requestedTypes.Contains(EffectType.Defense))
            EnqueueDirectEffect(sourceCard, EffectType.Defense);
    }

    public bool HasAssignedVisual(CardView sourceCard, EffectType effectType)
    {
        CardData cardData = sourceCard != null ? sourceCard.Data : null;
        CardEffectVisualSetting setting = FindVisualSetting(cardData, effectType);
        return HasAssignedVisual(setting);
    }

    public IEnumerator PlayAssignedEffectAndWait(
        CardView sourceCard,
        EffectType effectType,
        QueuedEffectTiming timing,
        Action onImpact = null,
        Transform targetOverride = null,
        float value = 0f,
        Action<bool> onComplete = null)
    {
        if (!HasAssignedVisual(sourceCard, effectType))
        {
            onImpact?.Invoke();
            onComplete?.Invoke(true);
            yield break;
        }

        bool completed = false;
        CardData cardData = sourceCard != null ? sourceCard.Data : null;
        EnqueueEffect(new QueuedEffectRequest(
                sourceCard,
                cardData,
                effectType,
                timing,
                onImpact,
                targetOverride,
                value),
            completedNormally =>
            {
                completed = true;
                onComplete?.Invoke(completedNormally);
            });

        while (!completed && isActiveAndEnabled)
            yield return null;
    }

    public bool PlayAssignedEffectDetached(
        CardView sourceCard,
        EffectType effectType,
        QueuedEffectTiming timing,
        Action onImpact = null,
        Transform targetOverride = null,
        float value = 0f)
    {
        if (!isActiveAndEnabled || !HasAssignedVisual(sourceCard, effectType))
            return false;

        CardData cardData = sourceCard != null ? sourceCard.Data : null;
        QueuedEffectRequest request = new(
            sourceCard,
            cardData,
            effectType,
            timing,
            onImpact,
            targetOverride,
            value);

        // 체인 진행과 무관하게 비주얼만 독립 실행한다.
        StartCoroutine(PlayDetachedEffect(request));
        return true;
    }

    public void EnqueueEffect(QueuedEffectRequest request)
    {
        EnqueueEffect(request, null);
    }

    private void EnqueueEffect(QueuedEffectRequest request, Action<bool> onComplete)
    {
        _effectQueue.Enqueue(new QueuedEffectHandle(request, onComplete));

        if (_playRoutine == null && isActiveAndEnabled)
            _playRoutine = StartCoroutine(PlayQueuedEffects());
    }

    public void CancelQueuedEffects()
    {
        _activeCompletion?.Invoke(false);
        _activeCompletion = null;

        while (_effectQueue.Count > 0)
            _effectQueue.Dequeue().OnComplete?.Invoke(false);

        _effectQueue.Clear();

        StopAllCoroutines();
        _playRoutine = null;

        ClearSpawnedVisuals();
    }

    private void EnqueueEffect(
        CardView sourceCard,
        EffectType effectType,
        QueuedEffectTiming timing,
        Action onImpact,
        Transform targetOverride,
        float value)
    {
        CardData cardData = sourceCard != null ? sourceCard.Data : null;
        EnqueueEffect(new QueuedEffectRequest(
            sourceCard,
            cardData,
            effectType,
            timing,
            onImpact,
            targetOverride,
            value));
    }

    private IEnumerator PlayQueuedEffects()
    {
        while (_effectQueue.Count > 0)
        {
            QueuedEffectHandle handle = _effectQueue.Dequeue();
            _activeCompletion = handle.OnComplete;
            yield return PlayEffectRequest(handle.Request);
            _activeCompletion?.Invoke(true);
            _activeCompletion = null;
        }

        _playRoutine = null;
    }

    private IEnumerator PlayDetachedEffect(QueuedEffectRequest request)
    {
        CardEffectVisualSetting setting = FindVisualSetting(request.CardData, request.EffectType);
        if (HasVisual(request, setting))
            yield return PlayVisual(request, setting);

        request.OnImpact?.Invoke();
    }

    private IEnumerator PlayEffectRequest(QueuedEffectRequest request)
    {
        CardEffectVisualSetting setting = FindVisualSetting(request.CardData, request.EffectType);
        bool hasVisual = HasVisual(request, setting);

        request.OnImpact?.Invoke();

        if (hasVisual)
            yield return PlayVisual(request, setting);
    }

    private IEnumerator PlayVisual(QueuedEffectRequest request, CardEffectVisualSetting setting)
    {
        if (!TryGetStartPosition(request, setting, out Vector3 startPosition) ||
            !TryGetTargetPosition(request, setting, out Vector3 targetPosition))
        {
            yield break;
        }

        Vector3 rawStartPosition = startPosition;
        // 기울어진 UI 그리드의 깊이값이 월드 VFX 시작 위치에 섞이지 않게 한다.
        startPosition.z = targetPosition.z;

        VisualMovementMode movementMode = ResolveMovementMode(setting);
        if (movementMode == VisualMovementMode.RigidbodyProjectile)
        {
            Vector3 direction = targetPosition - startPosition;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                if (TryCreateVisual(request, setting, targetPosition, GetInitialRotation(setting), out GameObject zeroDistanceVisual))
                {
                    LogVisualSpawn(request, zeroDistanceVisual, rawStartPosition, targetPosition, targetPosition);
                    DestroyVisual(zeroDistanceVisual);
                }
                yield break;
            }

            direction.Normalize();
            Vector3 projectileStartPosition = startPosition + direction * GetForwardSpawnOffset(setting);
            Quaternion projectileRotation = Quaternion.LookRotation(direction, Vector3.up);

            if (!TryCreateVisual(request, setting, projectileStartPosition, projectileRotation, out GameObject projectileObject))
                yield break;

            LogVisualSpawn(request, projectileObject, rawStartPosition, projectileStartPosition, targetPosition);
            yield return PlayRigidbodyProjectile(projectileObject, direction, targetPosition, setting);
            yield break;
        }

        if (!TryCreateVisual(request, setting, startPosition, GetInitialRotation(setting), out GameObject visualObject))
            yield break;

        LogVisualSpawn(request, visualObject, rawStartPosition, startPosition, targetPosition);
        yield return PlayLinearVisual(visualObject, startPosition, targetPosition, setting);
    }

    private IEnumerator PlayRigidbodyProjectile(
        GameObject visualObject,
        Vector3 direction,
        Vector3 targetPosition,
        CardEffectVisualSetting setting)
    {
        if (visualObject == null)
            yield break;

        Rigidbody body = visualObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            yield return PlayLinearVisual(visualObject, visualObject.transform.position, targetPosition, setting);
            yield break;
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.AddForce(direction * GetProjectileForce(setting));

        float impactDistance = GetImpactDistance(setting);
        float maxFlightDuration = GetMaxFlightDuration(setting);
        float elapsed = 0f;

        while (visualObject != null && elapsed < maxFlightDuration)
        {
            if (Vector3.Distance(visualObject.transform.position, targetPosition) <= impactDistance)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (visualObject == null)
            yield break;

        if (elapsed >= maxFlightDuration)
        {
            Debug.LogWarning($"[CardEffectPlaySystem] Rigidbody projectile timed out before impact: {visualObject.name}");
            visualObject.transform.position = targetPosition;
        }

        DestroyVisual(visualObject);
    }

    private IEnumerator PlayLinearVisual(
        GameObject visualObject,
        Vector3 startPosition,
        Vector3 targetPosition,
        CardEffectVisualSetting setting)
    {
        if (visualObject == null)
            yield break;

        visualObject.transform.position = startPosition;

        float duration = GetDuration(setting);
        if (duration <= 0f)
        {
            visualObject.transform.position = targetPosition;
            DestroyVisual(visualObject);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && visualObject != null)
        {
            float t = elapsed / duration;
            visualObject.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (visualObject != null)
            visualObject.transform.position = targetPosition;

        DestroyVisual(visualObject);
    }

    private bool TryCreateVisual(
        QueuedEffectRequest request,
        CardEffectVisualSetting setting,
        Vector3 position,
        Quaternion rotation,
        out GameObject visualObject)
    {
        visualObject = null;

        if (setting != null && setting.vfxPrefab != null)
        {
            visualObject = Instantiate(setting.vfxPrefab, position, rotation);
            ApplyVisualScale(visualObject, GetSize(setting));
            _spawnedVisuals.Add(visualObject);
            return true;
        }

        Sprite projectileSprite = GetProjectileSprite(request, setting);
        if (projectileSprite == null)
            return false;

        visualObject = new GameObject($"CardEffectProjectile_{request.EffectType}");
        visualObject.transform.SetPositionAndRotation(position, rotation);
        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.sortingOrder = _projectileSortingOrder;
        ApplyVisualScale(visualObject, GetSize(setting));
        _spawnedVisuals.Add(visualObject);
        return true;
    }

    private bool TryGetStartPosition(
        QueuedEffectRequest request,
        CardEffectVisualSetting setting,
        out Vector3 position)
    {
        Transform source = ResolveSourceTransform(request.SourceCard);
        Vector2 offset = setting != null ? setting.spawnOffset : Vector2.zero;
        position = GetOffsetWorldPosition(source, offset);
        return source != null;
    }

    private Transform ResolveSourceTransform(CardView sourceCard)
    {
        if (sourceCard != null && sourceCard.CurrentSlot != null)
            return sourceCard.CurrentSlot.transform;

        return sourceCard != null ? sourceCard.transform : transform;
    }

    private bool TryGetTargetPosition(
        QueuedEffectRequest request,
        CardEffectVisualSetting setting,
        out Vector3 position)
    {
        Transform target = request.TargetOverride != null
            ? request.TargetOverride
            : setting != null && setting.targetOverride != null
                ? setting.targetOverride
                : _defaultTarget != null
                    ? _defaultTarget
                    : transform;

        Vector2 offset = setting != null ? setting.targetOffset : Vector2.zero;
        position = GetOffsetWorldPosition(target, offset);
        return target != null;
    }

    private CardEffectVisualSetting FindVisualSetting(CardData cardData, EffectType effectType)
    {
        CardEffectVisualSetting cardDefault = null;
        CardEffectVisualSetting effectDefault = null;

        for (int i = 0; i < _visualSettings.Count; i++)
        {
            CardEffectVisualSetting setting = _visualSettings[i];
            if (setting == null)
                continue;

            bool matchesCard = setting.card != null && setting.card == cardData;
            bool matchesEffect = !setting.matchAnyEffectType && setting.effectType == effectType;

            if (matchesCard && matchesEffect)
                return setting;

            if (matchesCard && setting.matchAnyEffectType && cardDefault == null)
                cardDefault = setting;

            if (setting.card == null && matchesEffect && effectDefault == null)
                effectDefault = setting;
        }

        return cardDefault != null ? cardDefault : effectDefault;
    }

    private bool HasVisual(QueuedEffectRequest request, CardEffectVisualSetting setting)
    {
        return (setting != null && setting.vfxPrefab != null)
            || GetProjectileSprite(request, setting) != null;
    }

    private static bool HasAssignedVisual(CardEffectVisualSetting setting)
    {
        return setting != null
            && (setting.vfxPrefab != null || setting.projectileSprite != null);
    }

    private Sprite GetProjectileSprite(QueuedEffectRequest request, CardEffectVisualSetting setting)
    {
        if (setting != null && setting.projectileSprite != null)
            return setting.projectileSprite;

        return request.CardData != null ? request.CardData.icon : null;
    }

    private void LogVisualSpawn(
        QueuedEffectRequest request,
        GameObject visualObject,
        Vector3 rawStartPosition,
        Vector3 startPosition,
        Vector3 targetPosition)
    {
        Debug.Log(
            $"[CardEffectPlaySystem] Spawn visual={visualObject.name}, effect={request.EffectType}, card={(request.CardData != null ? request.CardData.displayName : "None")}, rawStart={rawStartPosition}, start={startPosition}, target={targetPosition}");
    }

    private VisualMovementMode ResolveMovementMode(CardEffectVisualSetting setting)
    {
        VisualMovementMode movementMode = setting != null
            ? setting.movementMode
            : VisualMovementMode.LinearTransform;

        if (movementMode != VisualMovementMode.Auto)
            return movementMode;

        return setting != null && setting.vfxPrefab != null && setting.vfxPrefab.GetComponent<Rigidbody>() != null
            ? VisualMovementMode.RigidbodyProjectile
            : VisualMovementMode.LinearTransform;
    }

    private static Quaternion GetInitialRotation(CardEffectVisualSetting setting)
    {
        return setting != null && setting.vfxPrefab != null
            ? setting.vfxPrefab.transform.rotation
            : Quaternion.identity;
    }

    private float GetDuration(CardEffectVisualSetting setting)
    {
        if (setting != null)
            return Mathf.Max(0f, setting.duration);

        return Mathf.Max(0f, _defaultProjectileDuration);
    }

    private Vector2 GetSize(CardEffectVisualSetting setting)
    {
        if (setting != null)
            return setting.size;

        return _defaultProjectileSize;
    }

    private float GetProjectileForce(CardEffectVisualSetting setting)
    {
        return setting != null && setting.projectileForce > 0f
            ? setting.projectileForce
            : DefaultProjectileForce;
    }

    private float GetForwardSpawnOffset(CardEffectVisualSetting setting)
    {
        return setting != null && setting.forwardSpawnOffset > 0f
            ? setting.forwardSpawnOffset
            : DefaultForwardSpawnOffset;
    }

    private float GetImpactDistance(CardEffectVisualSetting setting)
    {
        return setting != null && setting.impactDistance > 0f
            ? setting.impactDistance
            : DefaultImpactDistance;
    }

    private float GetMaxFlightDuration(CardEffectVisualSetting setting)
    {
        return setting != null && setting.maxFlightDuration > 0f
            ? setting.maxFlightDuration
            : DefaultMaxFlightDuration;
    }

    private void ApplyVisualScale(GameObject visualObject, Vector2 size)
    {
        if (visualObject == null)
            return;

        float x = size.x > 0f ? size.x : 1f;
        float y = size.y > 0f ? size.y : 1f;
        visualObject.transform.localScale = new Vector3(x, y, 1f);
    }

    private static Vector3 GetOffsetWorldPosition(Transform source, Vector2 offset)
    {
        if (source == null)
            return Vector3.zero;

        return source.position + source.right * offset.x + source.up * offset.y;
    }

    private void DestroyVisual(GameObject visualObject)
    {
        if (visualObject == null)
            return;

        _spawnedVisuals.Remove(visualObject);
        Destroy(visualObject);
    }

    private void ClearSpawnedVisuals()
    {
        for (int i = _spawnedVisuals.Count - 1; i >= 0; i--)
        {
            if (_spawnedVisuals[i] != null)
                Destroy(_spawnedVisuals[i]);
        }

        _spawnedVisuals.Clear();
    }
}
