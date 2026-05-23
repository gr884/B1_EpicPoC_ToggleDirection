using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : SingletonBehaviour<ItemManager>
{
    [Header("Refs")]
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private RectTransform _pickupRoot;
    [SerializeField] private Canvas _rootCanvas;

    [Header("Pickup Layout")]
    [SerializeField] private Vector2 _itemSize = new Vector2(80f, 80f);
    [SerializeField] private float _itemSpacing = 10f;

    [Header("Settings")]
    [SerializeField] private int _maxSelectCount = 3;

    private readonly List<ItemView> _pickupItems = new();

    public int MaxSelectCount => _maxSelectCount;
    public int CurrentSelectCount => BackpackManager.Instance.PlacedItems.Count;
    public bool CanSelect => CurrentSelectCount < _maxSelectCount;

    public event Action OnSelectCountChanged;

    public void Init()
    {
        if (_rootCanvas == null)
            _rootCanvas = FindFirstObjectByType<Canvas>();

        Debug.Log("[ItemManager] Init");
    }

    // ── 픽업 아이템 세팅 ───────────────────────────────────

    public void SetupPickupItems(List<ItemData> items, int maxSelect)
    {
        ClearPickupItems();
        _maxSelectCount = maxSelect;

        foreach (ItemData data in items)
        {
            if (data == null) continue;
            ItemView item = SpawnPickupItem(data);
            _pickupItems.Add(item);
        }

        ArrangePickupItems();
    }

    public void ConfirmSelection()
    {
        foreach (var kv in BackpackManager.Instance.PlacedItems)
        {
            GridSlot slot = GridManager.Instance.GetSlot(kv.Key);
            if (slot?.OccupiedItem != null && slot.OccupiedItem.IsPickupItem)
                slot.OccupiedItem.SetPickupItem(false);
        }

        ClearPickupItems();
        Debug.Log("[ItemManager] 고르기 완료");
    }

    // ── 백팩 ↔ 픽업 영역 ──────────────────────────────────

    public bool TryMoveToBackpack(ItemView item)
    {
        if (!CanSelect)
        {
            Debug.Log("[ItemManager] 최대 선택 개수 초과");
            return false;
        }
        return true;
    }

    public void OnItemRemovedFromBackpack(ItemView item)
    {
        if (item == null) return;

        item.SetPickupItem(true);

        if (!_pickupItems.Contains(item))
            _pickupItems.Add(item);

        item.SetDraggable(true);
        OnSelectCountChanged?.Invoke();
    }

    public void NotifySelectCountChanged()
    {
        OnSelectCountChanged?.Invoke();
    }

    // ── 스폰/반환 ──────────────────────────────────────────

    public ItemView SpawnOnSlot(ItemData data, Transform parent, bool startsActivated)
    {
        if (_itemPrefab == null || data == null) return null;

        GameObject obj = PoolManager.Instance.Get(_itemPrefab, Vector3.zero, parent);
        ItemView item = obj.GetComponent<ItemView>();
        item.Initialize(data, startsActivated);
        item.SetDraggable(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        return item;
    }

    public void ReturnItem(ItemView item)
    {
        if (item == null) return;
        _pickupItems.Remove(item);
        PoolManager.Instance.Return(item.gameObject);
        OnSelectCountChanged?.Invoke();
    }

    // ── 내부 ───────────────────────────────────────────────

    private ItemView SpawnPickupItem(ItemData data)
    {
        GameObject obj = PoolManager.Instance.Get(_itemPrefab, Vector3.zero, _pickupRoot);
        ItemView item = obj.GetComponent<ItemView>();
        item.Initialize(data, false, isPickupItem: true);
        item.SetDraggable(true);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = _itemSize;
        }

        return item;
    }

    private void ArrangePickupItems()
    {
        int count = _pickupItems.Count;
        if (count == 0) return;

        float step = _itemSize.x + _itemSpacing;
        float start = -((count - 1) * 0.5f) * step;

        for (int i = 0; i < count; i++)
        {
            if (_pickupItems[i] == null) continue;
            RectTransform rect = _pickupItems[i].GetComponent<RectTransform>();
            if (rect == null) continue;
            rect.anchoredPosition = new Vector2(start + i * step, 0f);
        }
    }

    private void ClearPickupItems()
    {
        foreach (ItemView item in _pickupItems)
        {
            if (item != null)
                PoolManager.Instance.Return(item.gameObject);
        }
        _pickupItems.Clear();
        OnSelectCountChanged?.Invoke();
    }

    protected override void Dispose()
    {
        OnSelectCountChanged = null;
        base.Dispose();
    }
}