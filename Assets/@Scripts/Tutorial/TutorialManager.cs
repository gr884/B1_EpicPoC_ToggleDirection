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
    Turn1_Cost,
    Turn1_Chain,
    Turn1_Confirm,
    Turn2_Intro,
    Turn2_Place,
    Turn3_Guided,
    Turn3_Free,
    Complete,
}

public class TutorialManager : SingletonBehaviour<TutorialManager>
{
    [Header("Tutorial Enemy")]
    [SerializeField] private EnemyDataSO _tutorialEnemyData;

    [Header("Turn3 Settings")]
    [SerializeField] private EnemyDataSO _turn3EnemyData;
    [Tooltip("→ 방향 카드 데이터")]
    [SerializeField] private CardData _turn3RightCard;
    [Tooltip("← 방향 카드 데이터")]
    [SerializeField] private CardData _turn3LeftCard;

    // Turn3_Guided 서브스텝 상태
    private int _turn3PlacedCount;
    private bool _turn3FirstWasRight;

    [Header("Tutorial Deck")]
    [Tooltip("튜토리얼에서 사용할 카드 목록 (순서대로 손패에 들어옴)")]
    [SerializeField] private List<CardData> _tutorialDeck = new();

    [Header("Step Placement Rules")]
    [Tooltip("각 단계별 허용할 카드+슬롯 조합")]
    [SerializeField] private List<TutorialStepRules> _stepRules = new();

    public TutorialStep CurrentStep { get; private set; }
    public bool IsActive => GameManager.Instance.CurrentState == GameManager.GameState.Tutorial;

    public event Action<TutorialStep> OnStepChanged;
    public event Action<string> OnWrongAction;

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
        TutorialStepRules prevRules = GetStepRules(CurrentStep);
        if (prevRules != null)
            foreach (GameObject obj in prevRules.highlightObjects)
                if (obj != null) obj.SetActive(false);

        CurrentStep = step;

        if (step == TutorialStep.Turn3_Guided)
            StartCoroutine(EnterTurn3Routine(_turn3EnemyData));
        else if (step == TutorialStep.Turn3_Free)
            StartCoroutine(RestartTurn3FreeRoutine());

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

        if (CurrentStep == TutorialStep.Turn3_Free) return true;

        if (CurrentStep == TutorialStep.Turn3_Guided)
        {
            Vector2Int allowed = GetTurn3GuidedAllowedSlot(card.Data);
            if (allowed.x < 0) return false;
            return slot.Position == allowed;
        }

