using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 적의 다음 행동(인텐트)을 표시하는 UI (슬레이더스파이어 방식)
public class UI_EnemyStatsView : MonoBehaviour
{
    [SerializeField] private Image _intentIcon;
    [SerializeField] private TMP_Text _intentTypeText;
    [SerializeField] private TMP_Text _intentValueText;

    private void Start()
    {
        BattleManager.Instance.OnEnemyIntentChanged += Refresh;
        Refresh(BattleManager.Instance.GetCurrentEnemyIntent());
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnEnemyIntentChanged -= Refresh;
    }

    private void Refresh(EnemyAction action)
    {
        if (action == null) return;

        if (_intentIcon != null)
        {
            _intentIcon.sprite = action.icon;
            _intentIcon.enabled = action.icon != null;
        }

        if (_intentTypeText != null)
        {
            string label = string.IsNullOrEmpty(action.description)
                ? ActionTypeLabel(action.actionType)
                : action.description;
            _intentTypeText.text = label;
        }

        if (_intentValueText != null)
            _intentValueText.text = action.value > 0 ? action.value.ToString() : "";
    }

    private static string ActionTypeLabel(EnemyActionType type) => type switch
    {
        EnemyActionType.Attack => "ATK",
        EnemyActionType.Defense => "DEF",
        EnemyActionType.Buff => "BUFF",
        _ => type.ToString()
    };
}
