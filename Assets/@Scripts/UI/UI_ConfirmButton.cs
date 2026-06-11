using UnityEngine;
using UnityEngine.UI;

public class UI_ConfirmButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Button _button;
    private bool _visible;

    private void Awake()
    {
        _button.onClick.AddListener(OnClick);
    }

    private void Start()
    {
        BattleManager.Instance.OnPhaseChanged += OnPhaseChanged;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (BattleManager.Instance != null)
            BattleManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(BattleManager.BattlePhase phase)
    {
        RefreshVisible();
    }

    private void Update()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.CurrentPhase == BattleManager.BattlePhase.PlayerTurn)
            RefreshVisible();
    }

    private void OnClick()
    {
        BattleManager.Instance.ConfirmPlayerTurn();
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void RefreshVisible()
    {
        bool visible = BattleManager.Instance != null
            && BattleManager.Instance.CurrentPhase == BattleManager.BattlePhase.PlayerTurn;

        StartSceneTutorialDirector tutorial = StartSceneTutorialDirector.Instance;
        if (tutorial != null && tutorial.IsRunning)
            visible = visible && tutorial.CanConfirmTurn();

        if (_visible != visible)
            SetVisible(visible);
    }
}
