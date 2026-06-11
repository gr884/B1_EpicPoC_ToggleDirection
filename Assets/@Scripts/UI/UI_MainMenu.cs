using UnityEngine;
using UnityEngine.UI;

public class UI_MainMenu : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _tutorialButton;

    [Header("Card Menu")]
    [SerializeField] private MainMenuGridController _menuGrid;
    [SerializeField] private MainMenuHandController _menuHand;
    [SerializeField] private MainMenuCardResolver _menuResolver;
    [SerializeField] private bool _initializeCardMenuOnStart = true;

    private bool _cardMenuInitialized;

    private void Start()
    {
        if (_startButton != null)
            _startButton.onClick.AddListener(OnStartClicked);
        if (_tutorialButton != null)
            _tutorialButton.gameObject.SetActive(false);

        if (_initializeCardMenuOnStart)
            InitializeCardMenu();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnStateChanged;
            SetVisible(GameManager.Instance.CurrentState == GameManager.GameState.Idle);
        }
        else
        {
            SetVisible(true);
        }
    }

    private void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartClicked);
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        SetVisible(state == GameManager.GameState.Idle);
    }

    private void OnStartClicked()
    {
        GameManager.Instance.GameStart();
    }

    public void InitializeCardMenu()
    {
        if (_cardMenuInitialized) return;

        if (_menuGrid == null)
            _menuGrid = GetComponentInChildren<MainMenuGridController>(true);
        if (_menuHand == null)
            _menuHand = GetComponentInChildren<MainMenuHandController>(true);
        if (_menuResolver == null)
            _menuResolver = GetComponentInChildren<MainMenuCardResolver>(true);

        _menuGrid?.Initialize();
        _menuHand?.Initialize();
        _menuResolver?.Initialize();
        _cardMenuInitialized = true;
    }

    public void StartGameFromMenuCard()
    {
        SetVisible(false);
        GameManager.Instance?.GameStart();
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
