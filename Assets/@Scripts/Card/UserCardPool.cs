using System.Collections.Generic;
using UnityEngine;

public class CardInstance
{
    public CardData Data;
    public List<CardDirection> Directions;
}

public class UserCardPool : SingletonBehaviour<UserCardPool>
{
    [Header("Deck")]
    [SerializeField] private List<CardData> _startingDeck = new();
    [SerializeField] private bool _shuffleOnReset = true;
    [SerializeField] private int _initialDrawCount = 5;
    [SerializeField] private int _turnDrawCount = 1;

    private readonly List<CardInstance> _drawPile = new();
    private readonly List<CardInstance> _graveyard = new();

    public int InitialDrawCount => _initialDrawCount;
    public int TurnDrawCount => _turnDrawCount;
    public int DrawPileCount => _drawPile.Count;
    public int GraveyardCount => _graveyard.Count;

    public void Init()
    {
        Debug.Log("[UserCardPool] Init");
    }

    public void ResetForBattle()
    {
        _drawPile.Clear();
        _graveyard.Clear();

        if (_startingDeck != null)
        {
            foreach (CardData data in _startingDeck)
            {
                // directionFlags가 설정된 카드 → 고정 방향 사용, 아니면 랜덤 롤
                List<CardDirection> dirs = data.GetFixedDirections() ?? RollDirections();
                _drawPile.Add(new CardInstance { Data = data, Directions = dirs });
            }
        }

        if (_shuffleOnReset)
            Shuffle(_drawPile);

        Debug.Log($"[UserCardPool] 덱 리셋 — {_drawPile.Count}장");
    }

    public List<CardInstance> DrawCards(int count)
    {
        List<CardInstance> drawn = new();

        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            if (_drawPile.Count == 0)
                RefillFromGraveyard();

            if (_drawPile.Count == 0) break;

            int last = _drawPile.Count - 1;
            drawn.Add(_drawPile[last]);
            _drawPile.RemoveAt(last);
        }

        Debug.Log($"[UserCardPool] {drawn.Count}장 드로우 — 드로우파일: {_drawPile.Count} / 묘지: {_graveyard.Count}");
        return drawn;
    }

    public void DiscardToGraveyard(CardInstance instance)
    {
        if (instance == null) return;
        _graveyard.Add(instance);
        Debug.Log($"[UserCardPool] 묘지 이동: {instance.Data?.displayName} — 묘지: {_graveyard.Count}");
    }

    // ── 방향 확률 생성 (덱 리셋 시 1회) ────────────────────

    public enum DirectionMode { FourWay, EightWay }

    [Header("Direction Roll")]
    [SerializeField] private DirectionMode _directionMode = DirectionMode.EightWay;
    // index 0 = 화살표 1개, index 1 = 2개, ... 상대 가중치 (합산 후 정규화)
    [SerializeField] private float[] _countWeights = { 50f, 30f, 15f, 5f };

    private static readonly CardDirection[] _fourWayPool =
    {
        CardDirection.Up, CardDirection.Right, CardDirection.Down, CardDirection.Left
    };

    private static readonly CardDirection[] _eightWayPool =
    {
        CardDirection.Up,    CardDirection.UpRight,
        CardDirection.Right, CardDirection.DownRight,
        CardDirection.Down,  CardDirection.DownLeft,
        CardDirection.Left,  CardDirection.UpLeft
    };

    private List<CardDirection> RollDirections()
    {
        CardDirection[] source = _directionMode == DirectionMode.FourWay
            ? _fourWayPool : _eightWayPool;

        int count = Mathf.Min(RollCount(), source.Length);

        CardDirection[] pool = (CardDirection[])source.Clone();
        for (int i = pool.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        List<CardDirection> result = new(count);
        for (int i = 0; i < count; i++) result.Add(pool[i]);
        return result;
    }

    private int RollCount()
    {
        if (_countWeights == null || _countWeights.Length == 0) return 1;

        float total = 0f;
        foreach (float w in _countWeights) total += Mathf.Max(0f, w);
        if (total <= 0f) return 1;

        float r = Random.value * total;
        float cumulative = 0f;
        for (int i = 0; i < _countWeights.Length; i++)
        {
            cumulative += Mathf.Max(0f, _countWeights[i]);
            if (r < cumulative) return i + 1;
        }
        return _countWeights.Length;
    }

    // ── 내부 ───────────────────────────────────────────────

    private void RefillFromGraveyard()
    {
        if (_graveyard.Count == 0)
        {
            Debug.Log("[UserCardPool] 묘지도 비어있음 — 드로우 불가");
            return;
        }

        Shuffle(_graveyard);
        _drawPile.AddRange(_graveyard);
        _graveyard.Clear();
        Debug.Log($"[UserCardPool] 묘지 셔플 → 드로우파일 재구성 — {_drawPile.Count}장");
    }

    private static void Shuffle(List<CardInstance> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
