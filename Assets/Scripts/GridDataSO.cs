using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "EpicPoC/Grid Data", fileName = "GridData")]
public class GridDataSO : ScriptableObject
{
    [Min(1)] public int rows = 1;
    [Min(1)] public int columns = 5;
    public List<Vector2Int> disabledCells = new();

    public bool IsCellEnabled(Vector2Int position)
    {
        if (position.x < 0 || position.y < 0 || position.x >= columns || position.y >= rows)
        {
            return false;
        }

        return disabledCells == null || !disabledCells.Contains(position);
    }
}
