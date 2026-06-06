using TMPro;
using UnityEngine;

public class UI_BattleStageText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private BattleManager _battleManager;
    [SerializeField] private string _format = "STAGE {0}/{1}";
    [SerializeField] private string _emptyText = "";

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (_battleManager == null)
            _battleManager = BattleManager.Instance;

        if (_battleManager != null)
            _battleManager.OnStageChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
            _battleManager.OnStageChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_battleManager == null || !_battleManager.HasStageInfo)
        {
            SetText(_emptyText);
            return;
        }

        Refresh(_battleManager.CurrentStageNumber, _battleManager.TotalStageCount);
    }

    private void Refresh(int currentStage, int totalStage)
    {
        if (totalStage <= 0)
        {
            SetText(_emptyText);
            return;
        }

        SetText(string.Format(_format, currentStage, totalStage));
    }

    private void SetText(string value)
    {
        if (_text != null)
            _text.text = value;
    }
}
