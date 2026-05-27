using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드로우 파일 수 / 버린 파일 수를 표시.
/// 각 버튼 클릭 시 해당 파일의 카드 목록 팝업 표시.
/// </summary>
public class UI_DeckView : MonoBehaviour
{
    [Header("Draw Pile")]
    [SerializeField] private TMP_Text _drawPileCountText;
    [SerializeField] private Button _drawPileButton;

    [Header("Discard Pile")]
    [SerializeField] private TMP_Text _discardPileCountText;
    [SerializeField] private Button _discardPileButton;

    [Header("Popup")]
    [SerializeField] private UI_CardListPopup _popup;

    private void Start()
    {
        CardManager.Instance.OnHandChanged += Refresh;

        _drawPileButton.onClick.AddListener(() =>
            _popup.Show("드로우 파일", CardManager.Instance.DrawPile));

        _discardPileButton.onClick.AddListener(() =>
            _popup.Show("버린 파일", CardManager.Instance.DiscardPile));

        Refresh();
    }

    private void OnDestroy()
    {
        if (CardManager.Instance != null)
            CardManager.Instance.OnHandChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_drawPileCountText != null)
            _drawPileCountText.text = CardManager.Instance.DrawPileCount.ToString();

        if (_discardPileCountText != null)
            _discardPileCountText.text = CardManager.Instance.DiscardPileCount.ToString();
    }
}