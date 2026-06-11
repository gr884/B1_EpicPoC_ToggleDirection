using System.Collections.Generic;

public static class CardPlacementResolver
{
    public static bool TryResolveSlots(
        CardData data,
        GridSlot anchor,
        GridManager grid,
        out List<GridSlot> slots)
    {
        slots = new List<GridSlot>();
        if (data == null || anchor == null || grid == null) return false;

        HashSet<GridSlot> uniqueSlots = new();
        foreach (UnityEngine.Vector2Int offset in data.GetOccupiedOffsets())
        {
            GridSlot slot = grid.GetSlot(anchor.Position + offset);
            if (slot == null || !uniqueSlots.Add(slot))
                return false;
            slots.Add(slot);
        }
        return slots.Count > 0;
    }
}
