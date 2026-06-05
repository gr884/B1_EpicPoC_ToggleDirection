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
    DirectionalDamageBonus, // 각 방향의 데미지 추가

    // 행동 효과 (즉시 실행)
    Draw,     // value = 드로우 장 수
    GainCost, // value = 획득 cost 수
    Preserve, // value = 방향으로 연결된 카드에 쌓을 보존 스택 수

    // 현재 수치에 효과 추가
    GainDamage,
    DecayDamage, // ON될 때마다 BonusDamage -N (0 아래로 안 내려감)

    // 배치 시 1회 실행
    OnPlaced, // 카드가 그리드에 놓이는 순간 발동 (체인 전)

    // 턴 종료 시 실행
    FinisherDamage, // 턴 종료 시 ON 상태라면 그리드 ON 카드 수 × value 데미지

    // 반전형: ON/OFF 상태에 따라 다른 효과
    DefenseOnOff, // ON → Damage(value), OFF → Defense(secondaryValue)

    // 카운터형: ON될 때 value + 이번 턴 그리드 전체 ON 횟수 데미지
    CounterDamage,

    // 폭발형: ON될 때 directions 방향 카드 강제 ON 후 소멸
    Explode,

    // 인싸형: ON될 때 인접 카드 수(8방향) × value 데미지
    PopularityDamage,

    // 자동 트리거: 체인 종료 후 그리드 ON 카드 수 >= threshold면 자동 ON
    AutoTrigger,

    // 재발동형: ON될 때 이번 턴 첫 번째로 놓인 카드 재발동
    Replay,
}

public enum CountScope
{
    None,
    Row,
    Column,
    Cross,
    Total,
    Self,     // 이번 턴에 이 카드가 ON된 횟수 (TurnOnCount)
    GridTotal, // 이번 턴 그리드 전체 ON 횟수 (_turnToggleCount)
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
    public float secondaryValue; // 보조 수치 (DefenseOnOff의 OFF 방어값 등)
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

    [Header("Curse")]
    public bool isUnplayable = false;  // 사용 불가 카드 (배치 거부)
    public bool isCurseCard = false;   // 손패에 있을 때 턴 종료 시 데미지
    public int curseDamage = 3;        // 손패에 있을 때 입히는 데미지 (isCurseCard = true일 때만 사용)
    public bool isRecaller = false;    // 점유된 슬롯에 배치 가능 (조작형)

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