using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMotionQueuePlayer : MonoBehaviour
{
    public enum QueuedEffectTiming
    {
        OnActivated,
        OnTurnEnd,
        OnTurnStart,
        Direct,
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
        public bool waitForVisualImpact = true;
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
    [SerializeField] private bool _fallbackCardIconWaitsForImpact = true;
    [SerializeField] private int _projectileSortingOrder = 100;

    private readonly Queue<QueuedEffectRequest> _effectQueue = new();
    private readonly List<GameObject> _spawnedVisuals = new();
    private Coroutine _playRoutine;

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

    public void EnqueueEffect(QueuedEffectRequest request)
    {
        _effectQueue.Enqueue(request);

        if (_playRoutine == null && isActiveAndEnabled)
            _playRoutine = StartCoroutine(PlayQueuedEffects());
    }

    public void CancelQueuedEffects()
    {
        _effectQueue.Clear();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

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
            QueuedEffectRequest request = _effectQueue.Dequeue();
            yield return PlayEffectRequest(request);
        }

        _playRoutine = null;
    }

    private IEnumerator PlayEffectRequest(QueuedEffectRequest request)
    {
        CardEffectVisualSetting setting = FindVisualSetting(request.CardData, request.EffectType);
        bool hasVisual = HasVisual(request, setting);
        bool waitForImpact = setting != null
            ? setting.waitForVisualImpact
            : _fallbackCardIconWaitsForImpact;

        if (hasVisual && waitForImpact)
        {
            yield return PlayVisual(request, setting);
            request.OnImpact?.Invoke();
            yield break;
        }

        request.OnImpact?.Invoke();

        if (hasVisual)
            yield return PlayVisual(request, setting);
    }

    private IEnumerator PlayVisual(QueuedEffectRequest request, CardEffectVisualSetting setting)
    {
        if (!TryCreateVisual(request, setting, out GameObject visualObject))
            yield break;

        if (!TryGetStartPosition(request, setting, out Vector3 startPosition) ||
            !TryGetTargetPosition(request, setting, out Vector3 targetPosition))
        {
            DestroyVisual(visualObject);
            yield break;
        }

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
        out GameObject visualObject)
    {
        visualObject = null;

        if (setting != null && setting.vfxPrefab != null)
        {
            visualObject = Instantiate(setting.vfxPrefab);
            ApplyVisualScale(visualObject, GetSize(setting));
            _spawnedVisuals.Add(visualObject);
            return true;
        }

        Sprite projectileSprite = GetProjectileSprite(request, setting);
        if (projectileSprite == null)
            return false;

        visualObject = new GameObject($"CardEffectProjectile_{request.EffectType}");
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
        Transform source = request.SourceCard != null ? request.SourceCard.transform : transform;
        Vector2 offset = setting != null ? setting.spawnOffset : Vector2.zero;
        position = GetOffsetWorldPosition(source, offset);
        return source != null;
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

    private Sprite GetProjectileSprite(QueuedEffectRequest request, CardEffectVisualSetting setting)
    {
        if (setting != null && setting.projectileSprite != null)
            return setting.projectileSprite;

        return request.CardData != null ? request.CardData.icon : null;
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
