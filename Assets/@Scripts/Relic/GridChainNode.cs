using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct GridDirectionRay
{
    public GridDirectionRay(GridSlot originSlot, CardDirection direction, int range)
    {
        OriginSlot = originSlot;
        Direction = direction;
        Range = Mathf.Max(1, range);
    }

    public GridSlot OriginSlot { get; }
    public CardDirection Direction { get; }
    public int Range { get; }
}

public interface IGridChainNode
{
    GridSlot CurrentSlot { get; }
    bool IsActivated { get; }
    bool IsEnemy { get; }
    string ChainDisplayName { get; }

    IEnumerable<GridDirectionRay> GetDirectionRays();
    void SetActivated(bool activated);
    IEnumerator PlayActivationFeedback(float duration);
}
