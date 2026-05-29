using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyIntentType
{
    Attack,
    Defend,
}

[Serializable]
public class EnemyIntentData
{
    public EnemyIntentType type;
    public int value;
    [Min(1)] public int hits = 1; // 공격 횟수 (Attack 타입에서만 사용)
}

[Serializable]
public class EnemyIntentTurn
{
    public List<EnemyIntentData> intents = new();
}

[CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Identity")]
    public string enemyId;
    public string displayName = "Enemy";
    [Min(1)] public int maxHp = 20;
    public Sprite sprite;

    [Header("Intent Pattern")]
    [Tooltip("턴마다 순환. 한 턴에 여러 Intent 동시 실행 가능")]
    public List<EnemyIntentTurn> intentPattern = new();
}