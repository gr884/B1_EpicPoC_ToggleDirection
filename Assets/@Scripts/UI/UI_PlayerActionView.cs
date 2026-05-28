using System.Collections.Generic;
using UnityEngine;

public class UI_PlayerActionView : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private UI_ActionEntry _entryPrefab;
    [SerializeField] private ActionIconSO _icons;

    private readonly List<UI_ActionEntry> _entries = new();

    private void Start()
    {
        ChainExecutor.Instance.OnStatsUpdated += OnStatsUpdated;
        BattleManager.Instance.OnPhaseChanged += OnPhaseChanged;
        Refresh(0, 0, 0);
    }

    private void OnDestroy()
    {
        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnStatsUpdated -= OnStatsUpdated;
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnStatsUpdated(ChainResult result)
    {
        Refresh(
            attack: Mathf.RoundToInt(result.damage),
            defend: Mathf.RoundToInt(result.defense),
            heal: Mathf.RoundToInt(result.heal)
        );
    }

    private void OnPhaseChanged(BattleManager.BattlePhase phase)
    {
        if (phase == BattleManager.BattlePhase.PlayerTurn)
            Refresh(0, 0, 0);
    }

    private void Refresh(int attack, int defend, int heal)
    {
        foreach (UI_ActionEntry entry in _entries)
            Destroy(entry.gameObject);
        _entries.Clear();

        if (attack > 0) AddEntry(_icons.attack, attack.ToString());
        if (defend > 0) AddEntry(_icons.defend, defend.ToString());
        if (heal > 0) AddEntry(_icons.heal, heal.ToString());
    }

    private void AddEntry(Sprite icon, string value)
    {
        UI_ActionEntry entry = Instantiate(_entryPrefab, _container);
        entry.Setup(icon, value);
        _entries.Add(entry);
    }
}