using UnityEngine;

public readonly struct CharacterMotionRequest
{
    public CharacterMotionRequest(
        CharacterMotionType motionType,
        CardView sourceCard,
        CardData cardData,
        EffectType effectType,
        Vector2Int gridPosition,
        int damageAmount = 0)
    {
        MotionType = motionType;
        SourceCard = sourceCard;
        CardData = cardData;
        EffectType = effectType;
        GridPosition = gridPosition;
        DamageAmount = damageAmount;
    }

    public CharacterMotionType MotionType { get; }
    public CardView SourceCard { get; }
    public CardData CardData { get; }
    public EffectType EffectType { get; }
    public Vector2Int GridPosition { get; }
    public int DamageAmount { get; }

    public CharacterMotionRequest WithDamageAmount(int damageAmount)
    {
        return new CharacterMotionRequest(
            MotionType,
            SourceCard,
            CardData,
            EffectType,
            GridPosition,
            damageAmount);
    }
}
