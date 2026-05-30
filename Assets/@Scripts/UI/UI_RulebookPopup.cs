using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_RulebookPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _contentText;
    [SerializeField] private Button _closeButton;

    private const string RULEBOOK_TEXT =
@"<b>═ 기본 흐름 ═</b>

매 턴 손패에서 카드를 그리드에 배치하면 체인이 발동됩니다.
턴 확정 버튼을 누르면 적이 행동합니다.
적의 다음 행동은 미리 표시됩니다.


<b>═ 핵심 메카닉: ON / OFF ═</b>

카드는 활성화(ON)될 때마다 효과가 발동됩니다.
이미 켜진 카드도 다시 체인이 닿으면 꺼졌다가 켜지며 효과가 다시 발동됩니다.
끄고 켜는 것을 활용해 같은 카드로 여러 번 효과를 얻을 수 있습니다.


<b>═ 체인 ═</b>

카드를 그리드에 배치하면 화살표 방향으로 체인이 전파됩니다.
체인이 닿은 카드는 ON/OFF가 토글됩니다.
OFF → ON이 될 때만 효과가 발동됩니다.


<b>═ 카드 ═</b>

카드마다 Cost가 있고 턴마다 Cost가 충전됩니다.
그리드에 배치된 카드는 턴 종료 시 버려집니다.
배치된 카드를 우클릭하면 Cost 1을 소모해 손패로 회수합니다.


<b>═ 전투 ═</b>

쌓은 방어는 적의 공격을 막고 턴 종료 시 사라집니다.
적의 방어는 플레이어의 공격으로 깎을 수 있습니다.";

    private void Awake()
    {
        _closeButton.onClick.AddListener(Hide);
        if (_contentText != null)
            _contentText.text = RULEBOOK_TEXT;
        SetVisible(false);
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }
}