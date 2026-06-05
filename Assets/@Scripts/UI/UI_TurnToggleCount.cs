using TMPro;
using UnityEngine;

/// <summary>
/// 이번 턴 그리드 전체 ON 횟수를 표시하는 UI.
/// 별도 게임오브젝트에 붙여서 사용.
/// </summary>
public class UI_TurnToggleCount : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text _countText;

    [Header("Settings")]
    [SerializeField] private string _prefix = "ON 횟수: ";

    private void Start()
    {
        ChainExecutor.Instance.OnToggleCountChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnToggleCountChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_countText == null) return;
        _countText.text = $"{_prefix}{ChainExecutor.Instance.TurnToggleCount}";
    }
}