using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 플레이그라운드 전용 BoardSlot 드롭 핸들러.
/// 기존 BoardSlot을 수정하지 않고 별도 컴포넌트로 분리.
/// BoardSlot과 같은 GameObject에 추가하면 OnDrop을 오버라이드한다.
///
/// [사용법]
/// BoardSlot 프리팹에 이 컴포넌트를 추가하고,
/// 기존 BoardSlot의 OnDrop은 gameManager가 null이면 아무것도 안 하므로 충돌 없음.
/// </summary>
public class Ryeol_BoardSlotDrop : MonoBehaviour, IDropHandler
{
    private Ryeol_GameManager gameManager;
    private BoardSlot slot;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<Ryeol_GameManager>();
        slot = GetComponent<BoardSlot>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (gameManager == null || slot == null) return;

        CardDragHandler dragHandler = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<CardDragHandler>()
            : null;

        if (dragHandler == null || dragHandler.Card == null) return;

        bool placed = gameManager.TryPlaceCard(dragHandler.Card, slot);
        if (placed)
        {
            dragHandler.CommitDrop(transform);
        }
    }
}
