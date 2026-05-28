using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyActionType { Attack, Defense, Buff }

[Serializable]
public class EnemyAction
{
    public EnemyActionType actionType;
    [Min(0)] public int value;
    [Tooltip("인텐트 UI에 표시할 설명 텍스트")]
    public string description;
    public Sprite icon;
}

[CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    [Header("Identity")]
    public string enemyId;
    public string displayName = "Enemy";
    [Min(1)] public int maxHp = 20;
    public Sprite sprite;

    [Header("Action Pattern")]
    [Tooltip("순서대로 반복할 행동 패턴 (슬레이더스파이어 방식)")]
    public List<EnemyAction> actions = new();
}
