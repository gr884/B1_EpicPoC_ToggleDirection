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

// Inspector에서 체크박스로 8방위를 켜고 끄는 플래그
[Flags]
public enum CardDirectionFlags
{
    None      = 0,
    Up        = 1 << 0,
    UpRight   = 1 << 1,
    Right     = 1 << 2,
    DownRight = 1 << 3,
    Down      = 1 << 4,
    DownLeft  = 1 << 5,
    Left      = 1 << 6,
    UpLeft    = 1 << 7,
}

public enum CardType { Active, Buff, Draw, Multiplier }

public enum TriggerTiming
{
    OnTurnEnd,  // END 버튼 시 효과 실행 (기본값)
    Immediate,  // 신호전달 완료 직후 즉시 실행
}

public enum EffectType
{
    Damage,
    Defense,
    Heal,
    Draw,     // 덱에서 카드 드로우 (value = 장수)
    Multiply, // 다음 Active 카드 효과 배율 (value = 배수)
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
    [Header("Type")]
    public CardType cardType = CardType.Active;
    public TriggerTiming triggerTiming = TriggerTiming.OnTurnEnd;

    [Header("Durability")]
    [Tooltip("신호전달 가능 횟수. 0 = 무제한")]
    [Min(0)] public int maxDurability = 0;

    [Header("Identity")]
    public string cardId;
    public string displayName;
    [TextArea(2, 5)]
    public string description;

    [Header("Directions")]
    public CardDirectionFlags directionFlags;

    [Header("Effects")]
    public List<CardEffect> effects = new();

    [Header("Visual")]
    public Sprite icon;

    // 플래그 → 개별 CardDirection 열거
    public IEnumerable<CardDirection> GetAllDirections()
    {
        if ((directionFlags & CardDirectionFlags.Up)        != 0) yield return CardDirection.Up;
        if ((directionFlags & CardDirectionFlags.UpRight)   != 0) yield return CardDirection.UpRight;
        if ((directionFlags & CardDirectionFlags.Right)     != 0) yield return CardDirection.Right;
        if ((directionFlags & CardDirectionFlags.DownRight) != 0) yield return CardDirection.DownRight;
        if ((directionFlags & CardDirectionFlags.Down)      != 0) yield return CardDirection.Down;
        if ((directionFlags & CardDirectionFlags.DownLeft)  != 0) yield return CardDirection.DownLeft;
        if ((directionFlags & CardDirectionFlags.Left)      != 0) yield return CardDirection.Left;
        if ((directionFlags & CardDirectionFlags.UpLeft)    != 0) yield return CardDirection.UpLeft;
    }

    // directionFlags가 설정된 카드면 고정 방향 리스트 반환, 아니면 null(= 랜덤)
    public List<CardDirection> GetFixedDirections()
    {
        if (directionFlags == CardDirectionFlags.None) return null;

        var list = new List<CardDirection>();
        foreach (CardDirection dir in GetAllDirections())
            list.Add(dir);
        return list;
    }
}