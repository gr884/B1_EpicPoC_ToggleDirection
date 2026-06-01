using UnityEngine;

public class BattleActorMotionTarget : MonoBehaviour
{
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private Transform _attackPoint;

    public Transform VisualRoot => _visualRoot != null ? _visualRoot : transform;
    public Transform AttackPoint => _attackPoint != null ? _attackPoint : VisualRoot;
}