        TutorialStepRules stepRules = GetStepRules(CurrentStep);
        if (stepRules == null) return false;

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
        return CurrentStep == TutorialStep.Turn1_Confirm
            || CurrentStep == TutorialStep.Turn3_Free;
    }

    // ── 하이라이트 ─────────────────────────────────────────

    private void RefreshHighlights()
    {
        if (CurrentStep == TutorialStep.Turn3_Guided)
        {
            RefreshTurn3GuidedHighlights();
            return;
        }

        ClearAllHighlights();

        TutorialStepRules stepRules = GetStepRules(CurrentStep);
        if (stepRules == null) return;

        foreach (CardView card in CardManager.Instance.Hand)
        {
            bool shouldHighlight = false;

            foreach (TutorialPlacementRule rule in stepRules.placementRules)
                if (card.Data == rule.card) { shouldHighlight = true; break; }

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

    // ── 드래그 체크 ────────────────────────────────────────

    public bool CanDragCard(CardView card)
    {
        if (!IsActive) return true;

        if (CurrentStep == TutorialStep.Turn3_Guided)
        {
            Vector2Int allowed = GetTurn3GuidedAllowedSlot(card.Data);
            if (allowed.x < 0)
            {
                OnWrongAction?.Invoke("지금은 반대 방향 카드를 놓아야 해요!");
                return false;
            }
            return true;
        }

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
                StartCoroutine(EnterStepRoutine(TutorialStep.Turn1_Cost));
                break;
            case TutorialStep.Turn1_Chain:
                if (CardManager.Instance.HandCount == 0)
                    StartCoroutine(EnterStepRoutine(TutorialStep.Turn1_Confirm));
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
            case TutorialStep.Turn3_Guided:
                AdvanceTurn3Guided(card.Data);
                break;
        }
    }

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

    public void OnEnemyDefeated()
    {
        if (!IsActive) return;
        if (CurrentStep == TutorialStep.Turn2_Place)
            StartCoroutine(EnterStepDelayedRoutine(TutorialStep.Turn3_Guided));
        else if (CurrentStep == TutorialStep.Turn3_Guided)
            StartCoroutine(EnterStepDelayedRoutine(TutorialStep.Turn3_Free));
        else if (CurrentStep == TutorialStep.Turn3_Free)
            StartCoroutine(EnterStepDelayedRoutine(TutorialStep.Complete));
    }

    private System.Collections.IEnumerator EnterStepRoutine(TutorialStep step)
    {
        yield return null;
        EnterStep(step);
    }

    private System.Collections.IEnumerator EnterStepDelayedRoutine(TutorialStep step)
    {
        yield return new WaitForSecondsRealtime(1.2f);
        EnterStep(step);
    }

    public void OnTurn3FreeFailed()
    {
        if (!IsActive || CurrentStep != TutorialStep.Turn3_Free) return;
        // Turn3_Free 재진입 — EnterStep이 RestartTurn3FreeRoutine 코루틴을 실행
        EnterStep(TutorialStep.Turn3_Free);
    }

    // ── Turn3 내부 ─────────────────────────────────────────

    private System.Collections.IEnumerator EnterTurn3Routine(EnemyDataSO enemyData)
    {
        yield return new WaitForSecondsRealtime(1.2f);

        _turn3PlacedCount = 0;
        _turn3FirstWasRight = false;
        GridManager.Instance.SetGridSize(1, 4);
        CardManager.Instance.DestroyHand();
        CardManager.Instance.DiscardGrid();
        BattleManager.Instance.Player.SetHandSize(4);

        List<CardData> deck = new() { _turn3RightCard, _turn3RightCard, _turn3LeftCard, _turn3LeftCard };
        CardManager.Instance.DrawToHandFresh(deck);

        BattleManager.Instance.SetTutorialEnemy(enemyData);
        BattleManager.Instance.ResetToPlayerTurn();
        RefreshHighlights();
    }

    private System.Collections.IEnumerator RestartTurn3FreeRoutine()
    {
        yield return new WaitForSecondsRealtime(1.2f);

        CardManager.Instance.DestroyHand();
        CardManager.Instance.DiscardGrid();

        List<CardData> deck = new() { _turn3RightCard, _turn3RightCard, _turn3LeftCard, _turn3LeftCard };
        CardManager.Instance.DrawToHandFresh(deck);

        BattleManager.Instance.SetTutorialEnemy(_turn3EnemyData);
        BattleManager.Instance.ResetToPlayerTurn();
        RefreshHighlights();
        Debug.Log("[TutorialManager] Turn3_Free 재시작 (도르마무)");
    }

    private Vector2Int GetTurn3GuidedAllowedSlot(CardData cardData)
    {
        bool isRight = cardData == _turn3RightCard;
        bool isLeft = cardData == _turn3LeftCard;
        if (!isRight && !isLeft) return new Vector2Int(-1, 0);

        bool shouldBeRight;
        if (_turn3PlacedCount == 0)
            shouldBeRight = isRight;
        else
            shouldBeRight = (_turn3PlacedCount % 2 == 0) == _turn3FirstWasRight;

        if (isRight != shouldBeRight) return new Vector2Int(-1, 0);

        if (_turn3PlacedCount == 0)
            return isRight ? new Vector2Int(1, 0) : new Vector2Int(2, 0);

        return (_turn3PlacedCount, _turn3FirstWasRight, isRight) switch
        {
            (1, true, false) => new Vector2Int(2, 0),
            (1, false, true) => new Vector2Int(1, 0),
            (2, true, true) => new Vector2Int(0, 0),
            (2, false, false) => new Vector2Int(3, 0),
            (3, true, false) => new Vector2Int(3, 0),
            (3, false, true) => new Vector2Int(0, 0),
            _ => new Vector2Int(-1, 0)
        };
    }

    private void AdvanceTurn3Guided(CardData cardData)
    {
        if (_turn3PlacedCount == 0)
            _turn3FirstWasRight = (cardData == _turn3RightCard);

        _turn3PlacedCount++;
        RefreshTurn3GuidedHighlights();
        Debug.Log($"[TutorialManager] Turn3_Guided {_turn3PlacedCount}/4 완료");
    }

    private void RefreshTurn3GuidedHighlights()
    {
        ClearAllHighlights();

        foreach (CardView card in CardManager.Instance.Hand)
        {
            Vector2Int allowed = GetTurn3GuidedAllowedSlot(card.Data);
            card.SetHighlight(allowed.x >= 0);

            if (allowed.x >= 0)
            {
                GridSlot slot = GridManager.Instance.GetSlot(allowed);
                if (slot != null) slot.SetHighlight(true);
            }
        }
    }

    // ── 튜토리얼 완료 ──────────────────────────────────────

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