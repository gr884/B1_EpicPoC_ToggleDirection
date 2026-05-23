using UnityEngine;

[DefaultExecutionOrder(-100)]
public class BootManager : MonoBehaviour
{
    private void Start()
    {
        PoolManager.Instance.Init();
        GridManager.Instance.Init();
        ItemManager.Instance.Init();
        BackpackManager.Instance.Init();
        ChainExecutor.Instance.Init();
        GameManager.Instance.Init();
        GameFlowManager.Instance.Init();

        GameManager.Instance.GameStart();

        Debug.Log("[BootManager] 초기화 완료");
    }
}