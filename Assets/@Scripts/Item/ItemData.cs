using System.Collections.Generic;
using UnityEngine;

public enum ItemDirection
{
    None,
    Up,
    UpRight, // 오른쪽 위 대각선
    Right,
    DownRight, // 오른쪽 아래 대각선
    Down,
    DownLeft, // 왼쪽 아래 대각선
    Left,
    UpLeft // 왼쪽 위 대각선
}

[CreateAssetMenu(menuName = "Game/Item Data", fileName = "ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string displayName;

    [Header("Ability")]
    public List<ItemDirection> directions = new();

    [Header("Visual")]
    public Sprite icon;

    public IEnumerable<ItemDirection> GetAllDirections()
    {
        foreach (ItemDirection dir in directions)
        {
            if (dir != ItemDirection.None)
                yield return dir;
        }
    }
}