using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class TutorialFixedCardSeed
{
    public CardData card;
    public Vector2Int position;
    public bool startsActivated;
}

public class StartSceneTutorialDirector : MonoBehaviour
{
    public static StartSceneTutorialDirector Instance { get; private set; }

    [Header("Scene Gate")]
    [SerializeField] private string _mainSceneName = "Main2_Test";
    [SerializeField] private string _playerPrefsKey = "StartSceneTutorialCompleted";

    [Header("Grid")]
    [SerializeField, Min(1)] private int _rows = 6;
    [SerializeField, Min(1)] private int _columns = 6;
    [SerializeField] private Vector2Int _placementSlot = new(2, 2);
    [SerializeField] private List<TutorialFixedCardSeed> _fixedCards = new();

    [Header("Battle")]
    [SerializeField] private EnemyDataSO _tutorialEnemyData;
    [SerializeField] private CardData _drawCard;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField, Min(1)] private int _playerStartHp = 1;

    [Header("Dialogue")]
    [SerializeField] private List<string> _dialogueLines = new()
    {
        "카드의 화살표는 다른 카드를 켜고 끄며 연쇄를 만듭니다.",
        "비어 있는 한 칸에 손패의 카드를 놓아 보세요.",
        "연쇄가 끝나면 턴 종료 버튼을 눌러 적의 공격까지 확인합니다."
    };
    [SerializeField] private CanvasGroup _dialogueGroup;
    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private Button _dialogueNextButton;
    [SerializeField] private CanvasGroup _turnEndGuideGroup;
    [SerializeField] private TMP_Text _turnEndGuideText;

    private int _dialogueIndex;
    private bool _canPlace;
    private bool _canConfirm;
    private bool _placedCard;
    private bool _waitingForChain;
    private bool _isCompleting;

    public bool IsRunning { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt(_playerPrefsKey, 0) == 1)
        {
            LoadMainScene();
            return;
        }

        EnsureRuntimeUi();

        if (_dialogueNextButton != null)
            _dialogueNextButton.onClick.AddListener(AdvanceDialogue);

        StartCoroutine(StartTutorialRoutine());
    }

    private IEnumerator StartTutorialRoutine()
    {
        yield return null;

        IsRunning = true;
        _canPlace = false;
        _canConfirm = false;
        _placedCard = false;
        _waitingForChain = false;
        _isCompleting = false;

        GameManager.Instance.StartFirstRunTutorial();
        BattleManager.Instance.Player.Setup(_playerStartHp);
        BattleManager.Instance.Player.SetHandSize(1);
        BattleManager.Instance.Player.OnDied += HandlePlayerDied;

        CardManager.Instance.ClearCombatPersistentStates();
        CardManager.Instance.DestroyHand();
        GridManager.Instance.SetGridSize(_rows, _columns);
        PlaceFixedCards();

        if (_drawCard != null)
            CardManager.Instance.SetFixedDrawDeck(new List<CardData> { _drawCard });

        BattleManager.Instance.StartFirstRunTutorialBattle(_tutorialEnemyData);

        ShowDialogueLine(0);
        SetTurnEndGuideVisible(false);
    }

    public bool CanDragCard(CardView card)
    {
        return IsRunning
            && _canPlace
            && card != null
            && !card.IsLocked
            && card.CurrentSlot == null;
    }

    public bool CanPlaceCard(CardView card, GridSlot slot)
    {
        return IsRunning
            && _canPlace
            && !_placedCard
            && card != null
            && slot != null
            && slot.Position == _placementSlot;
    }

    public bool CanConfirmTurn()
    {
        return !IsRunning || _canConfirm;
    }

    public void OnCardPlaced(CardView card)
    {
        if (!IsRunning || _placedCard) return;

        _placedCard = true;
        _canPlace = false;
        _waitingForChain = true;
        ClearHighlights();

        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnChainFinished += HandleChainFinished;
    }

    public void OnCardDragBegin(CardView card)
    {
        if (!IsRunning) return;
        ClearHighlights();
        GridManager.Instance.GetSlot(_placementSlot)?.SetHighlight(true);
    }

    public void OnCardDragCancelled(CardView card)
    {
        if (!IsRunning) return;
        RefreshPlacementHighlights();
    }

    public void OnTurnConfirmed()
    {
        if (!IsRunning) return;
        _canConfirm = false;
        SetTurnEndGuideVisible(false);
    }

    private void AdvanceDialogue()
    {
        if (!IsRunning) return;

        _dialogueIndex++;
        if (_dialogueIndex < _dialogueLines.Count)
        {
            ShowDialogueLine(_dialogueIndex);
            return;
        }

        SetDialogueVisible(false);
        DrawOneCard();
    }

    private void DrawOneCard()
    {
        CardManager.Instance.DrawToHand(1);
        _canPlace = true;
        RefreshPlacementHighlights();
    }

    private void HandleChainFinished()
    {
        if (!_waitingForChain) return;

        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnChainFinished -= HandleChainFinished;

        _waitingForChain = false;
        _canConfirm = true;
        SetTurnEndGuideVisible(true);
    }

    private void HandlePlayerDied()
    {
        if (!IsRunning || _isCompleting) return;
        StartCoroutine(CompleteAndLoadRoutine());
    }

    private IEnumerator CompleteAndLoadRoutine()
    {
        _isCompleting = true;
        PlayerPrefs.SetInt(_playerPrefsKey, 1);
        PlayerPrefs.Save();

        yield return new WaitForSecondsRealtime(0.8f);

        Time.timeScale = 1f;
        LoadMainScene();
    }

    private void PlaceFixedCards()
    {
        GameObject prefab = _cardPrefab != null ? _cardPrefab : CardManager.Instance.CardPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[StartSceneTutorialDirector] 카드 프리팹이 없어 사전 배치를 건너뜁니다.");
            return;
        }

        foreach (TutorialFixedCardSeed seed in _fixedCards)
        {
            if (seed == null || seed.card == null) continue;
            CardView card = GridManager.Instance.PlacePlayerCard(seed.card, prefab, seed.position, seed.startsActivated, locked: true);
            if (card == null)
                Debug.LogWarning($"[StartSceneTutorialDirector] 사전 배치 실패: {seed.position}");
        }
    }

    private void RefreshPlacementHighlights()
    {
        ClearHighlights();

        if (!_canPlace) return;

        foreach (CardView card in CardManager.Instance.Hand)
            if (card != null)
                card.SetHighlight(true);

        GridManager.Instance.GetSlot(_placementSlot)?.SetHighlight(true);
    }

    private void ClearHighlights()
    {
        if (CardManager.Instance != null)
            foreach (CardView card in CardManager.Instance.Hand)
                if (card != null)
                    card.SetHighlight(false);

        if (GridManager.Instance != null)
            foreach (GridSlot slot in GridManager.Instance.Slots.Values)
                if (slot != null)
                    slot.SetHighlight(false);
    }

    private void ShowDialogueLine(int index)
    {
        SetDialogueVisible(true);
        if (_dialogueText != null)
        {
            string line = index >= 0 && index < _dialogueLines.Count ? _dialogueLines[index] : "";
            _dialogueText.text = line;
        }
    }

    private void SetDialogueVisible(bool visible)
    {
        if (_dialogueGroup == null) return;
        _dialogueGroup.alpha = visible ? 1f : 0f;
        _dialogueGroup.interactable = visible;
        _dialogueGroup.blocksRaycasts = visible;
    }

    private void SetTurnEndGuideVisible(bool visible)
    {
        if (_turnEndGuideText != null)
            _turnEndGuideText.text = "연쇄가 끝났습니다. 턴 종료를 눌러 보세요.";

        if (_turnEndGuideGroup == null) return;
        _turnEndGuideGroup.alpha = visible ? 1f : 0f;
        _turnEndGuideGroup.interactable = false;
        _turnEndGuideGroup.blocksRaycasts = false;
    }

    private void EnsureRuntimeUi()
    {
        if (_dialogueGroup != null && _dialogueText != null && _dialogueNextButton != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            GameObject canvasObject = new("StartSceneTutorialCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = new("StartSceneTutorialDialogue");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 42f);
        panelRect.sizeDelta = new Vector2(760f, 170f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        _dialogueGroup = panel.AddComponent<CanvasGroup>();

        GameObject textObject = new("MessageText");
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(28f, 58f);
        textRect.offsetMax = new Vector2(-28f, -22f);
        _dialogueText = textObject.AddComponent<TextMeshProUGUI>();
        _dialogueText.fontSize = 28f;
        _dialogueText.color = Color.white;
        _dialogueText.alignment = TextAlignmentOptions.Left;

        GameObject buttonObject = new("NextButton");
        buttonObject.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-24f, 18f);
        buttonRect.sizeDelta = new Vector2(120f, 44f);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.92f);
        _dialogueNextButton = buttonObject.AddComponent<Button>();

        GameObject labelObject = new("Text");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "다음";
        label.fontSize = 24f;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;

        GameObject guideObject = new("TurnEndGuide");
        guideObject.transform.SetParent(canvas.transform, false);
        RectTransform guideRect = guideObject.AddComponent<RectTransform>();
        guideRect.anchorMin = new Vector2(0.5f, 1f);
        guideRect.anchorMax = new Vector2(0.5f, 1f);
        guideRect.pivot = new Vector2(0.5f, 1f);
        guideRect.anchoredPosition = new Vector2(0f, -42f);
        guideRect.sizeDelta = new Vector2(620f, 64f);
        _turnEndGuideGroup = guideObject.AddComponent<CanvasGroup>();
        _turnEndGuideText = guideObject.AddComponent<TextMeshProUGUI>();
        _turnEndGuideText.fontSize = 26f;
        _turnEndGuideText.color = Color.yellow;
        _turnEndGuideText.alignment = TextAlignmentOptions.Center;
    }

    private void LoadMainScene()
    {
        if (!string.IsNullOrEmpty(_mainSceneName))
            SceneManager.LoadScene(_mainSceneName);
    }

    private void OnDestroy()
    {
        if (_dialogueNextButton != null)
            _dialogueNextButton.onClick.RemoveListener(AdvanceDialogue);

        if (BattleManager.Instance != null && BattleManager.Instance.Player != null)
            BattleManager.Instance.Player.OnDied -= HandlePlayerDied;

        if (ChainExecutor.Instance != null)
            ChainExecutor.Instance.OnChainFinished -= HandleChainFinished;

        if (Instance == this)
            Instance = null;
    }
}
