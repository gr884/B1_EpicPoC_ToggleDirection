using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "EpicPoC/Card Direction Data", fileName = "CardDirectionData")]
public class CardDirectionData : ScriptableObject
{
    [Header("Visual")]
    public string displayName;
    public Sprite icon;
    [Tooltip("겹치기 순서: 리스트 앞이 아래, 뒤가 위")]
    public List<Sprite> layeredIcons = new();

    [Header("Ability Direction")]
    public AbilityDirection abilityDirection = AbilityDirection.None;
    public List<AbilityDirection> additionalDirections = new();

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
