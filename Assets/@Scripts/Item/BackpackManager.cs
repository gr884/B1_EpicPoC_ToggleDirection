using System;
using System.Collections.Generic;
using UnityEngine;

public class BackpackManager : SingletonBehaviour<BackpackManager>
{
    // 좌표 하나당 아이템 데이터 하나를 매핑 (단일 칸)
    private readonly Dictionary<Vector2Int, ItemView> _placedItems = new();
    public IReadOnlyDictionary<Vector2Int, ItemView> PlacedItems => _placedItems;

    public event Action OnBackpackChanged;

    public void Init()
    {
        Debug.Log("[BackpackManager] Init");
    }

    // [수정] 아이템을 특정 슬롯에 배치 시도
    public bool TryPlaceItem(ItemView item, GridSlot targetSlot)
    {
        if (item == null || targetSlot == null || !targetSlot.IsEmpty || targetSlot.IsLocked) return false;
        if (!GameManager.Instance.IsPlaying) return false;

        // 이미 다른 아이템이 그 자리에 있다면 실패
        if (_placedItems.TryGetValue(targetSlot.Position, out var existingItem) && existingItem != item)
            return false;

        // 픽업 아이템인 경우에만 선택 가능 개수 체크
        if (item.IsPickupItem && !ItemManager.Instance.TryMoveToBackpack(item)) return false;

        // [핵심] 기존에 가방 내 다른 슬롯에 있었다면 기존 위치 데이터 먼저 제거 (이동 처리)
        if (item.IsPlaced && item.CurrentSlot != null)
        {
            _placedItems.Remove(item.CurrentSlot.Position);
            item.CurrentSlot.ClearItem();
        }

        // 새 위치에 등록
        _placedItems[targetSlot.Position] = item;
        targetSlot.AssignItem(item);
        item.SetDraggable(true);

        OnBackpackChanged?.Invoke();
        ItemManager.Instance.NotifySelectCountChanged();

        Debug.Log($"[BackpackManager] 배치 성공: {item.Data.displayName} @ {targetSlot.Position}");
        return true;
    }

    // [수정] 백팩에서 완전히 아이템을 제거 (바닥에 버릴 때 사용)
    public bool TryRemoveItem(ItemView item)
    {
        if (item == null || !GameManager.Instance.IsPlaying) return false;

        // 해당 아이템이 위치한 좌표 찾기
        Vector2Int targetPosition = Vector2Int.left; // 잘못된 값으로 초기화
        bool found = false;

        foreach (var kvp in _placedItems)
        {
            if (kvp.Value == item)
            {
                targetPosition = kvp.Key;
                found = true;
                break;
            }
        }

        if (!found) return false;

        // 데이터 및 슬롯 정리
        _placedItems.Remove(targetPosition);
        if (item.CurrentSlot != null)
        {
            item.CurrentSlot.ClearItem();
        }

        // 백팩에서 완전히 빠진 아이템 처리 (자유 배치 상태 혹은 버림 상태로)
        ItemManager.Instance.OnItemRemovedFromBackpack(item);
        OnBackpackChanged?.Invoke();

        Debug.Log($"[BackpackManager] 가방에서 제거됨: {item.Data.displayName}");
        return true;
    }

    public bool IsOccupied(Vector2Int position) => _placedItems.ContainsKey(position);

    public void Clear()
    {
        _placedItems.Clear();
    }
}