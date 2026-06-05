using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_TutorialPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private TMP_Text _confirmButtonText;

    private void Start()
    {
        _confirmButton.onClick.AddListener(OnConfirmClicked);
        TutorialManager tutorial = TutorialManager.Instance;
        if (tutorial != null)
        {
            tutorial.OnStepChanged += OnStepChanged;
            tutorial.OnWrongAction += OnWrongAction;
        }
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnStepChanged -= OnStepChanged;
            TutorialManager.Instance.OnWrongAction -= OnWrongAction;
        }
    }

    private void OnWrongAction(string message)
    {
        // 기존 메시지는 유지하면서 피드백만 잠깐 보여줌
        _messageText.text = message;
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        // 2초 후 원래 단계 메시지로 복원
        CancelInvoke(nameof(RestoreStepMessage));
        Invoke(nameof(RestoreStepMessage), 2f);
    }

    private void RestoreStepMessage()
    {
        TutorialManager tutorial = TutorialManager.Instance;
        if (tutorial != null)
            OnStepChanged(tutorial.CurrentStep);
    }

    private void OnStepChanged(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.Intro:
                Show("튜토리얼에 오신 걸 환영합니다!\n\n적의 의도를 확인하세요.\n적은 이번 턴에 공격 10, 방어 5를 할 예정입니다.\n\n당신의 HP는 5입니다. 공격을 막지 못하면 죽습니다!", "다음");
                break;
            case TutorialStep.Turn1_Place:
                Show("방어5 카드를 그리드에 드래그 해서 배치하세요.\n\n카드가 배치되면 ON 상태가 됩니다.\n<b>OFF → ON이 될 때만</b> 화살표 방향으로 체인이 전파되고 효과가 발동됩니다.", null);
                break;
            case TutorialStep.Turn1_Cost:
                Show("카드를 배치하면 Cost가 소모됩니다.\n왼쪽 하단에서 남은 Cost를 확인할 수 있어요.\n\nCost가 부족하면 카드를 배치할 수 없습니다.", "다음");
                break;
            case TutorialStep.Turn1_Chain:
                Show("아직 방어가 부족합니다...\n공격5 카드 2장을 배치해서\n방어5 카드를 OFF → ON 시키세요!\n\n같은 카드도 다시 ON되면 효과가 또 발동됩니다.", null);
                break;
            case TutorialStep.Turn1_Confirm:
                Show("방어 10이 쌓였습니다!\n적의 공격 10을 막을 수 있어요.\n\n확정 버튼을 눌러 턴을 마무리하세요.", null);
                break;
            case TutorialStep.Turn2_Intro:
                Show("적의 HP가 5 남았습니다.\n\n하지만 적이 다음 턴에 40의 공격을 예고했습니다!\n반드시 이번 턴에 적을 처치해야 합니다.", "다음");
                break;
            case TutorialStep.Turn2_Place:
                Show("배운 것을 활용해서 공격을 최대한 높이세요.\n카드를 끄고 다시 켜서 효과를 여러 번 발동시킬 수 있습니다!", null);
                break;
            case TutorialStep.Turn3_Guided:
                Show("이제 핵심 기술을 배울 차례입니다!\n\n← 카드 2장, → 카드 2장이 있어요.\n잘 배치하면 30의 피해를 한 번에 넣을 수 있습니다.\n안내에 따라 배치해보세요.", null);
                break;
            case TutorialStep.Turn3_Free:
                Show("이제 직접 해보세요!\n\n← ← → → 4장으로 적에게 30딜을 넣어보세요.\n힌트: 화살표가 서로를 가리키게 놓아보세요.\n실패하면 다시 도전할 수 있어요.", null);
                break;
            case TutorialStep.Complete:
                Show("튜토리얼 완료!\n\n이제 게임의 핵심을 이해했습니다.\n즐거운 플레이 되세요!", "메인 메뉴로");
                break;
            default:
                SetVisible(false);
                break;
        }
    }

    private void OnConfirmClicked()
    {
        TutorialManager tutorial = TutorialManager.Instance;
        if (tutorial == null) return;

        TutorialStep step = tutorial.CurrentStep;
        SetVisible(false);

        switch (step)
        {
            case TutorialStep.Intro:
                tutorial.EnterStep(TutorialStep.Turn1_Place);
                break;
            case TutorialStep.Turn1_Cost:
                tutorial.EnterStep(TutorialStep.Turn1_Chain);
                break;
            case TutorialStep.Turn2_Intro:
                tutorial.EnterStep(TutorialStep.Turn2_Place);
                break;
            case TutorialStep.Complete:
                tutorial.CompleteTutorial();
                break;
        }
    }

    private void Show(string message, string buttonLabel)
    {
        _messageText.text = message;

        bool hasButton = buttonLabel != null;
        _confirmButton.gameObject.SetActive(hasButton);
        if (hasButton) _confirmButtonText.text = buttonLabel;

        // 버튼 없는 힌트 메시지는 레이캐스트를 막지 않음 (플레이어가 카드 조작 가능)
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = hasButton;
        _canvasGroup.blocksRaycasts = hasButton;
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}
