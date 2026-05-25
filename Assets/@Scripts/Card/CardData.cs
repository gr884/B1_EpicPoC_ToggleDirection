using System;
using System.Collections.Generic;
using UnityEngine;

public enum CardDirection
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

public enum EffectType
{
    Damage,
    Defense,
    Heal,
}

public enum CountScope
{
    None,           // 조건 없이 On되면 즉시 발동
    Row,            // 같은 행에서 On된 개수
    Column,         // 같은 열에서 On된 개수
    Cross,          // 같은 행 + 같은 열에서 On된 개수 합산
    Total,          // 그리드 전체에서 On된 총 개수
}

[Serializable]
public class CardEffect
{
    [Header("집계 범위")]
    public CountScope scope;

    [Header("발동 조건")]
    public int threshold;   // N개 이상일 때 발동

    [Header("효과")]
    public EffectType effectType;
    public float value;
}

[CreateAssetMenu(menuName = "Game/Card Data", fileName = "CardData")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;
    public string displayName;
    [TextArea(2, 5)]
    public string description;

    [Header("Durability")]
    public int maxDurability = 5;

    [Header("Directions")]
    public List<CardDirection> directions = new();

    [Header("Effects")]
    public List<CardEffect> effects = new();

    [Header("Visual")]
    public Sprite icon;

    public IEnumerable<CardDirection> GetAllDirections()
    {
        foreach (CardDirection dir in directions)
            if (dir != CardDirection.None)
                yield return dir;
    }
}