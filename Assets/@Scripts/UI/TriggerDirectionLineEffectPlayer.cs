using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// OFF→ON으로 켜진 카드가 가진 트리거 방향을 임시 LineRenderer로 보여준다.
public class TriggerDirectionLineEffectPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Material lineMaterial;

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float travelDuration = 0.22f;
    [SerializeField, Min(0f)] private float holdDuration = 0.08f;
    [SerializeField, Min(0.1f)] private float lineWidth = 8f;
    [SerializeField, Min(0.01f)] private float cameraProjectionDepth = 5f;

    [Header("Colors")]
    [SerializeField] private Color startColor = new(0.2f, 0.95f, 1f, 0.9f);
    [SerializeField] private Color endColor = new(1f, 1f, 1f, 0.95f);

    private readonly List<GameObject> activeLines = new();

    private void Awake()
    {
        ResolveRefs();
    }

    public void Play(CardView sourceCard)
    {
        if (sourceCard?.Data == null || sourceCard.CurrentSlot == null) return;
        if (sourceCard.IsEnemy) return;
        if (!ResolveRefs()) return;

        // 실제 체인 대상 카드가 있는 방향만 발사 이펙트를 만든다.
        foreach (CardView targetCard in FindActualTargets(sourceCard))
        {
            if (targetCard == null || targetCard == sourceCard) continue;
            StartCoroutine(PlayLineRoutine(sourceCard, targetCard));
        }
    }

    public void Clear()
    {
        for (int i = activeLines.Count - 1; i >= 0; i--)
        {
            if (activeLines[i] != null)
                Destroy(activeLines[i]);
        }

        activeLines.Clear();
    }

    private IEnumerator PlayLineRoutine(CardView sourceCard, CardView targetCard)
    {
        if (!TryGetScreenCenter(sourceCard, out Vector2 startScreen)) yield break;
        if (!TryGetScreenCenter(targetCard, out Vector2 targetScreen)) yield break;
        if ((targetScreen - startScreen).sqrMagnitude < 1f) yield break;

        // UI 카드 중심점을 LineRenderer가 그릴 수 있는 월드 좌표로 투영한다.
        Vector3 startWorld = ScreenToEffectWorld(startScreen);
        Vector3 targetWorld = ScreenToEffectWorld(targetScreen);
        GameObject lineObject = CreateLineObject(out LineRenderer lineRenderer);
        if (lineObject == null || lineRenderer == null) yield break;

        activeLines.Add(lineObject);

        float elapsed = 0f;
        while (elapsed < travelDuration)
        {
            if (lineRenderer == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);
            SetLine(lineRenderer, startWorld, Vector3.Lerp(startWorld, targetWorld, t), 1f);
            yield return null;
        }

        SetLine(lineRenderer, startWorld, targetWorld, 1f);

        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        elapsed = 0f;
        float fadeDuration = Mathf.Max(0.04f, travelDuration * 0.35f);
        while (elapsed < fadeDuration)
        {
            if (lineRenderer == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            SetLine(lineRenderer, startWorld, targetWorld, alpha);
            yield return null;
        }

        DestroyLine(lineObject);
    }

    private List<CardView> FindActualTargets(CardView sourceCard)
    {
        List<CardView> targets = new();
        if (GridManager.Instance == null || sourceCard?.Data == null || sourceCard.CurrentSlot == null)
            return targets;

        // 카드의 방향/range 안에서 실제 점유 카드만 수집한다.
        foreach (GridSlot targetSlot in CardTargetResolver.ResolveToggleSlots(sourceCard, GridManager.Instance))
        {
            CardView target = targetSlot.OccupiedCard;
            if (target != null && !targets.Contains(target))
                targets.Add(target);
        }

        return targets;
    }

    private bool ResolveRefs()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (targetCamera == null)
        {
            if (rootCanvas != null && rootCanvas.worldCamera != null)
                targetCamera = rootCanvas.worldCamera;
            else
                targetCamera = Camera.main;
        }

        if (lineMaterial == null)
        {
            // 임시 VFX라 전용 머티리얼이 없으면 기본 스프라이트 셰이더로 생성한다.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        return targetCamera != null && lineMaterial != null;
    }

    private bool TryGetScreenCenter(CardView card, out Vector2 screenCenter)
    {
        screenCenter = default;
        if (card == null || !card.TryGetComponent(out RectTransform rectTransform)) return false;

        Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
        Camera canvasCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        screenCenter = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCenter);
        return IsFinite(screenCenter);
    }

    private Vector3 ScreenToEffectWorld(Vector2 screenPoint)
    {
        return targetCamera.ScreenToWorldPoint(new Vector3(
            screenPoint.x,
            screenPoint.y,
            Mathf.Max(targetCamera.nearClipPlane + 0.01f, cameraProjectionDepth)));
    }

    private float GetWorldLineWidth(float pixelWidth)
    {
        if (targetCamera == null) return pixelWidth;

        // 인스펙터 폭 값을 화면 픽셀 기준으로 다루기 위해 현재 카메라 깊이의 월드 폭으로 바꾼다.
        Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector3 a = ScreenToEffectWorld(center);
        Vector3 b = ScreenToEffectWorld(center + Vector2.right * Mathf.Max(1f, pixelWidth));
        float width = Vector3.Distance(a, b);
        return Mathf.Max(0.001f, width);
    }

    private GameObject CreateLineObject(out LineRenderer lineRenderer)
    {
        GameObject obj = new("TriggerDirectionLineEffect");
        obj.transform.SetParent(transform, false);

        // 추후 ParticleSystem/VFX로 교체하기 전까지 사용하는 일회성 라인 오브젝트.
        lineRenderer = obj.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.widthMultiplier = GetWorldLineWidth(lineWidth);
        lineRenderer.sortingOrder = 5000;
        lineRenderer.material = lineMaterial;

        return obj;
    }

    private void SetLine(LineRenderer lineRenderer, Vector3 start, Vector3 end, float alpha)
    {
        if (lineRenderer == null) return;

        Color lineStartColor = startColor;
        Color lineEndColor = endColor;
        lineStartColor.a *= alpha;
        lineEndColor.a *= alpha;

        lineRenderer.startColor = lineStartColor;
        lineRenderer.endColor = lineEndColor;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void DestroyLine(GameObject lineObject)
    {
        if (lineObject == null) return;

        activeLines.Remove(lineObject);
        Destroy(lineObject);
    }

    private void OnDisable()
    {
        Clear();
    }

    private static bool IsFinite(Vector2 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
