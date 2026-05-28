using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ActivationOrderView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform _content;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private GameObject _entryPrefab;

    private readonly List<GameObject> _entryObjects = new();
    private int _consumedCount; // 효과 실행으로 소비된 항목 수

    private void Start()
    {
        ChainExecutor.Instance.OnActivationOrderResolved += OnActivationOrderResolved;
        ChainExecutor.Instance.OnEffectStepProcessed     += OnEffectStepProcessed;
        BattleManager.Instance.OnTurnStateChanged        += OnTurnStateChanged;
    }

    private void OnDestroy()
    {
        if (ChainExecutor.Instance != null)
        {
            ChainExecutor.Instance.OnActivationOrderResolved -= OnActivationOrderResolved;
            ChainExecutor.Instance.OnEffectStepProcessed     -= OnEffectStepProcessed;
        }
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnTurnStateChanged -= OnTurnStateChanged;
    }

    // ── 이벤트 핸들러 ──────────────────────────────────────

    private void OnTurnStateChanged(BattleManager.TurnState state)
    {
        if (state == BattleManager.TurnState.PlayerTurn)
            ClearList();
    }

    // 체인 전파 완료 직후 — 효과 실행 전에 호출
    // 지연 카드만 큐에 추가 (즉시 발동 카드는 포함 안 됨)
    private void OnActivationOrderResolved(IReadOnlyList<CardView> order)
    {
        for (int i = 0; i < order.Count; i++)
        {
            CardView card = order[i];
            GameObject obj = Instantiate(_entryPrefab, _content);

            if (card?.Data != null)
            {
                TMP_Text label = obj.GetComponent<TMP_Text>();
                if (label != null)
                    label.text = BuildEntryText(_entryObjects.Count + 1, card);
            }

            _entryObjects.Add(obj);
        }

        if (order.Count > 0)
        {
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
                _scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }
    }

    // 효과 스텝 실행 시 다음 항목 순서대로 제거
    // (즉시 발동 효과도 이 이벤트를 발생시키지만 해당 항목은 큐에 없으므로 무시됨)
    private void OnEffectStepProcessed(int _)
    {
        while (_consumedCount < _entryObjects.Count && _entryObjects[_consumedCount] == null)
            _consumedCount++;

        if (_consumedCount >= _entryObjects.Count) return;

        Destroy(_entryObjects[_consumedCount]);
        _entryObjects[_consumedCount] = null;
        _consumedCount++;
    }

    // ── 내부 ───────────────────────────────────────────────

    private void ClearList()
    {
        foreach (GameObject obj in _entryObjects)
            if (obj != null) Destroy(obj);
        _entryObjects.Clear();
        _consumedCount = 0;
    }

    private static string BuildEntryText(int index, CardView card)
    {
        CardData data = card.Data;

        string typeTag = data.cardType switch
        {
            CardType.Buff       => "[BUFF]",
            CardType.Draw       => "[DRAW]",
            CardType.Multiplier => "[MULT]",
            _                   => "[ACT]"
        };

        if (data.effects == null || data.effects.Count == 0)
            return $"{index}. {data.displayName} {typeTag}";

        System.Text.StringBuilder sb = new();
        sb.Append($"{index}. {data.displayName} {typeTag}");
        foreach (CardEffect e in data.effects)
        {
            string effectLabel = e.effectType switch
            {
                EffectType.Damage   => "ATK",
                EffectType.Defense  => "DEF",
                EffectType.Heal     => "HEAL",
                EffectType.Draw     => "DRAW",
                EffectType.Multiply => "x",
                _ => e.effectType.ToString()
            };
            string suffix = e.effectType == EffectType.Multiply
                ? $"  {effectLabel}{e.value}"
                : $"  {effectLabel}+{e.value}";
            sb.Append(suffix);
        }
        return sb.ToString();
    }
}
