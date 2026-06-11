using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 카드 효과의 추상 베이스. 효과 하나당 이 클래스를 상속한 SO 하나.
/// 새 효과 추가 = 이 클래스를 상속한 클래스 작성 + SO 에셋 생성.
/// </summary>
public abstract class CardEffectBase : ScriptableObject
{
    public string EffectKey => GetType().FullName;

    public bool TryGetLegacyEffectCode(out int effectCode)
    {
        return LegacyTypesByClass.TryGetValue(GetType(), out effectCode);
    }

    public virtual bool ReceivesDamageTotemBonus =>
        this is DamageEffect
        || this is DirectionalDamageBonusEffect
        || this is CounterDamageEffect
        || this is PopularityDamageEffect
        || this is FinisherDamageEffect
        || this is DefenseOnOffEffect
        || this is CastingDamageEffect;

    public virtual bool ReceivesDefenseTotemBonus => this is DefenseEffect || this is CastingDefenseEffect;
    public virtual bool IsDamageTotemAura => this is TotemAura;
    public virtual bool IsDefenseTotemAura => this is TotemAuraDefense;
    public virtual bool IsDirectionalDamageBonus => this is DirectionalDamageBonusEffect;
    public virtual bool IsDamage => this is DamageEffect;
    public virtual bool IsDefenseOnOff => this is DefenseOnOffEffect;
    public virtual bool IsCounterDamage => this is CounterDamageEffect;
    public virtual bool IsPopularityDamage => this is PopularityDamageEffect;
    public virtual bool IsFinisherDamage => this is FinisherDamageEffect;
    public virtual bool IsDefense => this is DefenseEffect;
    public virtual bool IsHeal => this is HealEffect;
    public virtual bool IsDevour => this is DevourEffect;
    public virtual bool IsCastingDamage => this is CastingDamageEffect;
    public virtual bool IsCastingDefense => this is CastingDefenseEffect;
    public virtual bool IsInitDamage => this is InitDamageEffect;
    public virtual bool IsReplay => this is ReplayEffect;

    public bool Matches(CardEffectBase other)
    {
        return other != null && GetType() == other.GetType();
    }

    /// <summary>효과 실행. ctx를 통해 체인 환경에 접근한다.</summary>
    public abstract IEnumerator Apply(EffectContext ctx);

    private const int LegacyDamage = 0;
    private const int LegacyDefense = 1;
    private const int LegacyHeal = 2;
    private const int LegacyDirectionalDamageBonus = 3;
    private const int LegacyDraw = 4;
    private const int LegacyGainCost = 5;
    private const int LegacyPreserve = 6;
    private const int LegacyGainDamage = 7;
    private const int LegacyDecayDamage = 8;
    private const int LegacyDefenseOnOff = 9;
    private const int LegacyCounterDamage = 10;
    private const int LegacyExplode = 11;
    private const int LegacyPopularityDamage = 12;
    private const int LegacyReplay = 13;
    private const int LegacyFinisherDamage = 14;
    private const int LegacyInitDamage = 15;
    private const int LegacyTotemAura = 16;
    private const int LegacyTotemAuraDefense = 17;
    private const int LegacyDevour = 18;
    private const int LegacyCastingDamage = 19;
    private const int LegacyCastingDefense = 20;

    private static readonly Dictionary<int, CardEffectBase> LegacyEffects = new();
    private static readonly Dictionary<Type, int> LegacyTypesByClass = new()
    {
        { typeof(DamageEffect), LegacyDamage },
        { typeof(DefenseEffect), LegacyDefense },
        { typeof(HealEffect), LegacyHeal },
        { typeof(DirectionalDamageBonusEffect), LegacyDirectionalDamageBonus },
        { typeof(DrawEffect), LegacyDraw },
        { typeof(GainCostEffect), LegacyGainCost },
        { typeof(PreserveEffect), LegacyPreserve },
        { typeof(GainDamageEffect), LegacyGainDamage },
        { typeof(DecayDamageEffect), LegacyDecayDamage },
        { typeof(DefenseOnOffEffect), LegacyDefenseOnOff },
        { typeof(CounterDamageEffect), LegacyCounterDamage },
        { typeof(ExplodeEffect), LegacyExplode },
        { typeof(PopularityDamageEffect), LegacyPopularityDamage },
        { typeof(ReplayEffect), LegacyReplay },
        { typeof(FinisherDamageEffect), LegacyFinisherDamage },
        { typeof(InitDamageEffect), LegacyInitDamage },
        { typeof(TotemAura), LegacyTotemAura },
        { typeof(TotemAuraDefense), LegacyTotemAuraDefense },
        { typeof(DevourEffect), LegacyDevour },
        { typeof(CastingDamageEffect), LegacyCastingDamage },
        { typeof(CastingDefenseEffect), LegacyCastingDefense },
    };

    public static CardEffectBase FromLegacyEffectCode(int effectCode)
    {
        if (LegacyEffects.TryGetValue(effectCode, out CardEffectBase effect))
            return effect;

        effect = CreateLegacyEffect(effectCode);
        if (effect != null)
        {
            effect.hideFlags = HideFlags.HideAndDontSave;
            LegacyEffects.Add(effectCode, effect);
        }

        return effect;
    }

    private static CardEffectBase CreateLegacyEffect(int effectCode)
    {
        return effectCode switch
        {
            LegacyDamage => CreateInstance<DamageEffect>(),
            LegacyDefense => CreateInstance<DefenseEffect>(),
            LegacyHeal => CreateInstance<HealEffect>(),
            LegacyDirectionalDamageBonus => CreateInstance<DirectionalDamageBonusEffect>(),
            LegacyDraw => CreateInstance<DrawEffect>(),
            LegacyGainCost => CreateInstance<GainCostEffect>(),
            LegacyPreserve => CreateInstance<PreserveEffect>(),
            LegacyGainDamage => CreateInstance<GainDamageEffect>(),
            LegacyDecayDamage => CreateInstance<DecayDamageEffect>(),
            LegacyDefenseOnOff => CreateInstance<DefenseOnOffEffect>(),
            LegacyCounterDamage => CreateInstance<CounterDamageEffect>(),
            LegacyExplode => CreateInstance<ExplodeEffect>(),
            LegacyPopularityDamage => CreateInstance<PopularityDamageEffect>(),
            LegacyReplay => CreateInstance<ReplayEffect>(),
            LegacyFinisherDamage => CreateInstance<FinisherDamageEffect>(),
            LegacyInitDamage => CreateInstance<InitDamageEffect>(),
            LegacyTotemAura => CreateInstance<TotemAura>(),
            LegacyTotemAuraDefense => CreateInstance<TotemAuraDefense>(),
            LegacyDevour => CreateInstance<DevourEffect>(),
            LegacyCastingDamage => CreateInstance<CastingDamageEffect>(),
            LegacyCastingDefense => CreateInstance<CastingDefenseEffect>(),
            _ => null,
        };
    }
}
