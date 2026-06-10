using UnityEngine;

public readonly struct RelicEffectContext
{
    public RelicEffectContext(RelicView relic, RelicEffectTiming timing)
    {
        Relic = relic;
        Timing = timing;
    }

    public RelicView Relic { get; }
    public RelicEffectTiming Timing { get; }
    public RelicData Data => Relic != null ? Relic.Data : null;
    public bool IsRelicOn => Relic != null && Relic.IsActivated;
}

public abstract class RelicEffectSO : ScriptableObject
{
    [Header("Trigger")]
    public RelicEffectTiming timing = RelicEffectTiming.OnActivated;
    public RelicStateCondition stateCondition = RelicStateCondition.Any;

    public bool CanApply(RelicEffectContext context)
    {
        if (context.Relic == null) return false;
        if (context.Timing != timing) return false;

        return stateCondition switch
        {
            RelicStateCondition.On => context.IsRelicOn,
            RelicStateCondition.Off => !context.IsRelicOn,
            _ => true
        };
    }

    public abstract void Apply(RelicEffectContext context);
}
