using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Data Refs")]
    public CardDirectionData directionData;
    public CardEffectData effectData;

    [FormerlySerializedAs("displayName")]
    [SerializeField, HideInInspector] private string legacyDisplayName;
    [FormerlySerializedAs("abilityDirection")]
    [SerializeField, HideInInspector] private AbilityDirection legacyAbilityDirection = AbilityDirection.None;
    [FormerlySerializedAs("additionalDirections")]
    [SerializeField, HideInInspector] private List<AbilityDirection> legacyAdditionalDirections = new();
    [FormerlySerializedAs("icon")]
    [SerializeField, HideInInspector] private Sprite legacyIcon;

    public string displayName => directionData != null ? directionData.displayName : legacyDisplayName;
    public Sprite icon => directionData != null ? directionData.icon : legacyIcon;

    public int attackPower => effectData != null ? effectData.attackPower : 0;
    public int defensePower => effectData != null ? effectData.defensePower : 0;
    public int healPower => effectData != null ? effectData.healPower : 0;

    public IEnumerable<AbilityDirection> GetAllDirections()
    {
        if (directionData != null)
        {
            foreach (AbilityDirection dir in directionData.GetAllDirections())
            {
                yield return dir;
            }

            yield break;
        }

        if (legacyAbilityDirection != AbilityDirection.None)
        {
            yield return legacyAbilityDirection;
        }

        if (legacyAdditionalDirections == null)
        {
            yield break;
        }

        for (int i = 0; i < legacyAdditionalDirections.Count; i++)
        {
            AbilityDirection dir = legacyAdditionalDirections[i];
            if (dir == AbilityDirection.None || dir == legacyAbilityDirection)
            {
                continue;
            }

            yield return dir;
        }
    }
}
