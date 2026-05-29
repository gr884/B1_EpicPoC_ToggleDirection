using System.Collections.Generic;
using UnityEngine;

public class UI_EnemyActionView : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private UI_ActionEntry _entryPrefab;
    [SerializeField] private ActionIconSO _icons;
    [SerializeField] private Enemy _enemy;

    private readonly List<UI_ActionEntry> _entries = new();

    private void Start()
    {
        _enemy.OnIntentChanged += OnIntentChanged;
        OnIntentChanged(_enemy.CurrentIntentTurn);
    }

    private void OnDestroy()
    {
        if (_enemy != null)
            _enemy.OnIntentChanged -= OnIntentChanged;
    }

    private void OnIntentChanged(EnemyIntentTurn intentTurn)
    {
        foreach (UI_ActionEntry entry in _entries)
            Destroy(entry.gameObject);
        _entries.Clear();

        if (intentTurn == null) return;

        foreach (EnemyIntentData intent in intentTurn.intents)
        {
            Sprite icon = intent.type switch
            {
                EnemyIntentType.Attack => _icons.attack,
                EnemyIntentType.Defend => _icons.defend,
                EnemyIntentType.Contaminate => _icons.contaminate,
                _ => null
            };

            string valueText = intent.type == EnemyIntentType.Attack && intent.hits > 1
                ? $"{intent.value}x{intent.hits}"
                : intent.value > 0 ? intent.value.ToString() : "";

            AddEntry(icon, valueText);
        }
    }

    private void AddEntry(Sprite icon, string value)
    {
        UI_ActionEntry entry = Instantiate(_entryPrefab, _container);
        entry.Setup(icon, value);
        _entries.Add(entry);
    }
}