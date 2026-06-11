using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과(CardEffectBase)가 체인 실행 환경에 접근하기 위한 얇은 창구.
/// 효과 SO는 ChainExecutor 타입을 직접 참조하지 않고 이 컨텍스트만 통해 동작한다.
/// (순환 의존을 컨텍스트 경계에서 끊는다)
/// </summary>
public class EffectContext
{
    public CardView Card { get; private set; }
    public CardRuntimeState Runtime { get; private set; }
    public float Value { get; private set; }
    public float SecondaryValue { get; private set; }
    public CountScope Scope { get; private set; }
    public EffectTrigger Trigger { get; private set; }
    public HashSet<CardView> ActivatedCards { get; private set; }

    private readonly ChainExecutor _chain;

    public EffectContext(ChainExecutor chain)
    {
        _chain = chain;
    }

    /// <summary>한 효과 적용 직전에 호출해 컨텍스트를 세팅(재사용으로 할당 최소화).</summary>
    public void Setup(CardView card, float value, float secondaryValue,
        CountScope scope, EffectTrigger trigger, HashSet<CardView> activatedCards)
    {
        Card = card;
        Runtime = card != null ? card.GetComponent<CardRuntimeState>() : null;
        Value = value;
        SecondaryValue = secondaryValue;
        Scope = scope;
        Trigger = trigger;
        ActivatedCards = activatedCards;
    }

    // ── 자주 쓰는 파생값 ──────────────────────────────

    /// <summary>value를 1 이상 정수로 변환.</summary>
    public int IntValue => Mathf.Max(1, Mathf.RoundToInt(Value));

    /// <summary>런타임 누적 보너스가 적용된 데미지.</summary>
    public int GetModifiedDamage(int baseDamage) =>
        Runtime != null ? Runtime.GetModifiedDamage(baseDamage) : baseDamage;

    // ── 런타임 상태 조작 ──────────────────────────────

    public void AddBonusDamage(int amount) => Runtime?.AddBonusDamage(amount);
    public void SetBonusDamage(int amount) => Runtime?.SetBonusDamage(amount);
    public void DeductBonusDamage(int amount) => Runtime?.DeductBonusDamage(amount);

    // ── ChainExecutor 위임 ────────────────────────────

    public int TurnToggleCount => _chain.TurnToggleCount;

    public void DealDamage(int amount) => _chain.DealDamageToEnemyPublic(amount);

    public float GetTotemAdjusted(EffectType type, float baseValue) =>
        _chain.GetTotemAdjustedValue(Card, type, baseValue);

    /// <summary>임의 카드(이웃 등)에 대한 토템 보정값.</summary>
    public float GetTotemAdjustedFor(CardView target, EffectType type, float baseValue) =>
        _chain.GetTotemAdjustedValue(target, type, baseValue);

    public void ApplyPreserveToNeighbors(int amount) =>
        _chain.ApplyPreserveToNeighborsPublic(Card, amount);

    public IEnumerator RunExplodeChain() => _chain.RunExplodeChainPublic(Card);

    // ── 그리드/이웃 질의 (효과 계산용) ─────────────────

    /// <summary>8방향 인접 점유 칸 수 (인싸형 계산용).</summary>
    public int CountAdjacentOccupied()
    {
        if (Card?.CurrentSlot == null || GridManager.Instance == null) return 0;

        int count = 0;
        foreach (CardDirection dir in System.Enum.GetValues(typeof(CardDirection)))
        {
            if (dir == CardDirection.None) continue;
            GridSlot neighbor = GridManager.Instance.GetNeighbor(Card.CurrentSlot, dir);
            if (neighbor != null && !neighbor.IsEmpty) count++;
        }
        return count;
    }

    /// <summary>그리드 전체에서 ON 상태인 카드 수 (마무리형 계산용).</summary>
    public int CountActivatedOnGrid()
    {
        if (GridManager.Instance == null) return 0;

        int count = 0;
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            if (slot.OccupiedCard != null && slot.OccupiedCard.IsActivated)
                count++;
        return count;
    }
}
