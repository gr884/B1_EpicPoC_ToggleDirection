using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardPieceShapeView : MonoBehaviour
{
    private const float CellGap = 2f;
    private RectTransform _root;

    public void Rebuild(
        CardData data,
        bool isOnGrid,
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
        if (isOnGrid)
            _root.SetAsFirstSibling();
        else
            _root.SetAsLastSibling();
        _root.anchorMin = isOnGrid ? Vector2.zero : new Vector2(0.62f, 0.54f);
        _root.anchorMax = isOnGrid ? Vector2.one : new Vector2(0.94f, 0.88f);
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;

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
            CreateCell(data, offset, min, width, height, isOnGrid, cellColor, arrowSprites);
    }

    private void EnsureRoot()
    {
        if (_root != null) return;

        GameObject rootObject = new("PieceShape", typeof(RectTransform));
        _root = rootObject.GetComponent<RectTransform>();
        _root.SetParent(transform, false);
        _root.SetAsFirstSibling();
    }

    private void CreateCell(
        CardData data,
        Vector2Int offset,
        Vector2Int min,
        int width,
        int height,
        bool isOnGrid,
        Color cellColor,
        IReadOnlyDictionary<CardDirection, Sprite> arrowSprites)
    {
        GameObject cellObject = new($"Cell_{offset.x}_{offset.y}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform cellRect = cellObject.GetComponent<RectTransform>();
        cellRect.SetParent(_root, false);
        cellRect.anchorMin = new Vector2((float)(offset.x - min.x) / width, (float)(offset.y - min.y) / height);
        cellRect.anchorMax = new Vector2((float)(offset.x - min.x + 1) / width, (float)(offset.y - min.y + 1) / height);
        cellRect.offsetMin = Vector2.one * CellGap;
        cellRect.offsetMax = -Vector2.one * CellGap;

        Image cellImage = cellObject.GetComponent<Image>();
        cellImage.color = cellColor;
        cellImage.raycastTarget = false;

        foreach (CardDirection direction in data.GetDirectionsAt(offset))
            CreateArrow(cellRect, direction, isOnGrid, arrowSprites);
    }

    private static void CreateArrow(
        RectTransform cellRect,
        CardDirection direction,
        bool isOnGrid,
        IReadOnlyDictionary<CardDirection, Sprite> arrowSprites)
    {
        if (arrowSprites == null
            || !arrowSprites.TryGetValue(direction, out Sprite sprite)
            || sprite == null)
            return;

        GameObject arrowObject = new($"Arrow_{direction}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.SetParent(cellRect, false);
        Vector2 anchor = DirectionToAnchor(direction);
        arrowRect.anchorMin = anchor;
        arrowRect.anchorMax = anchor;
        arrowRect.pivot = anchor;
        arrowRect.anchoredPosition = Vector2.zero;
        arrowRect.sizeDelta = isOnGrid ? new Vector2(26f, 26f) : new Vector2(12f, 12f);

        Image arrowImage = arrowObject.GetComponent<Image>();
        arrowImage.sprite = sprite;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;
    }

    private static Vector2 DirectionToAnchor(CardDirection direction) => direction switch
    {
        CardDirection.Up => new Vector2(0.5f, 1f),
        CardDirection.UpRight => new Vector2(1f, 1f),
        CardDirection.Right => new Vector2(1f, 0.5f),
        CardDirection.DownRight => new Vector2(1f, 0f),
        CardDirection.Down => new Vector2(0.5f, 0f),
        CardDirection.DownLeft => new Vector2(0f, 0f),
        CardDirection.Left => new Vector2(0f, 0.5f),
        CardDirection.UpLeft => new Vector2(0f, 1f),
        _ => new Vector2(0.5f, 0.5f)
    };
}
