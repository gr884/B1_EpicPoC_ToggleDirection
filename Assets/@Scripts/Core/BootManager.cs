using UnityEngine;

[DefaultExecutionOrder(-100)]
public class BootManager : MonoBehaviour
{
    private void Start()
    {
        PoolManager.Instance.Init();
        GridManager.Instance.Init();
        CardManager.Instance.Init();
        ChainExecutor.Instance.Init();
        BattleManager.Instance.Init();
        GameManager.Instance.Init();
        GameFlowManager.Instance.Init();

        Debug.Log("[BootManager] 초기화 완료");
    }
}
