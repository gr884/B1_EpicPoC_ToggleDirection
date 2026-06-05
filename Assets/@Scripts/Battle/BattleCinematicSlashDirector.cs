using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

public class BattleCinematicSlashDirector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform effectLayer;
    [SerializeField] private MMF_Player hitFeedbackPlayer;

    [Header("Sprite")]
    [SerializeField] private Sprite loopSprite;
    [SerializeField] private Vector2 effectSize = new(96f, 96f);

    [Header("Motion")]
    [SerializeField, Min(0f)] private float flightDuration = 0.55f;
    [SerializeField, Min(0f)] private float staggerDelay = 0.04f;
    [SerializeField] private float arcHeight = 120f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private Ease flightEase = Ease.OutQuad;

    [Header("Color Cycle")]
    [SerializeField, Min(0.01f)] private float colorCycleDuration = 0.45f;
    [SerializeField] private Color[] colorCycle =
    {
        Color.red,
        Color.yellow,
        Color.green,
        Color.cyan,
        Color.blue,
        new(0.65f, 0f, 1f, 1f)
    };

    [Header("Feel Fallback")]
    [SerializeField] private bool autoCreateFallbackFeedback = true;
    [SerializeField, Min(0f)] private float fallbackShakeDuration = 0.1f;
    [SerializeField, Min(0f)] private float fallbackShakeAmplitude = 0.2f;
    [SerializeField, Min(0f)] private float fallbackShakeFrequency = 40f;

    private readonly List<GameObject> spawnedEffects = new();
    private readonly List<Tween> activeTweens = new();
    private Coroutine routine;

    public void Play()
    {
        StopAndRestore();

        if (!ResolveSceneRefs(out Transform enemyTarget))
            return;

        routine = StartCoroutine(PlayRoutine(enemyTarget));
    }

    public IEnumerator PlayAndWait()
    {
        StopAndRestore();

        if (!ResolveSceneRefs(out Transform enemyTarget))
            yield break;

        yield return PlayRoutine(enemyTarget);
    }

    public void StopAndRestore()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ClearActiveEffects();
    }

    private void OnDisable()
    {
        StopAndRestore();
    }

    private IEnumerator PlayRoutine(Transform enemyTarget)
    {
        List<Transform> cardTransforms = CollectGridCardTransforms();
        if (cardTransforms.Count == 0 || loopSprite == null)
        {
            if (loopSprite == null)
                Debug.LogWarning("[BattleCinematicSlashDirector] loopSprite가 연결되지 않았습니다.");

            routine = null;
            yield break;
        }

        if (!TryGetLocalPoint(enemyTarget, out Vector2 targetPosition))
        {
            routine = null;
            yield break;
        }

        Vector3 targetWorldPosition = GetWorldPoint(enemyTarget);
        int runningCount = 0;

        for (int i = 0; i < cardTransforms.Count; i++)
        {
            Transform source = cardTransforms[i];
            if (source == null) continue;
            if (!TryGetLocalPoint(source, out Vector2 startPosition)) continue;

            runningCount++;
            StartCoroutine(PlayCardFlightRoutine(startPosition, targetPosition, targetWorldPosition, () => runningCount--));

            if (staggerDelay > 0f && i < cardTransforms.Count - 1)
                yield return new WaitForSeconds(staggerDelay);
        }

        while (runningCount > 0)
            yield return null;

        routine = null;
    }

    private IEnumerator PlayCardFlightRoutine(
        Vector2 startPosition,
        Vector2 targetPosition,
        Vector3 targetWorldPosition,
        System.Action onComplete)
    {
        GameObject effectObject = CreateEffectObject(startPosition);
        RectTransform rectTransform = effectObject.GetComponent<RectTransform>();
        Image image = effectObject.GetComponent<Image>();

        if (rectTransform == null || image == null)
        {
            DestroyEffectObject(effectObject);
            onComplete?.Invoke();
            yield break;
        }

        float duration = Mathf.Max(0f, flightDuration);
        if (duration <= 0f)
        {
            rectTransform.anchoredPosition = targetPosition;
            PlayHitFeedback(targetWorldPosition);
            DestroyEffectObject(effectObject);
            onComplete?.Invoke();
            yield break;
        }

        Vector2 controlPosition = (startPosition + targetPosition) * 0.5f + Vector2.up * arcHeight;
        float progress = 0f;

        Tween tween = DOTween.To(() => progress, value =>
        {
            progress = value;
            rectTransform.anchoredPosition = EvaluateQuadraticBezier(startPosition, controlPosition, targetPosition, progress);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationSpeed * duration * progress);
            image.color = EvaluateColor(progress * duration);
        }, 1f, duration).SetEase(flightEase).SetTarget(effectObject);

        activeTweens.Add(tween);
        yield return tween.WaitForCompletion();
        activeTweens.Remove(tween);

        PlayHitFeedback(targetWorldPosition);
        DestroyEffectObject(effectObject);
        onComplete?.Invoke();
    }

    private bool ResolveSceneRefs(out Transform enemyTarget)
    {
        enemyTarget = ResolveEnemyTarget();

        if (rootCanvas == null)
            rootCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (effectLayer == null && rootCanvas != null)
            effectLayer = ResolveEffectLayer(rootCanvas.transform);

        if (hitFeedbackPlayer == null && autoCreateFallbackFeedback)
            hitFeedbackPlayer = ResolveFallbackFeedbackPlayer();

        bool valid = rootCanvas != null && effectLayer != null && enemyTarget != null;
        if (!valid)
            Debug.LogWarning("[BattleCinematicSlashDirector] Missing canvas, effect layer, or enemy target.");

        return valid;
    }

    private Transform ResolveEnemyTarget()
    {
        Enemy enemy = BattleManager.Instance != null ? BattleManager.Instance.Enemy : null;
        if (enemy == null) return null;

        if (enemy.MotionTarget != null && enemy.MotionTarget.AttackPoint != null)
            return enemy.MotionTarget.AttackPoint;

        if (enemy.ViewTransform != null)
            return enemy.ViewTransform;

        return enemy.transform;
    }

    private RectTransform ResolveEffectLayer(Transform canvasTransform)
    {
        Transform found = canvasTransform.Find("InfiniteLoopFlightEffectLayer");
        if (found != null && found.TryGetComponent(out RectTransform foundRect))
            return foundRect;

        GameObject layerObject = new("InfiniteLoopFlightEffectLayer", typeof(RectTransform));
        RectTransform rectTransform = layerObject.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.SetAsLastSibling();
        return rectTransform;
    }

    private MMF_Player ResolveFallbackFeedbackPlayer()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return null;

        if (mainCamera.GetComponent<MMCameraShaker>() == null)
            mainCamera.gameObject.AddComponent<MMCameraShaker>();

        GameObject feedbackObject = new("InfiniteLoopHitFeedbackPlayer", typeof(MMF_Player));
        feedbackObject.transform.SetParent(transform, false);

        MMF_Player player = feedbackObject.GetComponent<MMF_Player>();
        player.FeedbacksList = new List<MMF_Feedback>();

        MMF_CameraShake cameraShake = new()
        {
            CameraShakeProperties = new MMCameraShakeProperties(
                fallbackShakeDuration,
                fallbackShakeAmplitude,
                fallbackShakeFrequency)
        };

        player.FeedbacksList.Add(cameraShake);
        player.Initialization(true);
        return player;
    }

    private List<Transform> CollectGridCardTransforms()
    {
        List<Transform> result = new();
        if (GridManager.Instance == null) return result;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot != null ? slot.OccupiedCard : null;
            if (card != null)
                result.Add(card.transform);
        }

        return result;
    }

    private GameObject CreateEffectObject(Vector2 anchoredPosition)
    {
        GameObject obj = new("InfiniteLoopFlightEffect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        rectTransform.SetParent(effectLayer, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, effectSize.x), Mathf.Max(1f, effectSize.y));
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;

        Image image = obj.GetComponent<Image>();
        image.sprite = loopSprite;
        image.color = EvaluateColor(0f);
        image.preserveAspect = true;
        image.raycastTarget = false;

        spawnedEffects.Add(obj);
        return obj;
    }

    private bool TryGetLocalPoint(Transform source, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (source == null || rootCanvas == null || effectLayer == null)
            return false;

        Vector3 worldPoint = GetWorldPoint(source);
        Camera sourceCamera = GetSourceCamera(source);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPoint);
        Camera canvasCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(effectLayer, screenPoint, canvasCamera, out localPoint);
    }

    private void PlayHitFeedback(Vector3 worldPosition)
    {
        if (hitFeedbackPlayer == null) return;
        hitFeedbackPlayer.PlayFeedbacks(worldPosition);
    }

    private Color EvaluateColor(float elapsed)
    {
        if (colorCycle == null || colorCycle.Length == 0)
            return Color.white;
        if (colorCycle.Length == 1)
            return colorCycle[0];

        float cycleDuration = Mathf.Max(0.01f, colorCycleDuration);
        float normalized = Mathf.Repeat(elapsed / cycleDuration, 1f) * colorCycle.Length;
        int fromIndex = Mathf.FloorToInt(normalized) % colorCycle.Length;
        int toIndex = (fromIndex + 1) % colorCycle.Length;
        float t = normalized - Mathf.Floor(normalized);
        return Color.Lerp(colorCycle[fromIndex], colorCycle[toIndex], t);
    }

    private void DestroyEffectObject(GameObject effectObject)
    {
        if (effectObject == null) return;

        spawnedEffects.Remove(effectObject);
        Destroy(effectObject);
    }

    private void ClearActiveEffects()
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
            activeTweens[i]?.Kill();
        activeTweens.Clear();

        for (int i = spawnedEffects.Count - 1; i >= 0; i--)
            if (spawnedEffects[i] != null)
                Destroy(spawnedEffects[i]);
        spawnedEffects.Clear();
    }

    private static Vector3 GetWorldPoint(Transform source)
    {
        if (source is RectTransform rectTransform)
            return rectTransform.TransformPoint(rectTransform.rect.center);

        return source.position;
    }

    private static Camera GetSourceCamera(Transform source)
    {
        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        if (sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return sourceCanvas.worldCamera;

        return source is RectTransform ? null : Camera.main;
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * start + 2f * oneMinusT * t * control + t * t * end;
    }
}
