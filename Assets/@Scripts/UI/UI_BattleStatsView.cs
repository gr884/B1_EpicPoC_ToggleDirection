using TMPro;
using UnityEngine;

public class UI_BattleStatsView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private TMP_Text _defenseText;
    [SerializeField] private TMP_Text _healText;

    private void Start()
    {
        ChainExecutor.Instance.OnStatsUpdated += Refresh;
        ChainExecutor.Instance.OnChainStarted += ResetStats;
        ResetStats();
    }

    private void OnDestroy()
    {
        if (ChainExecutor.Instance == null) return;
        ChainExecutor.Instance.OnStatsUpdated -= Refresh;
        ChainExecutor.Instance.OnChainStarted -= ResetStats;
    }

    private void Refresh(ChainResult result)
    {
        if (_damageText != null) _damageText.text = Mathf.RoundToInt(result.damage).ToString();
        if (_defenseText != null) _defenseText.text = Mathf.RoundToInt(result.defense).ToString();
        if (_healText != null) _healText.text = Mathf.RoundToInt(result.heal).ToString();
    }

    private void ResetStats()
    {
        if (_damageText != null) _damageText.text = "0";
        if (_defenseText != null) _defenseText.text = "0";
        if (_healText != null) _healText.text = "0";
    }
}