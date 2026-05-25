using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Identity")]
    public string enemyId;
    public string displayName = "Enemy";
    [Min(1)] public int maxHp = 20;
    [Min(0)] public int baseDamage = 5;
    public Sprite sprite;

    [Header("Card Placement")]
    public bool isRandom = true;
    [Tooltip("isRandom이 true일 때 배치할 카드 수")]
    public int randomCardCount = 3;
    [Tooltip("isRandom이 true일 때 사용할 카드 풀")]
    public List<CardData> randomCardPool = new();
    [Tooltip("isRandom이 false일 때 직접 설정한 배치")]
    public List<EnemyCardPlacement> fixedPlacements = new();

    [Header("Enemy Card Durability")]
    public int minDurability = 2;
    public int maxDurability = 5;
}

[Serializable]
public class EnemyCardPlacement
{
    public CardData cardData;
    public Vector2Int position;
}