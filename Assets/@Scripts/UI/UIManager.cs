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

    private void Start()
    {
        if (_player == null)
            _player = FindFirstObjectByType<Player>();
        if (_enemy == null)
            _enemy = FindFirstObjectByType<Enemy>();

        BindShieldEntriesIfNeeded();

        CardManager.Instance.OnCostChanged += OnCostChanged;
        if (_player != null)
        {
            _player.OnBlockChanged += OnBlockChanged;
        }
        if (_enemy != null)
        {
            _enemy.OnBlockChanged += OnEnemyBlockChanged;
        }
        RefreshCost();
        RefreshBlock();
        RefreshEnemyBlock();
    }

    private void OnDestroy()
    {
        if (CardManager.Instance != null)
            CardManager.Instance.OnCostChanged -= OnCostChanged;
        if (_player != null)
        {
            _player.OnBlockChanged -= OnBlockChanged;
        }
        if (_enemy != null)
        {
            _enemy.OnBlockChanged -= OnEnemyBlockChanged;
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
    }

    private void OnEnemyBlockChanged(int currentBlock)
    {
        if (_enemyShieldEntry != null)
            _enemyShieldEntry.SetValue(currentBlock.ToString());
    }

    private void RefreshBlock()
    {
        if (_player == null) return;
        OnBlockChanged(_player.CurrentBlock);
    }

    private void RefreshEnemyBlock()
    {
        if (_enemy == null) return;
        OnEnemyBlockChanged(_enemy.CurrentBlock);
    }

    private void BindShieldEntriesIfNeeded()
    {
        if (_playerShieldEntry == null)
        {
            GameObject playerShield = GameObject.Find("PlayerInfo/ShieldInfo");
            if (playerShield != null)
                _playerShieldEntry = playerShield.GetComponent<UI_ActionEntry>();
        }

        if (_enemyShieldEntry == null)
        {
            GameObject enemyShield = GameObject.Find("EnemyInfo/ShieldInfo");
            if (enemyShield != null)
                _enemyShieldEntry = enemyShield.GetComponent<UI_ActionEntry>();
        }
    }
}
