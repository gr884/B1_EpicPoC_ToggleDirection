using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class CardFlightEffectSetting
{
    public EffectType effectType = EffectType.Damage;
    public Transform target;
    public Sprite sprite;
    public Vector2 spawnOffset;
    public Vector2 targetOffset;
    public Vector2 size = new(48f, 48f);
    [Min(0f)] public float duration = 0.28f;
    public Ease ease = Ease.OutQuad;
    public Color color = Color.white;

    public CardFlightEffectSetting()
    {
    }

    public CardFlightEffectSetting(EffectType type)
    {
        effectType = type;
    }
}

public class ActionBlockFlightEffectPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform effectLayer;
    [SerializeField] private Transform enemyTarget;
    [SerializeField] private Transform playerTarget;

    [Header("Settings")]
    [SerializeField] private List<CardFlightEffectSetting> settings = new()
    {
        new CardFlightEffectSetting(EffectType.Damage),
        new CardFlightEffectSetting(EffectType.Defense),
    };

    private readonly List<GameObject> spawnedEffects = new();
    private readonly List<Tween> activeTweens = new();

    private void Reset()
    {
        ResolveSceneRefs(null);
    }

    private void OnDisable()
    {
        ClearActiveEffects();
    }

    public IEnumerator Play(CardView card)
    {
        if (card?.Data?.effects == null)
        {
            yield break;
        }

        if (ContainsEffect(card.Data, EffectType.Damage))
        {
            yield return PlayEffect(card.transform, EffectType.Damage);
        }

        if (ContainsEffect(card.Data, EffectType.Defense))
        {
            yield return PlayEffect(card.transform, EffectType.Defense);
        }
    }

    public IEnumerator Play(CardView card, IEnumerable<EffectType> effectTypes)
    {
        if (card == null || effectTypes == null)
        {
            yield break;
        }

        HashSet<EffectType> requestedTypes = effectTypes as HashSet<EffectType> ?? new HashSet<EffectType>(effectTypes);

        if (requestedTypes.Contains(EffectType.Damage))
        {
            yield return PlayEffect(card.transform, EffectType.Damage);
        }

        if (requestedTypes.Contains(EffectType.Defense))
        {
            yield return PlayEffect(card.transform, EffectType.Defense);
        }
    }

    private IEnumerator PlayEffect(Transform source, EffectType effectType)
    {
        CardFlightEffectSetting setting = FindSetting(effectType);
        if (setting == null || setting.sprite == null)
        {
            yield break;
        }

        Transform target = GetTarget(effectType, setting);
        if (target == null)
        {
            yield break;
        }

        ResolveSceneRefs(source);
        if (rootCanvas == null || effectLayer == null)
        {
            yield break;
        }

        if (!TryGetLocalPoint(source, setting.spawnOffset, out Vector2 startPosition) ||
            !TryGetLocalPoint(target, setting.targetOffset, out Vector2 targetPosition))
        {
            yield break;
        }

        GameObject effectObject = CreateEffectObject(setting, startPosition);
        RectTransform effectRect = effectObject.GetComponent<RectTransform>();
        if (effectRect == null)
        {
            Destroy(effectObject);
            yield break;
        }

        float duration = Mathf.Max(0f, setting.duration);
        if (duration <= 0f)
        {
            effectRect.anchoredPosition = targetPosition;
            DestroyEffectObject(effectObject);
            yield break;
        }

        Tween tween = effectRect.DOAnchorPos(targetPosition, duration).SetEase(setting.ease).SetTarget(effectObject);
        activeTweens.Add(tween);
        yield return tween.WaitForCompletion();
        activeTweens.Remove(tween);

        DestroyEffectObject(effectObject);
    }

    private CardFlightEffectSetting FindSetting(EffectType effectType)
    {
        if (settings == null)
        {
            return null;
        }

        for (int i = 0; i < settings.Count; i++)
        {
            CardFlightEffectSetting setting = settings[i];
            if (setting != null && setting.effectType == effectType)
            {
                return setting;
            }
        }

        return null;
    }

    private Transform GetTarget(EffectType effectType, CardFlightEffectSetting setting)
    {
        if (setting != null && setting.target != null)
        {
            return setting.target;
        }

        return effectType switch
        {
            EffectType.Damage => enemyTarget,
            EffectType.Defense => playerTarget,
            _ => null
        };
    }

    private GameObject CreateEffectObject(CardFlightEffectSetting setting, Vector2 anchoredPosition)
    {
        GameObject obj = new($"ActionBlockFlightEffect_{setting.effectType}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(effectLayer, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(Mathf.Max(1f, setting.size.x), Mathf.Max(1f, setting.size.y));
        rect.localScale = Vector3.one;

        Image image = obj.GetComponent<Image>();
        image.sprite = setting.sprite;
        image.color = setting.color;
        image.preserveAspect = true;
        image.raycastTarget = false;

        spawnedEffects.Add(obj);
        return obj;
    }

    private static bool ContainsEffect(CardData data, EffectType effectType)
    {
        if (data?.effects == null)
        {
            return false;
        }

        for (int i = 0; i < data.effects.Count; i++)
        {
            if (data.effects[i].effectType == effectType)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetLocalPoint(Transform source, Vector2 localOffset, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (source == null || effectLayer == null || rootCanvas == null)
        {
            return false;
        }

        Vector3 worldPoint = GetWorldPoint(source, localOffset);
        Camera sourceCamera = GetSourceCamera(source);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPoint);
        Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(effectLayer, screenPoint, canvasCamera, out localPoint);
    }

    private static Vector3 GetWorldPoint(Transform source, Vector2 localOffset)
    {
        if (source is RectTransform rectTransform)
        {
            return rectTransform.TransformPoint(localOffset);
        }

        return source.position + source.right * localOffset.x + source.up * localOffset.y;
    }

    private static Camera GetSourceCamera(Transform source)
    {
        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        if (sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return sourceCanvas.worldCamera;
        }

        return source is RectTransform ? null : Camera.main;
    }

    private void ResolveSceneRefs(Transform source)
    {
        if (rootCanvas == null)
        {
            rootCanvas = source != null ? source.GetComponentInParent<Canvas>() : null;
            if (rootCanvas == null)
            {
                rootCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            }
        }

        if (effectLayer == null && rootCanvas != null)
        {
            Transform found = rootCanvas.transform.Find("ActionBlockFlightEffectLayer");
            if (found != null)
            {
                effectLayer = found.GetComponent<RectTransform>();
            }

            if (effectLayer == null)
            {
                GameObject layerObject = new("ActionBlockFlightEffectLayer", typeof(RectTransform));
                effectLayer = layerObject.GetComponent<RectTransform>();
                effectLayer.SetParent(rootCanvas.transform, false);
                effectLayer.anchorMin = Vector2.zero;
                effectLayer.anchorMax = Vector2.one;
                effectLayer.offsetMin = Vector2.zero;
                effectLayer.offsetMax = Vector2.zero;
            }

            effectLayer.SetAsLastSibling();
        }

        if (enemyTarget == null)
        {
            Enemy enemy = FindFirstObjectByType<Enemy>(FindObjectsInactive.Include);
            enemyTarget = enemy != null ? enemy.transform : null;
        }

        if (playerTarget == null)
        {
            Player player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
            playerTarget = player != null ? player.transform : null;
        }
    }

    private void DestroyEffectObject(GameObject effectObject)
    {
        if (effectObject == null)
        {
            return;
        }

        spawnedEffects.Remove(effectObject);
        Destroy(effectObject);
    }

    private void ClearActiveEffects()
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            activeTweens[i]?.Kill();
        }

        activeTweens.Clear();

        for (int i = spawnedEffects.Count - 1; i >= 0; i--)
        {
            if (spawnedEffects[i] != null)
            {
                Destroy(spawnedEffects[i]);
            }
        }

        spawnedEffects.Clear();
    }
}
