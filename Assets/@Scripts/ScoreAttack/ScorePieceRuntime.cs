using System.Collections.Generic;
using UnityEngine;

public sealed class ScorePieceRuntime
{
    private readonly List<Vector2Int> _signalOffsets;

    public ScorePieceRuntime(
        ScorePieceData data,
        Vector2Int anchor,
        int quarterTurns,
        Color color,
        IEnumerable<Vector2Int> signalOffsets)
    {
        Data = data;
        Anchor = anchor;
        QuarterTurns = ((quarterTurns % 4) + 4) % 4;
        Color = color;
        _signalOffsets = signalOffsets != null
            ? new List<Vector2Int>(signalOffsets)
            : new List<Vector2Int>();
        Durability = data != null ? data.maxDurability : 1;
        IsOn = true;
    }

    public ScorePieceData Data { get; }
    public Vector2Int Anchor { get; }
    public int QuarterTurns { get; }
    public Color Color { get; }
    public int Durability { get; private set; }
    public bool IsOn { get; private set; }
    public ScorePieceView View { get; set; }

    public IEnumerable<Vector2Int> OccupiedPositions
    {
        get
        {
            if (Data == null) yield break;
            foreach (Vector2Int offset in Data.GetRotatedOccupiedOffsets(QuarterTurns))
                yield return Anchor + offset;
        }
    }

    public IEnumerable<Vector2Int> SignalPositions
    {
        get
        {
            foreach (Vector2Int offset in RotatedSignalOffsets)
                yield return Anchor + offset;
        }
    }

    public IEnumerable<Vector2Int> RotatedSignalOffsets
    {
        get
        {
            foreach (Vector2Int offset in _signalOffsets)
                yield return ScorePieceData.RotateOffset(offset, QuarterTurns);
        }
    }

    public bool Toggle()
    {
        bool wasOn = IsOn;
        IsOn = !IsOn;
        if (wasOn && !IsOn)
            Durability = Mathf.Max(0, Durability - 1);
        return Durability <= 0;
    }
}
