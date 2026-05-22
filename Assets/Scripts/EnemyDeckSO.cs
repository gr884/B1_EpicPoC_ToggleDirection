using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "EpicPoC/Enemy Deck", fileName = "EnemyDeck")]
public class EnemyDeckSO : ScriptableObject
{
    public List<CardData> cards = new();
}
