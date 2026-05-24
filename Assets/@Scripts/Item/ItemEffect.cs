using System;
using UnityEngine;

public enum EffectType
{
    Damage,
    Defense,
    Heal,
}

public enum CountScope
{
    Row,            // 같은 행에서 On된 개수
    Column,         // 같은 열에서 On된 개수
    Total,          // 이 아이템을 시발점으로 On된 총 개수
}

public enum ConditionType
{
    AtLeast,   // N개 이상
}

[Serializable]
public class ItemEffect
{
    [Header("집계 범위")]
    public CountScope scope;

    [Header("발동 조건")]
    public int threshold;       // N개 이상일 때 발동

    [Header("효과")]
    public EffectType effectType;
    public float value;         // 조건 충족 시 적용값
}