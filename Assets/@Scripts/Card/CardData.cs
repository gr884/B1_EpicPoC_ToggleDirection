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
    None,
    Row,
    Column,
    Cross,
    Total,
}

public enum RecallDestination
{
    Hand,        // 손패로
    DrawPileTop, // 드로우 파일 맨 위로
    DrawPile,    // 드로우 파일 랜덤 위치로
}

[Serializable]
public class CardEffect
{
    [Header("집계 범위")]
    public CountScope scope;

    [Header("발동 조건")]
    public int threshold;

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

    [Header("Recall")]
    public RecallDestination recallDestination = RecallDestination.Hand;

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