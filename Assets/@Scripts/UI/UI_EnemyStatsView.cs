using TMPro;
using UnityEngine;

public class UI_EnemyStatsView : MonoBehaviour
{
    [SerializeField] private TMP_Text _attackText;
    [SerializeField] private TMP_Text _defenseText;

    private void Start()
    {
        ChainExecutor.Instance.OnStatsUpdated += OnStatsUpdated;
        ChainExecutor.Instance.OnChainStarted += Refresh;
        BattleManager.Instance.OnPhaseChanged += _ => Refresh();
        Refresh();
    }

    private void OnDestroy()
    {
        if (ChainExecutor.Instance == null) return;
        ChainExecutor.Instance.OnStatsUpdated -= OnStatsUpdated;
        ChainExecutor.Instance.OnChainStarted -= Refresh;
    }

    private void OnStatsUpdated(ChainResult _) => Refresh();

    private void Refresh()
    {
        int baseAtk = BattleManager.Instance.GetEnemyBaseDamage();
        int bonusAtk = BattleManager.Instance.GetEnemyCardBonus(EffectType.Damage);
        int bonusDef = BattleManager.Instance.GetEnemyCardBonus(EffectType.Defense);

        if (_attackText != null)
            _attackText.text = bonusAtk > 0 ? $"{baseAtk} + {bonusAtk}" : $"{baseAtk}";

        if (_defenseText != null)
            _defenseText.text = bonusDef.ToString();
    }
}