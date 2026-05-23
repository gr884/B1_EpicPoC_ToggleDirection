using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 보드에 놓인 카드를 클릭하면 다시 패로 돌려보낸다.
/// Card 프리팹에 추가하거나, BoardSlot에 추가해서 쓴다.
/// </summary>
public class Ryeol_CardClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Ryeol_GameManager gameManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<Ryeol_GameManager>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그 중에는 클릭 무시
        if (eventData.dragging) return;

        Card card = GetComponent<Card>();
        if (card == null || !card.IsPlacedOnBoard) return;

        BoardSlot slot = card.CurrentSlot;
        if (slot == null) return;

        gameManager?.TryRemoveCard(slot);
    }
}
