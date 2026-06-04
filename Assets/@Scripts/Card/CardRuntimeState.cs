using System;
using System.Collections.Generic;
using UnityEngine;

public class CardRuntimeState : MonoBehaviour
{
    CardView _owner;
    bool _isEnemy;
    int _bonusDamage;
    bool _subscribed;
    CardData _data;

    static readonly Dictionary<CardData, int> s_combatBonus = new();

    public int BonusDamage => _bonusDamage;

    public event Action OnChanged;

    void OnEnable()
    {
        TrySubscribeBattleEvents();
    }

    void OnDisable()
    {
        UnsubscribeBattleEvents();
    }

    public void Initialize(CardView owner, CardData data, bool isEnemy)
    {
        _owner = owner;
        _data = data;
        _isEnemy = isEnemy;

        if (_isEnemy || _data == null) _bonusDamage = 0;
        else
            s_combatBonus.TryGetValue(_data, out _bonusDamage);
        
        OnChanged?.Invoke();
        TrySubscribeBattleEvents();
    }

    public int GetModifiedDamage(int baseValue)
    {
        return baseValue + _bonusDamage;
    }

    //* amount만큼 누적 데미지 추가
    public void AddBonusDamage(int amount)
    {
        if (_isEnemy || _data == null) return;

        int add = Mathf.Max(0, amount);
        if (add == 0) return;

        _bonusDamage += add;
        s_combatBonus[_data] = _bonusDamage;
        OnChanged?.Invoke();
    }

    public void ResetTurn()
    {
        s_combatBonus.Clear();
        _bonusDamage = 0;
        OnChanged?.Invoke();
    }

    //* 전투가 끝나면 _bonusDamage 초기화
    public void ResetBattle()
    {
        s_combatBonus.Clear();
        _bonusDamage = 0;
        OnChanged?.Invoke();
    }

    //* BattleManager에 구독
    private void TrySubscribeBattleEvents()
    {
        // 이미 구독되어 있거나 BattleManager가 존재하지 않으면 리턴
        if (_subscribed) return;
        if (BattleManager.Instance == null) return;

        BattleManager.Instance.OnBattleEnded += ResetBattle;
        _subscribed = true;
    }

    //* BattleManager로부터 구독 해제
    private void UnsubscribeBattleEvents()
    {
        if (!_subscribed) return;
        if (BattleManager.Instance == null) { _subscribed = false; return;}

        BattleManager.Instance.OnBattleEnded -= ResetBattle;
        _subscribed = false;
    }
}
