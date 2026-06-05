using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DeckHandFlightEffectPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform effectLayer;
    [SerializeField] private Transform deckSource;
    [SerializeField] private Transform handTarget;
    [SerializeField] private Transform discardTarget;

    [Header("Sprites")]
    [SerializeField] private Sprite drawSprite;
    [SerializeField] private Sprite returnSprite;
    [SerializeField] private Sprite refillSprite;
    [SerializeField] private Sprite recallSprite;

    [Header("Motion")]
    [SerializeField] private Vector2 effectSize = new(112f, 144f);
    [SerializeField, Min(0f)] private float drawDuration = 0.28f;
    [SerializeField, Min(0f)] private float returnDuration = 0.24f;
    [SerializeField, Min(0f)] private float refillDuration = 0.32f;
    [SerializeField, Min(0f)] private float recallDuration = 0.24f;
    [SerializeField, Min(0f)] private float drawStaggerDelay = 0.05f;
    [SerializeField, Min(0f)] private float refillStaggerDelay = 0.04f;
    [SerializeField] private float arcHeight = 160f;
    [SerializeField, Min(0f)] private float refillArcHeight = 20f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField] private Color color = Color.white;

    private readonly List<GameObject> spawnedEffects = new();
    private readonly List<Tween> activeTweens = new();

    public bool CanPlayDraw => drawSprite != null && ResolveSceneRefs(null) && handTarget != null;
    public bool CanPlayReturn => returnSprite != null && ResolveSceneRefs(null) && discardTarget != null;
    public bool CanPlayRefill => GetRefillSprite() != null && ResolveSceneRefs(null) && discardTarget != null;
    public bool CanPlayRecall => GetRecallSprite() != null && ResolveSceneRefs(null) && handTarget != null;
    public float DrawStaggerDelay => drawStaggerDelay;
    public float DiscardFlightDuration => returnDuration;

    private void Reset()
    {
        ResolveSceneRefs(null);
    }

    private void OnDisable()
    {
        ClearActiveEffects();
    }

    public IEnumerator PlayDrawTo(Transform target)
    {
        if (drawSprite == null || target == null || !ResolveSceneRefs(target))
        {
            yield break;
        }

        if (!TryGetLocalPoint(deckSource, out Vector2 startPosition) ||
            !TryGetLocalPoint(target, out Vector2 targetPosition))
        {
            yield break;
        }

        yield return PlayFlightRoutine(drawSprite, startPosition, targetPosition, drawDuration);
    }

    public void PlayDrawToHand(int count)
    {
        if (count <= 0 || drawSprite == null || !ResolveSceneRefs(handTarget) || handTarget == null)
        {
            return;
        }

        StartCoroutine(PlayDrawToHandRoutine(count));
    }

    private IEnumerator PlayDrawToHandRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(PlayDrawTo(handTarget));

            if (drawStaggerDelay > 0f && i < count - 1)
            {
                yield return new WaitForSeconds(drawStaggerDelay);
            }
        }
    }

    public void PlayReturnFrom(Transform source)
    {
        PlayDiscardFrom(source);
    }

    public void PlayDiscardFrom(Transform source)
    {
        if (returnSprite == null || source == null || !ResolveSceneRefs(source))
        {
            return;
        }

        if (!TryGetLocalPoint(source, out Vector2 startPosition) ||
            !TryGetLocalPoint(discardTarget, out Vector2 targetPosition))
        {
            return;
        }

        StartCoroutine(PlayFlightRoutine(returnSprite, startPosition, targetPosition, returnDuration));
    }

    public void PlayRefillDiscardToDeck(int count)
    {
        if (count <= 0 || GetRefillSprite() == null || !ResolveSceneRefs(discardTarget) || discardTarget == null)
        {
            return;
        }

        StartCoroutine(PlayRefillDiscardToDeckRoutine(count));
    }

    private IEnumerator PlayRefillDiscardToDeckRoutine(int count)
    {
        Sprite sprite = GetRefillSprite();
        if (sprite == null ||
            !TryGetLocalPoint(discardTarget, out Vector2 startPosition) ||
            !TryGetLocalPoint(deckSource, out Vector2 targetPosition))
        {
            yield break;
        }

        float downwardArcHeight = -Mathf.Abs(refillArcHeight);

        for (int i = 0; i < count; i++)
        {
            StartCoroutine(PlayFlightRoutine(sprite, startPosition, targetPosition, refillDuration, downwardArcHeight));

            if (refillStaggerDelay > 0f && i < count - 1)
            {
                yield return new WaitForSeconds(refillStaggerDelay);
            }
        }
    }

    public void PlayRecallToHandFrom(Transform source)
    {
        Sprite sprite = GetRecallSprite();
        if (sprite == null || source == null || !ResolveSceneRefs(source) || handTarget == null)
        {
            return;
        }

        if (!TryGetLocalPoint(source, out Vector2 startPosition) ||
            !TryGetLocalPoint(handTarget, out Vector2 targetPosition))
        {
            return;
        }

        StartCoroutine(PlayFlightRoutine(sprite, startPosition, targetPosition, recallDuration));
    }

    private IEnumerator PlayFlightRoutine(Sprite sprite, Vector2 startPosition, Vector2 targetPosition, float duration)
    {
        return PlayFlightRoutine(sprite, startPosition, targetPosition, duration, arcHeight);
    }

    private IEnumerator PlayFlightRoutine(Sprite sprite, Vector2 startPosition, Vector2 targetPosition, float duration, float arcOffset)
    {
        GameObject effectObject = CreateEffectObject(sprite, startPosition);
        RectTransform effectRect = effectObject.GetComponent<RectTransform>();
        if (effectRect == null)
        {
            Destroy(effectObject);
            yield break;
        }

        if (duration <= 0f)
        {
            effectRect.anchoredPosition = targetPosition;
            DestroyEffectObject(effectObject);
            yield break;
        }

        Vector2 controlPosition = (startPosition + targetPosition) * 0.5f + Vector2.up * arcOffset;
        float progress = 0f;
        Tween tween = DOTween.To(() => progress, value =>
        {
            progress = value;
            effectRect.anchoredPosition = EvaluateQuadraticBezier(startPosition, controlPosition, targetPosition, progress);
        }, 1f, duration).SetEase(ease).SetTarget(effectObject);

        activeTweens.Add(tween);
        yield return tween.WaitForCompletion();
        activeTweens.Remove(tween);

        DestroyEffectObject(effectObject);
    }

    private Sprite GetRefillSprite()
    {
        return refillSprite != null ? refillSprite : returnSprite;
    }

    private Sprite GetRecallSprite()
    {
        if (recallSprite != null)
        {
            return recallSprite;
        }

        return drawSprite != null ? drawSprite : returnSprite;
    }

    private GameObject CreateEffectObject(Sprite sprite, Vector2 anchoredPosition)
    {
        GameObject obj = new("DeckHandFlightEffect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(effectLayer, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(Mathf.Max(1f, effectSize.x), Mathf.Max(1f, effectSize.y));
        rect.localScale = Vector3.one;

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;

        spawnedEffects.Add(obj);
        return obj;
    }

    private bool TryGetLocalPoint(Transform source, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (source == null || rootCanvas == null || effectLayer == null)
        {
            return false;
        }

        Vector3 worldPoint = GetWorldPoint(source);
        Camera sourceCamera = GetSourceCamera(source);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPoint);
        Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(effectLayer, screenPoint, canvasCamera, out localPoint);
    }

    private static Vector3 GetWorldPoint(Transform source)
    {
        if (source is RectTransform rectTransform)
        {
            return rectTransform.TransformPoint(rectTransform.rect.center);
        }

        return source.position;
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

    private bool ResolveSceneRefs(Transform source)
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
            Transform found = rootCanvas.transform.Find("DeckHandFlightEffectLayer");
            if (found != null)
            {
                effectLayer = found.GetComponent<RectTransform>();
            }

            if (effectLayer == null)
            {
                GameObject layerObject = new("DeckHandFlightEffectLayer", typeof(RectTransform));
                effectLayer = layerObject.GetComponent<RectTransform>();
                effectLayer.SetParent(rootCanvas.transform, false);
                effectLayer.anchorMin = Vector2.zero;
                effectLayer.anchorMax = Vector2.one;
                effectLayer.offsetMin = Vector2.zero;
                effectLayer.offsetMax = Vector2.zero;
            }

            // effectLayer.SetAsLastSibling();
        }

        if (deckSource == null)
        {
            GameObject found = GameObject.Find("DeckImg");
            deckSource = found != null ? found.transform : null;
        }

        if (handTarget == null)
        {
            GameObject found = GameObject.Find("Hand");
            handTarget = found != null ? found.transform : null;
        }

        if (discardTarget == null)
        {
            GameObject found = GameObject.Find("DiscardImg");
            if (found == null)
            {
                found = GameObject.Find("DiscardPileButton");
            }

            discardTarget = found != null ? found.transform : null;
        }

        return rootCanvas != null && effectLayer != null && deckSource != null;
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * start + 2f * oneMinusT * t * control + t * t * end;
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
