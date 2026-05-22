using System.Collections.Generic;
using UnityEngine;

public enum AbilityDirection
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

public enum CardTeam
{
    Ally,
    Enemy
}

[CreateAssetMenu(menuName = "EpicPoC/Card Data", fileName = "CardData")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;
    public string displayName;

    [Header("Ability")]
    public AbilityDirection abilityDirection = AbilityDirection.None;
    public List<AbilityDirection> additionalDirections = new();

    [Header("Visual")]
    public Sprite icon;

    public IEnumerable<AbilityDirection> GetAllDirections()
    {
        if (abilityDirection != AbilityDirection.None)
        {
            yield return abilityDirection;
        }

        if (additionalDirections == null)
        {
            yield break;
        }

        for (int i = 0; i < additionalDirections.Count; i++)
        {
            AbilityDirection dir = additionalDirections[i];
            if (dir == AbilityDirection.None || dir == abilityDirection)
            {
                continue;
            }

            yield return dir;
        }
    }
}
