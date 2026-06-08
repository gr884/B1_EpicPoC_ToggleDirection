using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DirectionalImpactTrailEffectPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform effectLayer;

    [Header("Motion")]
    [SerializeField, Min(1f)] private float moveSpeed = 260f;
    [SerializeField, Min(1f)] private float headSize = 20f;
    [SerializeField, Min(0)] private int trailCount = 7;
    [SerializeField, Min(0f)] private float trailSpacing = 8f;
    [SerializeField, Min(0f)] private float slotOuterOffset = 4f;

    [Header("Colors")]
    [SerializeField] private Color headColor = new(1f, 0.32f, 0.05f, 1f);
    [SerializeField] private Color trailColor = new(1f, 0.08f, 0.02f, 0.65f);

    private readonly List<LoopingTrail> activeTrails = new();
    private readonly Vector3[] worldCorners = new Vector3[4];
    private Sprite radialSprite;

    private void Awake()
    {
        if (ResolveRefs())
            EnsureLayoutExcluded();
    }

    public void AttachToGridRoot(RectTransform gridRoot)
    {
        if (gridRoot == null) return;
        if (!ResolveRefs()) return;

        if (effectLayer.parent != gridRoot)
            effectLayer.SetParent(gridRoot, false);

        effectLayer.anchorMin = Vector2.zero;
        effectLayer.anchorMax = Vector2.one;
        effectLayer.pivot = new Vector2(0.5f, 0.5f);
        effectLayer.anchoredPosition = Vector2.zero;
        effectLayer.sizeDelta = Vector2.zero;
        effectLayer.localRotation = Quaternion.identity;
        effectLayer.localScale = Vector3.one;
        effectLayer.SetAsLastSibling();

        EnsureLayoutExcluded();
    }

    private void EnsureLayoutExcluded()
    {
        if (!TryGetComponent(out LayoutElement layoutElement))
            layoutElement = gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
    }

    public void Show(IReadOnlyList<GridSlot> slots)
    {
        Clear();
        if (slots == null || slots.Count == 0) return;
        if (!ResolveRefs()) return;

        Canvas.ForceUpdateCanvases();
        Sprite sprite = GetRadialSprite();

        for (int i = 0; i < slots.Count; i++)
        {
            GridSlot slot = slots[i];
            if (slot == null) continue;
            if (!TryBuildSlotPath(slot, out List<Vector2> waypoints)) continue;

            LoopingTrail trail = CreateTrail(waypoints, sprite);
            if (trail != null)
                activeTrails.Add(trail);
        }
    }

    public void Clear()
    {
        for (int i = activeTrails.Count - 1; i >= 0; i--)
            activeTrails[i]?.Destroy();
        activeTrails.Clear();
    }

    private void Update()
    {
        if (activeTrails.Count == 0) return;

        float distanceDelta = moveSpeed * Time.unscaledDeltaTime;
        for (int i = 0; i < activeTrails.Count; i++)
            activeTrails[i].Update(distanceDelta, trailSpacing, headSize, headColor, trailColor);
    }

    private void OnDisable()
    {
        Clear();
    }

    private bool ResolveRefs()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (effectLayer == null)
            effectLayer = transform as RectTransform;

        return effectLayer != null;
    }

    private bool TryBuildSlotPath(GridSlot slot, out List<Vector2> waypoints)
    {
        waypoints = null;
        if (slot == null || effectLayer == null) return false;
        if (!slot.TryGetComponent(out RectTransform slotRect)) return false;

        slotRect.GetWorldCorners(worldCorners);
        Vector2[] localCorners = new Vector2[4];

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector3 localPoint = effectLayer.InverseTransformPoint(worldCorners[i]);
            if (!IsFinite(localPoint))
                return false;
            localCorners[i] = new Vector2(localPoint.x, localPoint.y);
        }

        Vector2 center = Vector2.zero;
        for (int i = 0; i < localCorners.Length; i++)
            center += localCorners[i];
        center /= localCorners.Length;

        waypoints = new List<Vector2>(4);
        for (int i = 0; i < localCorners.Length; i++)
        {
            Vector2 outward = localCorners[i] - center;
            if (outward.sqrMagnitude > 0.0001f)
                outward.Normalize();
            Vector2 waypoint = localCorners[i] + outward * slotOuterOffset;
            if (!IsFinite(waypoint))
                return false;
            waypoints.Add(waypoint);
        }

        return true;
    }

    private LoopingTrail CreateTrail(List<Vector2> waypoints, Sprite sprite)
    {
        if (waypoints == null || waypoints.Count < 2 || sprite == null || effectLayer == null) return null;

        LoopingTrail trail = new(waypoints);
        if (!trail.IsValid) return null;

        trail.Head = CreateDot("DirectionalImpactHead", headSize, sprite, headColor);
        for (int i = 0; i < trailCount; i++)
        {
            float t = trailCount <= 0 ? 0f : 1f - (i + 1f) / (trailCount + 1f);
            RectTransform dot = CreateDot("DirectionalImpactTrail", headSize * Mathf.Lerp(0.35f, 0.85f, t), sprite, trailColor);
            Image image = dot.GetComponent<Image>();
            trail.TrailDots.Add(dot);
            trail.TrailImages.Add(image);
        }

        trail.Update(0f, trailSpacing, headSize, headColor, trailColor);
        return trail;
    }

    private RectTransform CreateDot(string objectName, float size, Sprite sprite, Color color)
    {
        GameObject obj = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(effectLayer, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.one * Mathf.Max(1f, size);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    private Sprite GetRadialSprite()
    {
        if (radialSprite != null) return radialSprite;

        const int size = 64;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            name = "DirectionalImpactRadialTexture",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance01 = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance01);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        radialSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        radialSprite.name = "DirectionalImpactRadialSprite";
        radialSprite.hideFlags = HideFlags.HideAndDontSave;
        return radialSprite;
    }

    private static bool IsFinite(Vector2 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y);
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    private sealed class LoopingTrail
    {
        private readonly List<Vector2> waypoints;
        private readonly float[] segmentLengths;
        private float distance;
        private readonly float totalLength;

        public RectTransform Head { get; set; }
        public List<RectTransform> TrailDots { get; } = new();
        public List<Image> TrailImages { get; } = new();
        public bool IsValid => float.IsFinite(totalLength) && totalLength > 0f;

        public LoopingTrail(List<Vector2> sourceWaypoints)
        {
            waypoints = sourceWaypoints;
            segmentLengths = new float[waypoints.Count];

            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector2 a = waypoints[i];
                Vector2 b = waypoints[(i + 1) % waypoints.Count];
                segmentLengths[i] = Vector2.Distance(a, b);
                if (!float.IsFinite(segmentLengths[i]))
                {
                    totalLength = 0f;
                    return;
                }
                totalLength += segmentLengths[i];
            }
        }

        public void Update(float distanceDelta, float trailSpacing, float headSize, Color headColor, Color trailColor)
        {
            if (!IsValid) return;

            distance = Mathf.Repeat(distance + distanceDelta, totalLength);
            if (Head != null)
            {
                Head.anchoredPosition = Evaluate(distance);
                Head.sizeDelta = Vector2.one * Mathf.Max(1f, headSize);
                Image headImage = Head.GetComponent<Image>();
                if (headImage != null)
                    headImage.color = headColor;
            }

            float spacing = Mathf.Max(1f, trailSpacing);
            for (int i = 0; i < TrailDots.Count; i++)
            {
                RectTransform dot = TrailDots[i];
                if (dot == null) continue;

                float trailDistance = Mathf.Repeat(distance - spacing * (i + 1), totalLength);
                dot.anchoredPosition = Evaluate(trailDistance);

                float fade = 1f - (i + 1f) / (TrailDots.Count + 1f);
                dot.sizeDelta = Vector2.one * Mathf.Max(1f, headSize * Mathf.Lerp(0.35f, 0.85f, fade));

                if (i < TrailImages.Count && TrailImages[i] != null)
                {
                    Color color = trailColor;
                    color.a *= fade;
                    TrailImages[i].color = color;
                }
            }
        }

        public void Destroy()
        {
            if (Head != null)
                Object.Destroy(Head.gameObject);

            for (int i = TrailDots.Count - 1; i >= 0; i--)
                if (TrailDots[i] != null)
                    Object.Destroy(TrailDots[i].gameObject);

            TrailDots.Clear();
            TrailImages.Clear();
        }

        private Vector2 Evaluate(float targetDistance)
        {
            float remaining = Mathf.Repeat(targetDistance, totalLength);

            for (int i = 0; i < segmentLengths.Length; i++)
            {
                float segmentLength = segmentLengths[i];
                if (remaining <= segmentLength || i == segmentLengths.Length - 1)
                {
                    Vector2 a = waypoints[i];
                    Vector2 b = waypoints[(i + 1) % waypoints.Count];
                    float t = segmentLength > 0f ? remaining / segmentLength : 0f;
                    return Vector2.LerpUnclamped(a, b, t);
                }

                remaining -= segmentLength;
            }

            return waypoints[0];
        }
    }
}
