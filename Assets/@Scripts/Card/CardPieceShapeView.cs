using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardPieceShapeView : MonoBehaviour
{
    private const float HandPreviewSize = 84f;
    private RectTransform _root;

    public void Rebuild(
        CardData data,
        bool onGrid,
        Color cellColor,
        IReadOnlyDictionary<CardDirection, Sprite> arrowSprites)
    {
        EnsureRoot();
        for (int i = _root.childCount - 1; i >= 0; i--)
            Destroy(_root.GetChild(i).gameObject);

        if (data == null || data.pieceCells == null || data.pieceCells.Count == 0)
        {
            _root.gameObject.SetActive(false);
            return;
        }

        _root.gameObject.SetActive(true);
        if (onGrid)
        {
            _root.SetAsFirstSibling();
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
        }
        else
        {
            _root.SetAsLastSibling();
            // Hand cards always show their footprint on a centered 3x3 board.
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.anchoredPosition = Vector2.zero;
            _root.sizeDelta = new Vector2(HandPreviewSize, HandPreviewSize);
            CreateHandPreviewBackground();
            CreateHandPreviewGrid();
        }

        List<Vector2Int> offsets = new(data.GetOccupiedOffsets());
        Vector2Int min = offsets[0];
        Vector2Int max = offsets[0];
        foreach (Vector2Int offset in offsets)
        {
            min = Vector2Int.Min(min, offset);
            max = Vector2Int.Max(max, offset);
        }

        int width = max.x - min.x + 1;
        int height = max.y - min.y + 1;
        foreach (Vector2Int offset in offsets)
        {
            if (!onGrid && (Mathf.Abs(offset.x) > 1 || Mathf.Abs(offset.y) > 1))
                continue;

            CreateCell(data, offset, min, width, height, onGrid, cellColor, arrowSprites);
        }
    }

    private void EnsureRoot()
    {
        if (_root != null) return;
        GameObject obj = new("PieceShape", typeof(RectTransform));
        _root = obj.GetComponent<RectTransform>();
        _root.SetParent(transform, false);
    }

    private void CreateCell(
        CardData data,
        Vector2Int offset,
        Vector2Int min,
        int width,
        int height,
        bool onGrid,
        Color cellColor,
        IReadOnlyDictionary<CardDirection, Sprite> arrowSprites)
    {
        GameObject obj = new($"Cell_{offset.x}_{offset.y}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(_root, false);
        if (onGrid)
        {
            rect.anchorMin = new Vector2((float)(offset.x - min.x) / width, (float)(offset.y - min.y) / height);
            rect.anchorMax = new Vector2((float)(offset.x - min.x + 1) / width, (float)(offset.y - min.y + 1) / height);
        }
        else
        {
            int column = offset.x + 1;
            int row = offset.y + 1;
            rect.anchorMin = new Vector2(column / 3f, row / 3f);
            rect.anchorMax = new Vector2((column + 1) / 3f, (row + 1) / 3f);
        }
        rect.offsetMin = Vector2.one * 2f;
        rect.offsetMax = -Vector2.one * 2f;

        Image image = obj.GetComponent<Image>();
        Color displayedCellColor = cellColor;
        if (!onGrid)
            displayedCellColor.a = 1f;
        image.color = displayedCellColor;
        image.raycastTarget = false;

        foreach (CardDirection direction in data.GetDirectionsAt(offset))
            CreateArrow(rect, direction, arrowSprites);
    }

    private void CreateHandPreviewGrid()
    {
        Color emptyCellColor = new(1f, 1f, 1f, 0.32f);
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                GameObject obj = new($"Preview_{column}_{row}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.SetParent(_root, false);
                rect.anchorMin = new Vector2(column / 3f, row / 3f);
                rect.anchorMax = new Vector2((column + 1) / 3f, (row + 1) / 3f);
                rect.offsetMin = Vector2.one;
                rect.offsetMax = -Vector2.one;

                Image image = obj.GetComponent<Image>();
                image.color = emptyCellColor;
                image.raycastTarget = false;
            }
        }
    }

    private void CreateHandPreviewBackground()
    {
        GameObject obj = new("PreviewBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(_root, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = -Vector2.one * 3f;
        rect.offsetMax = Vector2.one * 3f;

        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.03f, 0.05f, 0.08f, 0.88f);
        image.raycastTarget = false;
    }

    private static void CreateArrow(
        RectTransform parent,
        CardDirection direction,
        IReadOnlyDictionary<CardDirection, Sprite> arrowSprites)
    {
        if (arrowSprites == null
            || !arrowSprites.TryGetValue(direction, out Sprite sprite)
            || sprite == null)
            return;

        GameObject obj = new($"Arrow_{direction}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        // Direction sprites are authored as full-card overlays, including their
        // own padding. Stretch them across one piece cell like the legacy view.
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }
}
