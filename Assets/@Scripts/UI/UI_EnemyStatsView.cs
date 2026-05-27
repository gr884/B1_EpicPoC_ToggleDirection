using TMPro;
using UnityEngine;

public class UI_EnemyStatsView : MonoBehaviour
{
    [SerializeField] private TMP_Text _attackText;
    [SerializeField] private TMP_Text _defenseText;
    [SerializeField] private Enemy _enemy;

    private void Start()
    {
        ChainExecutor.Instance.OnStatsUpdated += OnStatsUpdated;
        ChainExecutor.Instance.OnChainStarted += Refresh;
        BattleManager.Instance.OnPhaseChanged += OnPhaseChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (ChainExecutor.Instance != null)
        {
            ChainExecutor.Instance.OnStatsUpdated -= OnStatsUpdated;
            ChainExecutor.Instance.OnChainStarted -= Refresh;
        }
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnStatsUpdated(ChainResult _) => Refresh();
    private void OnPhaseChanged(BattleManager.BattlePhase _) => Refresh();

    private void Refresh()
    {
        if (_enemy == null) return;

        int bonusAtk = _enemy.GetCardBonus(EffectType.Damage);
        int bonusDef = _enemy.GetCardBonus(EffectType.Defense);

        if (_attackText != null) _attackText.text = bonusAtk.ToString();
        if (_defenseText != null) _defenseText.text = bonusDef.ToString();
    }
}