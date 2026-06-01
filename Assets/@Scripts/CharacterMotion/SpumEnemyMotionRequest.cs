using System;

public enum SpumEnemyMotionType
{
    Attack,
    Hit,
    Die
}

public class SpumEnemyMotionRequest
{
    public SpumEnemyMotionType MotionType { get; }
    public int HitCount { get; }
    public Action OnAttackImpact { get; }
    public bool IsComplete { get; private set; }

    public SpumEnemyMotionRequest(
        SpumEnemyMotionType motionType,
        int hitCount = 1,
        Action onAttackImpact = null)
    {
        MotionType = motionType;
        HitCount = Math.Max(1, hitCount);
        OnAttackImpact = onAttackImpact;
    }

    public void Complete()
    {
        IsComplete = true;
    }
}
