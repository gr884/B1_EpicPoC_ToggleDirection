using System.Collections.Generic;
using UnityEngine;

public static class CardTargetResolver
{
    public static List<GridSlot> ResolveToggleSlots(CardData data, GridSlot origin, GridManager grid)
    {
        List<GridSlot> result = new();
        if (data == null || origin == null || grid == null) return result;

        if (data.remoteToggleOffsets != null && data.remoteToggleOffsets.Count > 0)
        {
            foreach (Vector2Int offset in data.remoteToggleOffsets)
            {
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
        return source == null
            ? new List<GridSlot>()
            : ResolveToggleSlots(source.Data, source.CurrentSlot, grid);
    }

    public static List<GridSlot> ResolveAdjacentArrowSlots(CardData data, GridSlot origin, GridManager grid)
    {
        List<GridSlot> result = new();
        if (data == null || origin == null || grid == null) return result;

        foreach (Vector2Int cellOffset in data.GetOccupiedOffsets())
        {
            GridSlot cellSlot = grid.GetSlot(origin.Position + cellOffset);
            if (cellSlot == null) continue;
            foreach (CardDirection direction in data.GetDirectionsAt(cellOffset))
                AddUnique(result, grid.GetNeighbor(cellSlot, direction));
        }
        return result;
    }

    private static void AddUnique(List<GridSlot> slots, GridSlot slot)
    {
        if (slot != null && !slots.Contains(slot))
            slots.Add(slot);
    }
}
