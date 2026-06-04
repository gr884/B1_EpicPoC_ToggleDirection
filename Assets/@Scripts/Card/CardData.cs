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

public class CardRuntimeData
{
    public string cardId;
    public string displayName;
    public string description;
    public int cost;
    public bool isUnplayable;
    public bool isCurseCard;
    public int curseDamage;
    public int range;
    public RecallDestination recallDestination;
    public List<CardDirection> directions = new();
    public List<CardEffect> effects = new();
    public Sprite icon;

    public static CardRuntimeData FromSource(CardData source)
    {
        CardRuntimeData runtime = new();
        if (source == null) return runtime;

        runtime.cardId = source.cardId;
        runtime.displayName = source.displayName;
        runtime.description = source.description;
        runtime.cost = source.cost;
        runtime.isUnplayable = source.isUnplayable;
        runtime.isCurseCard = source.isCurseCard;
        runtime.curseDamage = source.curseDamage;
        runtime.range = source.range;
        runtime.recallDestination = source.recallDestination;
        runtime.directions = new List<CardDirection>(source.directions ?? new List<CardDirection>());
        runtime.effects = CloneEffects(source.effects);
        runtime.icon = source.icon;

        return runtime;
    }

    public IEnumerable<CardDirection> GetAllDirections()
    {
        foreach (CardDirection dir in directions)
            if (dir != CardDirection.None)
                yield return dir;
    }

    public void AddDamageToDamageEffects(float amount)
    {
        foreach (CardEffect effect in effects)
            if (effect.effectType == EffectType.Damage)
                effect.value += amount;
    }

    private static List<CardEffect> CloneEffects(List<CardEffect> sourceEffects)
    {
        List<CardEffect> result = new();
        if (sourceEffects == null) return result;

        foreach (CardEffect effect in sourceEffects)
        {
            if (effect == null) continue;
            result.Add(new CardEffect
            {
                scope = effect.scope,
                thresholdType = effect.thresholdType,
                threshold = effect.threshold,
                effectType = effect.effectType,
                value = effect.value
            });
        }

        return result;
    }
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
