using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TutorialPlacementRule
{
    public CardData card;
    public Vector2Int slot;
}

[Serializable]
public class TutorialStepRules
{
    public TutorialStep step;
    public List<TutorialPlacementRule> placementRules = new();
    [TextArea(2, 4)]
    public string wrongCardMessage;
    [Tooltip("드래그 자체를 막을 카드 목록")]
    public List<CardData> blockedCards = new();
    [Tooltip("이 단계에서 활성화할 오브젝트 목록")]
    public List<GameObject> highlightObjects = new();
}

public enum TutorialStep
{
    Intro,
    Turn1_Place,
    Turn1_Cost,     // Cost 설명
    Turn1_Chain,
    Turn1_Confirm,
    Turn2_Intro,
    Turn2_Place,
    Complete,
}

public class TutorialManager : SingletonBehaviour<TutorialManager>
{
    [Header("Tutorial Enemy")]
    [SerializeField] private EnemyDataSO _tutorialEnemyData;

    [Header("Tutorial Deck")]
    [Tooltip("튜토리얼에서 사용할 카드 목록 (순서대로 손패에 들어옴)")]
    [SerializeField] private List<CardData> _tutorialDeck = new();

    [Header("Step Placement Rules")]
    [Tooltip("각 단계별 허용할 카드+슬롯 조합")]
    [SerializeField] private List<TutorialStepRules> _stepRules = new();

    public TutorialStep CurrentStep { get; private set; }
    public bool IsActive => GameManager.Instance.CurrentState == GameManager.GameState.Tutorial;

    public event Action<TutorialStep> OnStepChanged;
    public event Action<string> OnWrongAction; // 잘못된 시도 시 피드백 메시지

    // ── 초기화 ─────────────────────────────────────────────

