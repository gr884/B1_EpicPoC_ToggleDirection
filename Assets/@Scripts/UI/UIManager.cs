using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Cost")]
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private string _costFormat = "\uCF54\uC2A4\uD2B8 : [{0}/{1}]";

    [Header("Block")]
    [SerializeField] private Player _player;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private UI_ActionEntry _playerShieldEntry;
    [SerializeField] private UI_ActionEntry _enemyShieldEntry;
    [SerializeField] private TMP_Text _playerShieldValueText;
    [SerializeField] private TMP_Text _enemyShieldValueText;
    [SerializeField] private TMP_Text _blockText;
    [SerializeField] private TMP_Text _blockUsedText;
    [SerializeField] private TMP_Text _blockRemainingText;

    private int _lastPlayerBlock = int.MinValue;
    private int _lastEnemyBlock = int.MinValue;

    private void Start()
    {
        if (_player == null)
            _player = FindFirstObjectByType<Player>();
        if (_enemy == null)
            _enemy = FindFirstObjectByType<Enemy>();

        BindShieldEntriesIfNeeded();

        if (CardManager.Instance != null)
            CardManager.Instance.OnCostChanged += OnCostChanged;

        if (_player != null)
        {
            _player.OnBlockChanged += OnBlockChanged;
            _player.OnBlockConsumed += OnBlockConsumed;
        }
        if (_enemy != null)
        {
            _enemy.OnBlockChanged += OnEnemyBlockChanged;
            _enemy.OnBlockConsumed += OnEnemyBlockConsumed;
        }
        RefreshCost();
        RefreshBlock();
        RefreshEnemyBlock();
    }

    private void LateUpdate()
    {
        if (_player != null && _player.CurrentBlock != _lastPlayerBlock)
        {
            OnBlockChanged(_player.CurrentBlock);
            _lastPlayerBlock = _player.CurrentBlock;
        }

        if (_enemy != null && _enemy.CurrentBlock != _lastEnemyBlock)
        {
            OnEnemyBlockChanged(_enemy.CurrentBlock);
            _lastEnemyBlock = _enemy.CurrentBlock;
        }
    }

    private void OnDestroy()
    {
        if (CardManager.Instance != null)
            CardManager.Instance.OnCostChanged -= OnCostChanged;
        if (_player != null)
        {
            _player.OnBlockChanged -= OnBlockChanged;
            _player.OnBlockConsumed -= OnBlockConsumed;
        }
        if (_enemy != null)
        {
            _enemy.OnBlockChanged -= OnEnemyBlockChanged;
            _enemy.OnBlockConsumed -= OnEnemyBlockConsumed;
        }
    }

    private void OnCostChanged(int current, int max)
    {
        if (_costText == null) return;
        _costText.text = string.Format(_costFormat, current, max);
    }

    private void RefreshCost()
    {
        if (CardManager.Instance == null) return;
        OnCostChanged(CardManager.Instance.CurrentCost, CardManager.Instance.MaxCostPerTurn);
    }

    private void OnBlockChanged(int currentBlock)
    {
        if (_playerShieldEntry != null)
            _playerShieldEntry.SetValue(currentBlock.ToString());
        if (_playerShieldValueText != null)
            _playerShieldValueText.text = currentBlock.ToString();

        if (_blockText != null)
            _blockText.text = currentBlock.ToString();
    }

    private void OnBlockConsumed(int usedBlock, int remainingBlock)
    {
        if (_blockUsedText != null)
            _blockUsedText.text = usedBlock.ToString();
        if (_blockRemainingText != null)
            _blockRemainingText.text = remainingBlock.ToString();
    }

    private void OnEnemyBlockChanged(int currentBlock)
    {
        if (_enemyShieldEntry != null)
            _enemyShieldEntry.SetValue(currentBlock.ToString());
        if (_enemyShieldValueText != null)
            _enemyShieldValueText.text = currentBlock.ToString();
    }

    private void OnEnemyBlockConsumed(int usedBlock, int remainingBlock)
    {
        if (_enemyShieldEntry != null)
            _enemyShieldEntry.SetValue(remainingBlock.ToString());
        if (_enemyShieldValueText != null)
            _enemyShieldValueText.text = remainingBlock.ToString();
    }

    private void RefreshBlock()
    {
        if (_player == null) return;
        OnBlockChanged(_player.CurrentBlock);
        OnBlockConsumed(0, _player.CurrentBlock);
        _lastPlayerBlock = _player.CurrentBlock;
    }

    private void RefreshEnemyBlock()
    {
        if (_enemy == null) return;
        OnEnemyBlockChanged(_enemy.CurrentBlock);
        OnEnemyBlockConsumed(0, _enemy.CurrentBlock);
        _lastEnemyBlock = _enemy.CurrentBlock;
    }

    private void BindShieldEntriesIfNeeded()
    {
        if (_playerShieldEntry == null)
        {
            _playerShieldEntry = FindShieldEntryUnderOwner("PlayerInfo");
        }
        if (_playerShieldValueText == null)
        {
            _playerShieldValueText = FindShieldValueTextUnderOwner("PlayerInfo");
        }

        if (_enemyShieldEntry == null)
        {
            _enemyShieldEntry = FindShieldEntryUnderOwner("EnemyInfo");
        }
        if (_enemyShieldValueText == null)
        {
            _enemyShieldValueText = FindShieldValueTextUnderOwner("EnemyInfo");
        }
    }

    private UI_ActionEntry FindShieldEntryUnderOwner(string ownerName)
    {
        UI_ActionEntry[] entries = FindObjectsByType<UI_ActionEntry>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (UI_ActionEntry entry in entries)
        {
            if (entry == null || entry.gameObject.name != "ShieldInfo")
                continue;

            Transform current = entry.transform.parent;
            while (current != null)
            {
                if (current.name == ownerName)
                    return entry;
                current = current.parent;
            }
        }

        return null;
    }

    private TMP_Text FindShieldValueTextUnderOwner(string ownerName)
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            bool hasShieldInfo = false;
            bool hasOwner = false;

            Transform current = text.transform.parent;
            while (current != null)
            {
                if (current.name == "ShieldInfo")
                    hasShieldInfo = true;
                if (current.name == ownerName)
                {
                    hasOwner = true;
                    break;
                }
                current = current.parent;
            }

            if (hasShieldInfo && hasOwner)
                return text;
        }

        return null;
    }
}
