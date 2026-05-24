using System.Collections.Generic;
using UnityEngine;

public enum ItemDirection
{
    None,
    Up,
    UpRight,
    Right,
    DownRight,
    Down,
    DownLeft,
    Left,
    UpLeft
}

[CreateAssetMenu(menuName = "Game/Item Data", fileName = "ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string displayName;
    [TextArea(2, 5)]
    public string description;

    [Header("Ability")]
    public List<ItemDirection> directions = new();

    [Header("Effects")]
    public List<ItemEffect> effects = new();

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