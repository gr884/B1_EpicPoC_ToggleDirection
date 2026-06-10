using System;
using System.Collections.Generic;
using UnityEngine;

public enum RelicEffectTiming
{
    OnPlaced,
    OnActivated,
    OnDeactivated,
    OnTurnStart,
    OnTurnEnd
}

public enum RelicStateCondition
{
    Any,
    On,
    Off
}

[Serializable]
public class RelicDirectionAnchor
{
    public Vector2Int localCell;
    public List<CardDirection> directions = new();
    [Min(1)] public int range = 1;
}

public readonly struct RelicDirectionRay
{
    public RelicDirectionRay(Vector2Int localCell, CardDirection direction, int range)
    {
        LocalCell = localCell;
        Direction = direction;
        Range = Mathf.Max(1, range);
    }

    public Vector2Int LocalCell { get; }
    public CardDirection Direction { get; }
    public int Range { get; }
}

[CreateAssetMenu(menuName = "Game/Relic Data", fileName = "RelicData")]
public class RelicData : ScriptableObject
{
    [Header("Identity")]
    public string relicId;
    public string displayName;
    [TextArea(2, 5)] public string description;

    [Header("Shape")]
    public Vector2Int boundsSize = Vector2Int.one;
    public List<Vector2Int> occupiedCells = new() { Vector2Int.zero };

    [Header("Directions")]
    public List<RelicDirectionAnchor> directionAnchors = new();

    [Header("Effects")]
    public List<RelicEffectSO> effects = new();

    [Header("Visual")]
    public Sprite icon;

    public IReadOnlyList<Vector2Int> GetOccupiedCells(int rotationSteps)
    {
        return RelicShapeUtility.RotateAndNormalizeCells(
            occupiedCells,
            boundsSize,
            rotationSteps,
            out _);
    }

    public IReadOnlyList<RelicDirectionRay> GetDirectionRays(int rotationSteps)
    {
        List<Vector2Int> sourceCells = occupiedCells != null && occupiedCells.Count > 0
            ? occupiedCells
            : new List<Vector2Int> { Vector2Int.zero };

        List<Vector2Int> rotatedCells = RelicShapeUtility.RotateAndNormalizeCells(
            sourceCells,
            boundsSize,
            rotationSteps,
            out Vector2Int minOffset);

        _ = rotatedCells;
        List<RelicDirectionRay> result = new();
        if (directionAnchors == null) return result;

        foreach (RelicDirectionAnchor anchor in directionAnchors)
        {
            if (anchor == null || anchor.directions == null) continue;

            Vector2Int localCell = RelicShapeUtility.RotateCell(anchor.localCell, boundsSize, rotationSteps) - minOffset;
            foreach (CardDirection direction in anchor.directions)
            {
                if (direction == CardDirection.None) continue;
                result.Add(new RelicDirectionRay(
                    localCell,
                    RelicShapeUtility.RotateDirection(direction, rotationSteps),
                    anchor.range));
            }
        }

        return result;
    }

    private void OnValidate()
    {
        boundsSize.x = Mathf.Max(1, boundsSize.x);
        boundsSize.y = Mathf.Max(1, boundsSize.y);

        if (occupiedCells == null || occupiedCells.Count == 0)
            occupiedCells = new List<Vector2Int> { Vector2Int.zero };
    }
}
