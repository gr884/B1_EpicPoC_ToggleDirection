using UnityEngine;

public class SpumAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private SpumEnemyMotionPlayer _motionPlayer;

    private void Awake()
    {
        if (_motionPlayer == null)
            _motionPlayer = GetComponentInParent<SpumEnemyMotionPlayer>(true);
    }

    public void OnAttackImpact()
    {
        _motionPlayer?.OnAttackImpact();
    }
}
