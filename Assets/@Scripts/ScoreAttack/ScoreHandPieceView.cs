using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScoreHandPieceView : MonoBehaviour, IPointerClickHandler
{
    private ScorePieceData _data;
    private Action<ScoreHandPieceView> _clicked;
    private Image _selection;
    private int _quarterTurns;
    private List<Vector2Int> _signalOffsets = new();

    public ScorePieceData Data => _data;
    public int QuarterTurns => _quarterTurns;
    public IReadOnlyList<Vector2Int> SignalOffsets => _signalOffsets;

    public void Initialize(ScorePieceData data, Action<ScoreHandPieceView> clicked)
    {
        _data = data;
        _quarterTurns = 0;
        _signalOffsets = data != null ? data.CreateRandomSignalOffsets() : new List<Vector2Int>();
        _clicked = clicked;
        Rebuild();
    }

    public void Rotate(int direction)
    {
        _quarterTurns = ((_quarterTurns + direction) % 4 + 4) % 4;
        Rebuild();
        SetSelected(true);
    }

    public void SetSelected(bool selected)
    {
        if (_selection != null)
            _selection.color = selected
                ? new Color(1f, 0.82f, 0.2f, 1f)
                : new Color(1f, 1f, 1f, 0.22f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            _clicked?.Invoke(this);
    }

    private void Rebuild()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        Image background = GetComponent<Image>();
        if (background == null)
            background = gameObject.AddComponent<Image>();
        background.color = new Color(0.04f, 0.07f, 0.1f, 0.94f);

        _selection = CreateStretchImage("Selection", new Color(1f, 1f, 1f, 0.22f), 3f);
        CreateTitle();
        CreatePreview();
    }

    private void CreateTitle()
    {
        GameObject obj = new("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0.76f);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = _data != null ? _data.displayName : "";
        text.fontSize = 16f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private void CreatePreview()
    {
        if (_data == null) return;
        const int previewSize = 5;
        RectTransform preview = new GameObject("Preview", typeof(RectTransform)).GetComponent<RectTransform>();
        preview.SetParent(transform, false);
        preview.anchorMin = new Vector2(0.12f, 0.08f);
        preview.anchorMax = new Vector2(0.88f, 0.74f);
        preview.offsetMin = Vector2.zero;
        preview.offsetMax = Vector2.zero;

        for (int y = 0; y < previewSize; y++)
        for (int x = 0; x < previewSize; x++)
            CreatePreviewCell(preview, new Vector2Int(x, y), previewSize, new Color(1f, 1f, 1f, 0.07f), false);

        foreach (Vector2Int offset in _data.GetRotatedOccupiedOffsets(_quarterTurns))
            CreatePreviewCell(preview, offset + Vector2Int.one * 2, previewSize, _data.pieceColor, true);
        foreach (Vector2Int signal in _signalOffsets)
        {
            Vector2Int rotated = ScorePieceData.RotateOffset(signal, _quarterTurns);
            CreateSignal(preview, rotated + Vector2Int.one * 2, previewSize, _data.pieceColor);
        }
    }

    private static void CreatePreviewCell(
        RectTransform parent,
        Vector2Int position,
        int gridSize,
        Color color,
        bool borderOnly)
    {
        if (position.x < 0 || position.x >= gridSize || position.y < 0 || position.y >= gridSize)
            return;
        GameObject obj = new("Cell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(position.x / (float)gridSize, position.y / (float)gridSize);
        rect.anchorMax = new Vector2((position.x + 1f) / gridSize, (position.y + 1f) / gridSize);
        rect.offsetMin = Vector2.one;
        rect.offsetMax = -Vector2.one;
        Image image = obj.GetComponent<Image>();
        image.color = borderOnly ? new Color(color.r, color.g, color.b, 0.28f) : color;
        image.raycastTarget = false;
        if (borderOnly)
        {
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }

    private void CreateSignal(RectTransform parent, Vector2Int position, int gridSize, Color color)
    {
        if (position.x < 0 || position.x >= gridSize || position.y < 0 || position.y >= gridSize)
            return;
        GameObject obj = new("Signal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Vector2 center = new((position.x + 0.5f) / gridSize, (position.y + 0.5f) / gridSize);
        rect.anchorMin = center;
        rect.anchorMax = center;
        rect.sizeDelta = new Vector2(14f, 14f);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private Image CreateStretchImage(string name, Color color, float inset)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = -Vector2.one * inset;
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
