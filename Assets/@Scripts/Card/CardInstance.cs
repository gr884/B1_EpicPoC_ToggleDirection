using System;
using UnityEngine;

[Serializable]
public class CardPersistentState
{
    public int BonusDamage { get; private set; }

    // 임계 활성화: 이번 턴에 ON된 횟수
    public int TurnOnCount { get; private set; }

    public void AddBonusDamage(int amount)
    {
        BonusDamage += Mathf.Max(0, amount);
    }

    public void SetBonusDamage(int amount)
    {
        BonusDamage = Mathf.Max(0, amount);
    }

    public void DeductBonusDamage(int amount)
    {
        BonusDamage = Mathf.Max(0, BonusDamage - Mathf.Max(0, amount));
    }

    public void IncrementTurnOnCount()
    {
        TurnOnCount++;
    }

    public void ResetTurnOnCount()
    {
        TurnOnCount = 0;
    }

    public void ClearCombatState()
    {
        BonusDamage = 0;
        TurnOnCount = 0;
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

    // 폭발형: 이번 전투에서 소멸 여부
    public bool IsExiled { get; private set; }
    public void Exile() => IsExiled = true;
    public void ResetExile() => IsExiled = false;
}