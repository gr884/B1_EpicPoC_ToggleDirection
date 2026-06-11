using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScorePieceView : MonoBehaviour
{
    private const float BorderWidth = 5f;
    private ScorePieceRuntime _runtime;
    private RectTransform _boardRoot;
    private Vector2 _cellSize;
    private Vector2 _spacing;

    public void Initialize(
        ScorePieceRuntime runtime,
        RectTransform boardRoot,
        Vector2 cellSize,
        Vector2 spacing)
    {
        _runtime = runtime;
        _boardRoot = boardRoot;
        _cellSize = cellSize;
        _spacing = spacing;
        Rebuild();
    }

    public void RefreshState()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        if (_runtime?.Data == null || _boardRoot == null) return;

        foreach (Vector2Int offset in _runtime.Data.GetRotatedOccupiedOffsets(_runtime.QuarterTurns))
            CreateOccupiedCell(offset);
        foreach (Vector2Int signal in _runtime.RotatedSignalOffsets)
            CreateSignal(signal);

        // Keep durability readable even when a signal overlaps the anchor cell.
        CreateDurabilityOverlay();
    }

    private void CreateOccupiedCell(Vector2Int offset)
    {
        RectTransform cell = CreatePositionedRect($"Body_{offset.x}_{offset.y}", offset, _cellSize);
        Color color = GetStateColor();

        CreateBorder(cell, color);
    }

    private void CreateSignal(Vector2Int offset)
    {
        Vector2 size = _cellSize * 0.28f;
        RectTransform signal = CreatePositionedRect($"Signal_{offset.x}_{offset.y}", offset, size);
        Image image = signal.gameObject.AddComponent<Image>();
        image.color = GetStateColor();
        image.raycastTarget = false;
        Outline outline = signal.gameObject.AddComponent<Outline>();
        outline.effectColor = _runtime.IsOn ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private Color GetStateColor()
    {
        if (_runtime.IsOn)
            return new Color(_runtime.Color.r, _runtime.Color.g, _runtime.Color.b, 1f);

        Color offColor = Color.Lerp(_runtime.Color, Color.black, 0.42f);
        offColor.a = 0.95f;
        return offColor;
    }

    private RectTransform CreatePositionedRect(string objectName, Vector2Int offset, Vector2 size)
    {
        GameObject obj = new(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 step = _cellSize + _spacing;
        Vector2 baseCenter = new(
            _runtime.Anchor.x * step.x + _cellSize.x * 0.5f,
            -_boardRoot.rect.height + _runtime.Anchor.y * step.y + _cellSize.y * 0.5f);
        rect.anchoredPosition = baseCenter + new Vector2(offset.x * step.x, offset.y * step.y);
        rect.sizeDelta = size;
        return rect;
    }

    private static void CreateBorder(RectTransform parent, Color color)
    {
        CreateLine(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, BorderWidth), color);
        CreateLine(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, BorderWidth), color);
        CreateLine(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(BorderWidth, 0f), color);
        CreateLine(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(BorderWidth, 0f), color);
    }

    private static void CreateLine(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 sizeDelta,
        Color color)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.sizeDelta = sizeDelta;
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void CreateDurabilityOverlay()
    {
        RectTransform parent = CreatePositionedRect("DurabilityOverlay", Vector2Int.zero, _cellSize);
        GameObject obj = new("Durability", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = _runtime.Durability.ToString();
        text.fontSize = Mathf.Max(18f, _cellSize.y * 0.32f);
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }
}
