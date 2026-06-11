using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    OnTurnStart, // 턴 시작 시
    OnPlaced,    // 배치 시 (체인 전)
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
    public EffectTrigger trigger; // OnActivated / OnTurnEnd / OnTurnStart / OnPlaced

    [Header("집계 범위")]
    public CountScope scope;

    [Header("발동 조건")]
    public ThresholdType thresholdType;
    public int threshold;

    [Header("효과")]
    public CardEffectBase effect; // 효과 SO 에셋을 드래그해 지정
    [FormerlySerializedAs("effectType")]
    [SerializeField, HideInInspector] private int _legacyEffectType = -1;
    public float value;
    public float secondaryValue; // DefenseOnOff의 OFF 방어값 등

    // 기존 effectType 직렬화 숫자를 새 효과 SO 구조로 읽기 위한 호환 접근자
    public CardEffectBase ResolvedEffect => effect != null
        ? effect
        : _legacyEffectType >= 0
            ? CardEffectBase.FromLegacyEffectCode(_legacyEffectType)
            : null;
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

    [Header("Casting")]
    public bool isCastingCard = false;
    public int castingRequiredCount = 3;

    [Header("Capacitor")]
    public bool isCapacitorCard = false;
    public int capacitorChargeRequired = 3;  // ON이 되기 위한 트리거 횟수
    public int capacitorDischargeCount = 1;  // ON 후 화살표 방향 트리거 실행 횟수 N

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
