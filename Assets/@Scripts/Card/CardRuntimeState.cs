using System;
using UnityEngine;

public class CardRuntimeState : MonoBehaviour
{
    private CardView _owner;
    private CardInstance _instance;
    private bool _isEnemy;

    public CardInstance Instance => _instance;
    public int BonusDamage => !_isEnemy && _instance != null ? _instance.PersistentState.BonusDamage : 0;
    public int CurrentCastingCount { get; private set; }

    public event Action OnChanged;

    public void Initialize(CardView owner, CardInstance instance, bool isEnemy)
    {
        _owner = owner;
        _instance = instance;
        _isEnemy = isEnemy;
        OnChanged?.Invoke();
    }

    public int GetModifiedDamage(int baseValue)
    {
        return baseValue + BonusDamage;
    }

    //* amount만큼 누적 데미지 추가
    public void AddBonusDamage(int amount)
    {
        if (_isEnemy || _instance == null) return;
        int add = Mathf.Max(0, amount);
        if (add == 0) return;
        _instance.PersistentState.AddBonusDamage(add);
        OnChanged?.Invoke();
    }

    //* 누적 데미지를 amount로 덮어쓰기 (OnPlaced 초기화용)
    public void SetBonusDamage(int amount)
    {
        if (_isEnemy || _instance == null) return;
        _instance.PersistentState.SetBonusDamage(amount);
        OnChanged?.Invoke();
    }

    //* amount만큼 누적 데미지 차감 (0 아래로 안 내려감)
    public void DeductBonusDamage(int amount)
    {
        if (_isEnemy || _instance == null) return;
        int deduct = Mathf.Max(0, amount);
        if (deduct == 0) return;
        _instance.PersistentState.DeductBonusDamage(deduct);
        OnChanged?.Invoke();
    }

    public void Refresh()
    {
        OnChanged?.Invoke();
    }

    public void ClearCombatState()
    {
        if (_instance == null) return;
        _instance.PersistentState.ClearCombatState();
        OnChanged?.Invoke();
    }

    public void InitializeCasting(int count)
    {
        CurrentCastingCount = count;
        OnChanged?.Invoke();
    }

    public void DecreaseCasting()
    {
        if (CurrentCastingCount > 0)
        {
            CurrentCastingCount--;
            OnChanged?.Invoke();
        }
    }

    public void ResetCasting()
    {
        CurrentCastingCount = 0;
        OnChanged?.Invoke();
    }
}