using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private BattleActorView _view;
    [SerializeField] private GameObject _cardPrefab;

    private EnemyDataSO _data;

    public bool IsDead => _view != null && _view.IsDead;

    public event Action OnDied;

    // ── 초기화 ─────────────────────────────────────────────

    public void Setup(EnemyDataSO data)
    {
        _data = data;

        _view.OnDied -= HandleDied;
        _view.OnDied += HandleDied;
        _view.Setup(data.displayName, data.maxHp);

        GridManager.Instance.PlaceEnemyCards(data, _cardPrefab);
    }

    // ── 전투 로직 ──────────────────────────────────────────

    public void TakeAttack(ChainResult result)
    {
        int damage = Mathf.RoundToInt(result.damage);
        int defense = GetCardBonus(EffectType.Defense);
        _view.TakeDamage(Mathf.Max(0, damage - defense));
    }

    public void Attack(Player player)
    {
        int raw = _data.baseDamage + GetCardBonus(EffectType.Damage);
        player.TakeAttack(raw);
    }

    // ── 카드 보너스 ────────────────────────────────────────

    public int GetCardBonus(EffectType type)
    {
        int bonus = 0;
        foreach (GridSlot slot in GridManager.Instance.Slots.Values)
        {
            CardView card = slot.OccupiedCard;
            if (card == null || !card.IsEnemy || !card.IsActivated) continue;
            if (card.Data?.effects == null) continue;

            foreach (CardEffect effect in card.Data.effects)
            {
                if (effect.scope != CountScope.None) continue;
                if (effect.effectType == type)
                    bonus += Mathf.RoundToInt(effect.value);
            }
        }
        return bonus;
    }

    // ── 내부 ───────────────────────────────────────────────

    private void HandleDied() => OnDied?.Invoke();

    private void OnDestroy()
    {
        if (_view != null)
            _view.OnDied -= HandleDied;

        OnDied = null;
    }
}