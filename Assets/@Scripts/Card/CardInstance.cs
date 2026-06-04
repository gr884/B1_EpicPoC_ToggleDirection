using System;
using UnityEngine;

[Serializable]
public class CardPersistentState
{
    public int BonusDamage { get; private set; }

    public void AddBonusDamage(int amount)
    {
        BonusDamage += Mathf.Max(0, amount);
    }

    public void ClearCombatState()
    {
        BonusDamage = 0;
    }
}

public class CardInstance
{
    private static int s_nextInstanceId = 1;

    public CardInstance(CardData sourceData)
    {
        InstanceId = s_nextInstanceId++;
        SourceData = sourceData;
        PersistentState = new CardPersistentState();
    }

    public int InstanceId { get; }
    public CardData SourceData { get; }
    public CardPersistentState PersistentState { get; }
}
