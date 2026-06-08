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

public enum EffectTrigger
{
    OnActivated, // ON될 때 (기본)
    OnTurnEnd,   // 턴 종료 시
    OnPlaced,    // 배치 시 (체인 전)
}

public enum EffectType
{
    // 수치 효과 (누적)
    Damage,
    Defense,
    Heal,
    DirectionalDamageBonus,

    // 행동 효과
    Draw,     // value = 드로우 장 수
    GainCost, // value = 획득 cost 수
    Preserve, // value = 방향으로 연결된 카드에 쌓을 보존 스택 수

    // 누적 데미지
    GainDamage,  // BonusDamage +N
    DecayDamage, // BonusDamage -N (0 아래로 안 내려감)

    // 반전형: ON → Damage(value), OFF → Defense(secondaryValue)
    DefenseOnOff,

    // 카운터형: value + 이번 턴 그리드 전체 ON 횟수 데미지
    CounterDamage,

    // 폭발형: directions 방향 카드 강제 ON 후 소멸
    Explode,

    // 인싸형: 인접 카드 수(8방향) × value 데미지
    PopularityDamage,

    // 재발동형: 이번 턴 첫 번째로 놓인 카드 재발동
    Replay,

    // 마무리형: 턴 종료 시 ON 상태라면 그리드 ON 카드 수 × value 데미지
    FinisherDamage,

    // 소진형 초기화: 배치 시 BonusDamage를 value로 세팅 (누적 아닌 덮어쓰기)
    InitDamage,
    TotemAura,
    TotemAura_Defense
}

public enum CountScope
{
    None,
    Row,
    Column,
    Cross,
    Total,
    Self,      // 이번 턴에 이 카드가 ON된 횟수 (TurnOnCount)
    GridTotal, // 이번 턴 그리드 전체 ON 횟수 (_turnToggleCount)
}

public enum RecallDestination
{
    Hand,
    DrawPileTop,
    DrawPile,
}

public enum ThresholdType
{
    AtLeast,
    Full,
}

[Serializable]
public class CardEffect
{
    [Header("발동 타이밍")]
    public EffectTrigger trigger; // OnActivated / OnTurnEnd / OnPlaced

    [Header("집계 범위")]
    public CountScope scope;

    [Header("발동 조건")]
    public ThresholdType thresholdType;
    public int threshold;

    [Header("효과")]
    public EffectType effectType;
    public float value;
    public float secondaryValue; // DefenseOnOff의 OFF 방어값 등
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
    public bool isUnplayable = false;
    public bool isCurseCard = false;
    public int curseDamage = 3;
    public bool isRecaller = false; // 점유된 슬롯에 배치 가능 (조작형)

    [Header("Auto Trigger")]
    public bool hasAutoTrigger = false;          // 체인 종료 후 조건 충족 시 자동 ON
    public int autoTriggerThreshold = 1;         // 그리드 ON 카드 수 기준

    [Header("Range")]
    [Min(1)] public int range = 1;

    [Header("Recall")]
    public RecallDestination recallDestination = RecallDestination.Hand;

    [Header("Directions")]
    public List<CardDirection> directions = new();

    [Header("Effects")]
    public List<CardEffect> effects = new();

    [Header("Totem Aura")]
    public List<Vector2Int> totemAuraOffsets = new();

    [Header("Visual")]
    public Sprite icon;

    public IEnumerable<CardDirection> GetAllDirections()
    {
        foreach (CardDirection dir in directions)
            if (dir != CardDirection.None)
                yield return dir;
    }
}
