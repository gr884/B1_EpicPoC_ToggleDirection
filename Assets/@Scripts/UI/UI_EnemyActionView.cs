using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_EnemyActionView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform _container;
    [SerializeField] private UI_ActionEntry _entryPrefab;
    [SerializeField] private ActionIconSO _icons;
    [SerializeField] private Enemy _enemy;

    private readonly List<UI_ActionEntry> _entries = new();
    private EnemyIntentTurn _currentIntentTurn;

    private void Start()
    {
        _enemy.OnIntentChanged += OnIntentChanged;
        OnIntentChanged(_enemy.CurrentIntentTurn);
    }

    private void OnDestroy()
    {
        if (_enemy != null)
            _enemy.OnIntentChanged -= OnIntentChanged;
    }

    private void OnIntentChanged(EnemyIntentTurn intentTurn)
    {
        _currentIntentTurn = intentTurn;

        foreach (UI_ActionEntry entry in _entries)
            Destroy(entry.gameObject);
        _entries.Clear();

        if (intentTurn == null) return;

        foreach (EnemyIntentData intent in intentTurn.intents)
        {
            Sprite icon = intent.type switch
            {
                EnemyIntentType.Attack => _icons.attack,
                EnemyIntentType.Defend => _icons.defend,
                EnemyIntentType.Contaminate => _icons.contaminate,
                _ => null
            };

            string valueText = intent.type == EnemyIntentType.Attack && intent.hits > 1
                ? $"{intent.value}x{intent.hits}"
                : intent.value > 0 ? intent.value.ToString() : "";

            AddEntry(icon, valueText);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentIntentTurn == null) return;
        string tooltip = BuildTooltipText(_currentIntentTurn);
        UI_IntentTooltip.Instance.Show("적의 의도", tooltip, Input.mousePosition);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UI_IntentTooltip.Instance.Hide();
    }

    private string BuildTooltipText(EnemyIntentTurn intentTurn)
    {
        StringBuilder sb = new();

        foreach (EnemyIntentData intent in intentTurn.intents)
        {
            switch (intent.type)
            {
                case EnemyIntentType.Attack:
                    if (intent.hits > 1)
                        sb.AppendLine($"{intent.value}의 피해를 {intent.hits}번 줍니다.");
                    else
                        sb.AppendLine($"{intent.value}의 피해를 줍니다.");
                    break;
                case EnemyIntentType.Defend:
                    sb.AppendLine($"{intent.value}의 방어를 합니다.");
                    break;
                case EnemyIntentType.Contaminate:
                    sb.AppendLine($"그리드에 오염 카드 {intent.spawnCount}개를 배치합니다.");
                    break;
            }
        }

        return sb.ToString().TrimEnd();
    }

    private void AddEntry(Sprite icon, string value)
    {
        UI_ActionEntry entry = Instantiate(_entryPrefab, _container);
        entry.Setup(icon, value);
        _entries.Add(entry);
    }
}