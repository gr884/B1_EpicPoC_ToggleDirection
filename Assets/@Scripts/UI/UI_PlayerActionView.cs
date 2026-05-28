using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_PlayerActionView : MonoBehaviour
{
    [Header("Fixed Shield UI (Optional)")]
    [SerializeField] private TMP_Text _shieldValueText;

    [Header("Fallback Dynamic Entries")]
    [SerializeField] private Transform _container;
    [SerializeField] private UI_ActionEntry _entryPrefab;
    [SerializeField] private ActionIconSO _icons;
    [SerializeField] private Player _player;

    private readonly List<UI_ActionEntry> _entries = new();

    private void Start()
    {
        if (_player == null)
            _player = FindFirstObjectByType<Player>();

        if (_player != null)
            _player.OnBlockChanged += OnBlockChanged;

        Refresh(_player != null ? _player.CurrentBlock : 0);
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.OnBlockChanged -= OnBlockChanged;
    }

    private void OnBlockChanged(int block)
    {
        Refresh(block);
    }

    private void Refresh(int block)
    {
        if (_shieldValueText != null)
        {
            _shieldValueText.text = block.ToString();
            return;
        }

        if (_container == null || _entryPrefab == null || _icons == null)
            return;

        foreach (UI_ActionEntry entry in _entries)
            Destroy(entry.gameObject);
        _entries.Clear();

        if (block > 0)
            AddEntry(_icons.defend, block.ToString());
    }

    private void AddEntry(Sprite icon, string value)
    {
        UI_ActionEntry entry = Instantiate(_entryPrefab, _container);
        entry.Setup(icon, value);
        _entries.Add(entry);
    }
}
