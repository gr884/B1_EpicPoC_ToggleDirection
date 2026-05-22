using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "EpicPoC/Level Data", fileName = "LevelData")]
public class LevelData : ScriptableObject
{
    [Min(1)] public int rows = 1;
    [Min(1)] public int columns = 5;

    [Header("Cards")]
    public List<PlacedCardSeed> prePlacedCards = new();
    public List<CardData> handCards = new();
}

[Serializable]
public class PlacedCardSeed
{
    public CardData card;
    public Vector2Int position;
    public bool startsActivated;
}
