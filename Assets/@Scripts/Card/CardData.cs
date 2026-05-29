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
    // 수치 효과 (누적)
    Damage,
    Defense,
    Heal,

    // 행동 효과 (즉시 실행)
    Draw,     // value = 드로우 장 수
    GainCost, // value = 획득 cost 수
    Preserve, // value = 방향으로 연결된 카드에 쌓을 보존 스택 수
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

public enum ThresholdType
{
    AtLeast,  // N개 이상 (같은 EffectType 중 가장 높은 조건만 적용)
    Full,     // 범위 전체가 켜졌을 때
}

[Serializable]
public class CardEffect
{
    [Header("집계 범위")]
    public CountScope scope;

    [Header("발동 조건")]
    public ThresholdType thresholdType;
    public int threshold; // AtLeast일 때만 사용

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

    [Header("Cost")]
    [Min(0)] public int cost = 1;

    [Header("Range")]
    [Min(1)] public int range = 1;

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