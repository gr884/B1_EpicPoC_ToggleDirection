using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 가방 우측에 배치된 패널.
/// BackpackManager.OnBackpackChanged 때마다 ChainSimulator를 호출해
/// 예상 효과 수치를 실시간으로 표시합니다.
/// </summary>
public class UI_ResultPanelView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text _damageText;
    [SerializeField] private TMP_Text _defenseText;
    [SerializeField] private TMP_Text _healText;
    // 효과 타입이 늘어나면 여기에 추가

    private void OnEnable()
    {
        BackpackManager.Instance.OnBackpackChanged += Refresh;
    }

    private void OnDisable()
    {
        if (BackpackManager.Instance != null)
            BackpackManager.Instance.OnBackpackChanged -= Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        Dictionary<EffectType, float> results =
            ChainExecutor.Instance.CalculateEffects(BackpackManager.Instance.PlacedItems);

        SetText(_damageText, "ATK", results, EffectType.Damage);
        SetText(_defenseText, "DEF", results, EffectType.Defense);
        SetText(_healText, "HEAL", results, EffectType.Heal);
    }

    private static void SetText(TMP_Text label, string prefix,
        Dictionary<EffectType, float> results, EffectType type)
    {
        if (label == null) return;
        float val = results.TryGetValue(type, out float v) ? v : 0f;
        label.text = $"{prefix}: {val:F0}";
    }
}