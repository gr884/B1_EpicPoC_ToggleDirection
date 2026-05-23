using UnityEngine;

[DefaultExecutionOrder(-100)]
public class BootManager : MonoBehaviour
{
    private void Start()
    {
        BoardManager.Instance.Init();
        DeckManager.Instance.Init();
        GameManager.Instance.Init();
        GameFlowManager.Instance.Init();
        UIManager.Instance.Init();

        Debug.Log("[BootManager] 초기화 완료");
    }
}