    public void Init()
    {
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        Debug.Log("[TutorialManager] Init");
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Tutorial)
            StartTutorial();
    }

    // ── 튜토리얼 시작 ──────────────────────────────────────

    private void StartTutorial()
    {
        BattleManager.Instance.Player.Setup(5);
        BattleManager.Instance.Player.SetHandSize(3);
        CardManager.Instance.SetTutorialDeck(_tutorialDeck);
        GridManager.Instance.BuildGrid();
        CardManager.Instance.DrawToHand(3);
        BattleManager.Instance.StartTutorialBattle(_tutorialEnemyData);
        EnterStep(TutorialStep.Intro);
    }

    // ── 단계 진행 ──────────────────────────────────────────

    public void EnterStep(TutorialStep step)
    {
        // 이전 단계 오브젝트 끄기
        TutorialStepRules prevRules = GetStepRules(CurrentStep);
        if (prevRules != null)
            foreach (GameObject obj in prevRules.highlightObjects)
                if (obj != null) obj.SetActive(false);

        CurrentStep = step;

        // 새 단계 오브젝트 켜기
        TutorialStepRules nextRules = GetStepRules(CurrentStep);
        if (nextRules != null)
            foreach (GameObject obj in nextRules.highlightObjects)
                if (obj != null) obj.SetActive(true);

        RefreshHighlights();
        OnStepChanged?.Invoke(step);
        Debug.Log($"[TutorialManager] Step → {step}");
    }

    // ── 허용 체크 ──────────────────────────────────────────

    public bool CanPlaceCard(CardView card, GridSlot slot)
    {
        if (!IsActive) return true;

        TutorialStepRules stepRules = GetStepRules(CurrentStep);
        if (stepRules == null) return false;

        // blockedCards에 있으면 배치도 차단
        if (stepRules.blockedCards.Contains(card.Data))
        {
            if (!string.IsNullOrEmpty(stepRules.wrongCardMessage))
                OnWrongAction?.Invoke(stepRules.wrongCardMessage);
            return false;
        }

        foreach (TutorialPlacementRule rule in stepRules.placementRules)
            if (rule.card == card.Data && rule.slot == slot.Position)
                return true;

        return false;
    }

    public bool CanConfirm()
    {
        if (!IsActive) return true;
        return CurrentStep == TutorialStep.Turn1_Confirm;
    }

    private void RefreshHighlights()
    {
        ClearAllHighlights();

        TutorialStepRules stepRules = GetStepRules(CurrentStep);
        if (stepRules == null) return;

        // placementRules + blockedCards 모두 하이라이트
        foreach (CardView card in CardManager.Instance.Hand)
        {
            bool shouldHighlight = false;

            foreach (TutorialPlacementRule rule in stepRules.placementRules)
            {
                if (card.Data == rule.card) { shouldHighlight = true; break; }
            }

            if (!shouldHighlight && stepRules.blockedCards.Contains(card.Data))
                shouldHighlight = true;

            card.SetHighlight(shouldHighlight);
        }
    }

    private void ClearAllHighlights()
    {
        foreach (CardView card in CardManager.Instance.Hand)
            card.SetHighlight(false);
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            slot.SetHighlight(false);
    }

    /// <summary>드래그 시작 전 막힌 카드인지 체크. true면 드래그 허용, false면 차단.</summary>
    public bool CanDragCard(CardView card)
    {
        if (!IsActive) return true;

        TutorialStepRules stepRules = GetStepRules(CurrentStep);
        if (stepRules == null) return true;

        if (stepRules.blockedCards.Contains(card.Data))
        {
            if (!string.IsNullOrEmpty(stepRules.wrongCardMessage))
                OnWrongAction?.Invoke(stepRules.wrongCardMessage);
            return false;
        }

        return true;
    }

    public void OnCardDragBegin(CardView card)
    {
        if (!IsActive) return;

        List<TutorialPlacementRule> rules = GetRulesForStep(CurrentStep);
        if (rules == null) return;

        // 카드 하이라이트 해제 + 목표 슬롯 하이라이트
        card.SetHighlight(false);
        foreach (TutorialPlacementRule rule in rules)
        {
            if (rule.card == card.Data)
            {
                GridSlot slot = GridManager.Instance.GetSlot(rule.slot);
                if (slot != null) slot.SetHighlight(true);
            }
        }
    }

    public void OnCardDragCancelled(CardView card)
    {
        if (!IsActive) return;

        // 슬롯 하이라이트 해제 + 카드 하이라이트 복원
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
            slot.SetHighlight(false);
        RefreshHighlights();
    }

    private List<TutorialPlacementRule> GetRulesForStep(TutorialStep step)
    {
        foreach (TutorialStepRules rules in _stepRules)
            if (rules.step == step)
                return rules.placementRules;
        return null;
    }

    private TutorialStepRules GetStepRules(TutorialStep step)
    {
        foreach (TutorialStepRules rules in _stepRules)
            if (rules.step == step)
                return rules;
        return null;
    }

    // ── 이벤트 감지 ────────────────────────────────────────

    public void OnCardPlaced(CardView card)
    {
        if (!IsActive) return;

        switch (CurrentStep)
        {
            case TutorialStep.Turn1_Place:
                EnterStep(TutorialStep.Turn1_Cost);
                break;
            case TutorialStep.Turn1_Chain:
                if (CardManager.Instance.HandCount == 0)
                    EnterStep(TutorialStep.Turn1_Confirm);
                break;
            case TutorialStep.Turn2_Place:
                TutorialStepRules stepRules = GetStepRules(TutorialStep.Turn2_Place);
                if (stepRules != null)
                {
                    bool wasBlocked = stepRules.blockedCards.Contains(card.Data);
                    if (!wasBlocked)
                    {
                        stepRules.blockedCards.Clear();
                        RefreshHighlights();
                    }
                }
                break;
        }
    }

    /// <summary>DiscardAndDraw 후 손패가 새로 드로우됐을 때 호출</summary>
    public void OnHandDrawn()
    {
        if (!IsActive) return;
        RefreshHighlights();
    }

    public void OnTurnConfirmed()
    {
        if (!IsActive) return;
        if (CurrentStep == TutorialStep.Turn1_Confirm)
            EnterStep(TutorialStep.Turn2_Intro);
    }

    /// <summary>적이 죽었을 때 BattleManager에서 호출</summary>
    public void OnEnemyDefeated()
    {
        if (!IsActive) return;
        if (CurrentStep == TutorialStep.Turn2_Place)
            EnterStep(TutorialStep.Complete);
    }

    public void CompleteTutorial()
    {
        TutorialStepRules rules = GetStepRules(CurrentStep);
        if (rules != null)
            foreach (GameObject obj in rules.highlightObjects)
                if (obj != null) obj.SetActive(false);

        ClearAllHighlights();
        BattleManager.Instance.Player.SetHandSize(BattleManager.Instance.Player.DefaultHandSize);
        CardManager.Instance.DiscardHand();
        CardManager.Instance.DiscardGrid();
        GameManager.Instance.GoToMainMenu();
    }

    protected override void Dispose()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        OnStepChanged = null;
        OnWrongAction = null;
        base.Dispose();
    }
}