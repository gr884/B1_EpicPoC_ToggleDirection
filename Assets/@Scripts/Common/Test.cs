using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private List<ItemData> _testPickupItems = new();
    [SerializeField] private int _maxSelectCount = 3;

    private void Start()
    {
        ItemManager.Instance.SetupPickupItems(_testPickupItems, _maxSelectCount);
        Debug.Log("[Test] 테스트 픽업 아이템 세팅 완료");
    }
}