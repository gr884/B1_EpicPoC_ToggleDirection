using UnityEngine;

public class TutorialSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TutorialSceneBootstrap] GameManager가 없어 튜토리얼을 시작할 수 없습니다.");
            return;
        }

        GameManager.Instance.BeginTutorialSession();
    }
}
