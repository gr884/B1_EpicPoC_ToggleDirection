using System.Collections.Generic;
using UnityEngine;

public static class CardTargetResolver
{
    public static List<GridSlot> ResolveToggleSlots(
        CardData data,
        GridSlot origin,
        GridManager grid)
    {
        List<GridSlot> result = new();
        if (data == null || origin == null || grid == null)
            return result;

        // Explicit offsets replace the legacy direction/range targeting.
        if (data.remoteToggleOffsets != null && data.remoteToggleOffsets.Count > 0)
        {
            foreach (Vector2Int offset in data.remoteToggleOffsets)
            {
                // (0, 0) is the source card itself and must not toggle it again.
                if (offset == Vector2Int.zero) continue;

                AddUnique(result, grid.GetSlot(origin.Position + offset));
            }

            return result;
        }

        int range = Mathf.Max(1, data.range);
        foreach (Vector2Int cellOffset in data.GetOccupiedOffsets())
        {
            GridSlot cellSlot = grid.GetSlot(origin.Position + cellOffset);
            if (cellSlot == null) continue;

            foreach (CardDirection direction in data.GetDirectionsAt(cellOffset))
            {
                GridSlot current = cellSlot;
                for (int i = 0; i < range; i++)
                {
                    current = grid.GetNeighbor(current, direction);
                    if (current == null) break;

                    AddUnique(result, current);
                }
            }
        }

        return result;
    }

    public static List<GridSlot> ResolveToggleSlots(CardView source, GridManager grid)
    {
        if (source == null)
            return new List<GridSlot>();

        return ResolveToggleSlots(source.Data, source.CurrentSlot, grid);
    }

    private static void AddUnique(List<GridSlot> slots, GridSlot slot)
    {
        if (slot != null && !slots.Contains(slot))
            slots.Add(slot);
    }
}
