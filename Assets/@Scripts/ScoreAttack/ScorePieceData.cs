using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Score Attack/Piece Data", fileName = "ScorePieceData")]
public class ScorePieceData : ScriptableObject
{
    [Header("Identity")]
    public string displayName;

    [Header("Rules")]
    [Min(1)] public int maxDurability = 3;
    public Color pieceColor = new(0.15f, 0.85f, 1f, 1f);
    public Color signalColor = new(1f, 0.65f, 0.1f, 1f);

    [Header("Shape")]
    [Tooltip("Occupied cells relative to the placement anchor. Include (0, 0).")]
    public List<Vector2Int> occupiedOffsets = new() { Vector2Int.zero };

    [Header("Signals")]
    [Tooltip("Virtual signal positions relative to the placement anchor.")]
    public List<Vector2Int> signalOffsets = new();
    [Min(1)] public int minRandomSignals = 1;
    [Min(1)] public int maxRandomSignals = 2;

    public IEnumerable<Vector2Int> GetUniqueOccupiedOffsets()
    {
        HashSet<Vector2Int> unique = new();
        if (occupiedOffsets != null)
            foreach (Vector2Int offset in occupiedOffsets)
                if (unique.Add(offset))
                    yield return offset;

        if (unique.Count == 0)
            yield return Vector2Int.zero;
    }

    public static Vector2Int RotateOffset(Vector2Int offset, int quarterTurns)
    {
        int turns = ((quarterTurns % 4) + 4) % 4;
        return turns switch
        {
            1 => new Vector2Int(-offset.y, offset.x),
            2 => new Vector2Int(-offset.x, -offset.y),
            3 => new Vector2Int(offset.y, -offset.x),
            _ => offset
        };
    }

    public IEnumerable<Vector2Int> GetRotatedOccupiedOffsets(int quarterTurns)
    {
        HashSet<Vector2Int> unique = new();
        foreach (Vector2Int offset in GetUniqueOccupiedOffsets())
        {
            Vector2Int rotated = RotateOffset(offset, quarterTurns);
            if (unique.Add(rotated))
                yield return rotated;
        }
    }

    public IEnumerable<Vector2Int> GetRotatedSignalOffsets(int quarterTurns)
    {
        if (signalOffsets == null) yield break;

        HashSet<Vector2Int> occupied = new(GetRotatedOccupiedOffsets(quarterTurns));
        HashSet<Vector2Int> unique = new();
        foreach (Vector2Int offset in signalOffsets)
        {
            Vector2Int rotated = RotateOffset(offset, quarterTurns);
            if (!occupied.Contains(rotated) && unique.Add(rotated))
                yield return rotated;
        }
    }

    public List<Vector2Int> CreateRandomSignalOffsets()
    {
        HashSet<Vector2Int> occupied = new(GetUniqueOccupiedOffsets());
        HashSet<Vector2Int> uniqueCandidates = new();
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        foreach (Vector2Int body in occupied)
        foreach (Vector2Int direction in directions)
        {
            Vector2Int candidate = body + direction;
            if (!occupied.Contains(candidate))
                uniqueCandidates.Add(candidate);
        }

        List<Vector2Int> candidates = new(uniqueCandidates);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        int minimum = Mathf.Clamp(minRandomSignals, 1, candidates.Count);
        int maximum = Mathf.Clamp(maxRandomSignals, minimum, candidates.Count);
        int count = Random.Range(minimum, maximum + 1);
        return candidates.GetRange(0, count);
    }

    private void OnValidate()
    {
        maxDurability = Mathf.Max(1, maxDurability);
        minRandomSignals = Mathf.Max(1, minRandomSignals);
        maxRandomSignals = Mathf.Max(minRandomSignals, maxRandomSignals);
        if (occupiedOffsets == null)
            occupiedOffsets = new List<Vector2Int>();
        if (!occupiedOffsets.Contains(Vector2Int.zero))
            occupiedOffsets.Insert(0, Vector2Int.zero);

        if (signalOffsets == null) return;
        foreach (Vector2Int signal in signalOffsets)
            if (occupiedOffsets.Contains(signal))
                Debug.LogWarning($"[{name}] Signal {signal} overlaps this piece and will be ignored.", this);
    }
}
