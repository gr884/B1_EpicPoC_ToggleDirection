using System.Collections.Generic;
using UnityEngine;

public static class RelicShapeUtility
{
    public static List<Vector2Int> RotateAndNormalizeCells(
        IReadOnlyList<Vector2Int> cells,
        Vector2Int boundsSize,
        int rotationSteps,
        out Vector2Int minOffset)
    {
        List<Vector2Int> rotated = new();
        if (cells == null || cells.Count == 0)
        {
            rotated.Add(Vector2Int.zero);
            minOffset = Vector2Int.zero;
            return rotated;
        }

        Vector2Int min = new(int.MaxValue, int.MaxValue);
        foreach (Vector2Int cell in cells)
        {
            Vector2Int value = RotateCell(cell, boundsSize, rotationSteps);
            rotated.Add(value);
            min.x = Mathf.Min(min.x, value.x);
            min.y = Mathf.Min(min.y, value.y);
        }

        minOffset = min;
        for (int i = 0; i < rotated.Count; i++)
            rotated[i] -= minOffset;

        return rotated;
    }

    public static Vector2Int RotateCell(Vector2Int cell, Vector2Int boundsSize, int rotationSteps)
    {
        Vector2Int result = cell;
        Vector2Int size = new(Mathf.Max(1, boundsSize.x), Mathf.Max(1, boundsSize.y));
        int steps = NormalizeRotation(rotationSteps);

        for (int i = 0; i < steps; i++)
        {
            result = new Vector2Int(size.y - 1 - result.y, result.x);
            size = new Vector2Int(size.y, size.x);
        }

        return result;
    }

    public static CardDirection RotateDirection(CardDirection direction, int rotationSteps)
    {
        if (direction == CardDirection.None) return CardDirection.None;

        int index = DirectionToIndex(direction);
        if (index < 0) return direction;
        index = (index + NormalizeRotation(rotationSteps) * 2) % 8;
        return IndexToDirection(index);
    }

    public static Vector2Int DirectionToDelta(CardDirection direction) => direction switch
    {
        CardDirection.Up => new Vector2Int(0, 1),
        CardDirection.UpRight => new Vector2Int(1, 1),
        CardDirection.Right => new Vector2Int(1, 0),
        CardDirection.DownRight => new Vector2Int(1, -1),
        CardDirection.Down => new Vector2Int(0, -1),
        CardDirection.DownLeft => new Vector2Int(-1, -1),
        CardDirection.Left => new Vector2Int(-1, 0),
        CardDirection.UpLeft => new Vector2Int(-1, 1),
        _ => Vector2Int.zero
    };

    private static int NormalizeRotation(int rotationSteps)
    {
        int steps = rotationSteps % 4;
        return steps < 0 ? steps + 4 : steps;
    }

    private static int DirectionToIndex(CardDirection direction) => direction switch
    {
        CardDirection.Up => 0,
        CardDirection.UpRight => 1,
        CardDirection.Right => 2,
        CardDirection.DownRight => 3,
        CardDirection.Down => 4,
        CardDirection.DownLeft => 5,
        CardDirection.Left => 6,
        CardDirection.UpLeft => 7,
        _ => -1
    };

    private static CardDirection IndexToDirection(int index) => index switch
    {
        0 => CardDirection.Up,
        1 => CardDirection.UpRight,
        2 => CardDirection.Right,
        3 => CardDirection.DownRight,
        4 => CardDirection.Down,
        5 => CardDirection.DownLeft,
        6 => CardDirection.Left,
        7 => CardDirection.UpLeft,
        _ => CardDirection.None
    };
}
