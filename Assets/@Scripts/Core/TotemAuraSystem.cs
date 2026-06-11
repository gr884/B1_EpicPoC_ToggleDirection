using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 토템 오라(공격/수비 버프 장판)의 계산·시각화·적용을 전담합니다.
/// ChainExecutor에서 분리되었으며, ChainExecutor가 위임 형태로 호출합니다.
/// </summary>
public class TotemAuraSystem : MonoBehaviour
{
    private readonly Dictionary<CardView, int> _damageBonusByCard = new();
    private readonly Dictionary<CardView, int> _defenseBonusByCard = new();
    private readonly Dictionary<GridSlot, int> _damageOverlayStacks = new();
    private readonly Dictionary<GridSlot, int> _defenseOverlayStacks = new();

    public event Action OnChanged;

    public int GetDamageBonus(CardView card) =>
        (card != null && _damageBonusByCard.TryGetValue(card, out int b)) ? b : 0;

    public int GetDefenseBonus(CardView card) =>
        (card != null && _defenseBonusByCard.TryGetValue(card, out int b)) ? b : 0;

    public float GetAdjustedValue(CardView card, CardEffectBase effect, float baseValue)
    {
        if (effect == null || (!effect.ReceivesDamageTotemBonus && !effect.ReceivesDefenseTotemBonus))
            return baseValue;

        int bonus = effect.ReceivesDefenseTotemBonus
            ? GetDefenseBonus(card)
            : GetDamageBonus(card);
        return baseValue + bonus;
    }

    public void Refresh()
    {
        RebuildMaps();          // 범위 내 타겟 및 중첩수 연산
        ApplyOverlays();        // 그리드에 시각화
        NotifyBonusChanged();   // 타겟 카드들에 수치 적용 및 갱신
        OnChanged?.Invoke();
    }

    //* 토템 영역 계산
    private void RebuildMaps()
    {
        _damageBonusByCard.Clear();
        _defenseBonusByCard.Clear();
        _damageOverlayStacks.Clear();
        _defenseOverlayStacks.Clear();

        if (GridManager.Instance == null) return;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            // 켜진 기물이 아니면 스킵
            CardView source = slot.OccupiedCard;
            if (source == null || source.IsEnemy || !source.IsActivated || source.CurrentSlot == null) continue;

            // 공/수 토템 확인
            bool isDmgTotem = ContainsEffect(source.Data, effect => effect.IsDamageTotemAura);
            bool isDefTotem = ContainsEffect(source.Data, effect => effect.IsDefenseTotemAura);
            if (!isDmgTotem && !isDefTotem) continue;

            List<Vector2Int> offsets = source.Data.totemAuraOffsets;    // 해당 토템의 영역 오프셋을 가져옴
            if (offsets == null || offsets.Count == 0) continue;

            // 각 오프셋에 해당하는 그리드를 확인 및 계산
            foreach (Vector2Int offset in offsets)
            {
                // 타겟 슬롯 지정 및 확인
                GridSlot targetSlot = GridManager.Instance.GetSlot(source.CurrentSlot.Position + offset);
                if (targetSlot == null) continue;

                if (isDmgTotem)
                    _damageOverlayStacks[targetSlot] = _damageOverlayStacks.GetValueOrDefault(targetSlot, 0) + 1;
                if (isDefTotem)
                    _defenseOverlayStacks[targetSlot] = _defenseOverlayStacks.GetValueOrDefault(targetSlot, 0) + 1;

                CardView target = targetSlot.OccupiedCard;
                if (target == null || target.IsEnemy) continue;
                if (IsTotemCardData(target.Data)) continue;

                if (isDmgTotem)
                {
                    int auraValue = GetAuraValue(source.Data, effect => effect.IsDamageTotemAura);
                    if (_damageBonusByCard.TryGetValue(target, out int bonus))
                        _damageBonusByCard[target] = bonus + auraValue;
                    else
                        _damageBonusByCard[target] = auraValue;
                }

                if (isDefTotem)
                {
                    int auraValue = GetAuraValue(source.Data, effect => effect.IsDefenseTotemAura);
                    if (_defenseBonusByCard.TryGetValue(target, out int bonus))
                        _defenseBonusByCard[target] = bonus + auraValue;
                    else
                        _defenseBonusByCard[target] = auraValue;
                }
            }
        }
    }

    //* 토템의 영역 시각화
    private void ApplyOverlays()
    {
        if (GridManager.Instance == null) return;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            if (slot == null) continue;
            bool showDmg = _damageOverlayStacks.ContainsKey(slot);
            bool showDef = _defenseOverlayStacks.ContainsKey(slot);

            slot.SetTotemBorder(showDmg, showDef);
        }
    }

    private void NotifyBonusChanged()
    {
        if (GridManager.Instance == null) return;

        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            if (card == null || card.IsEnemy) continue;
            card.GetComponent<CardRuntimeState>()?.Refresh();
        }
    }

    private static int GetAuraValue(CardData data, Predicate<CardEffectBase> matchesAura)
    {
        if (data?.effects == null) return 0;

        int total = 0;
        foreach (CardEffect effect in data.effects)
        {
            CardEffectBase resolvedEffect = effect.ResolvedEffect;
            if (resolvedEffect == null || !matchesAura(resolvedEffect)) continue;
            total += Mathf.RoundToInt(effect.value);
        }

        return total;
    }

    public static bool IsTotemCardData(CardData data)
    {
        return ContainsEffect(data, effect => effect.IsDamageTotemAura || effect.IsDefenseTotemAura);
    }

    private static bool ContainsEffect(CardData data, Predicate<CardEffectBase> predicate)
    {
        if (data?.effects == null) return false;

        foreach (CardEffect effect in data.effects)
        {
            CardEffectBase resolvedEffect = effect.ResolvedEffect;
            if (resolvedEffect != null && predicate(resolvedEffect))
                return true;
        }

        return false;
    }
}
